using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Syllanote.Application.Backups;
using Syllanote.Infrastructure.Persistence;
using System.Text.Json;

namespace Syllanote.Infrastructure.Backups;

public sealed class PendingDatabaseRestoreService
{
    private const long MaximumMetadataSizeInBytes = 64 * 1024;
    private static readonly JsonSerializerOptions MetadataJsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly SyllanoteDbContext _dbContext;

    public PendingDatabaseRestoreService(SyllanoteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> ApplyAsync(
        CancellationToken cancellationToken = default)
    {
        var liveDatabasePath = GetLiveDatabasePath();
        var pendingDatabasePath = LibraryRestorePaths
            .GetPendingDatabasePath(liveDatabasePath);
        var pendingMetadataPath = LibraryRestorePaths
            .GetPendingMetadataPath(liveDatabasePath);
        var hasPendingDatabase = File.Exists(pendingDatabasePath);
        var hasPendingMetadata = File.Exists(pendingMetadataPath);

        if (!hasPendingMetadata)
        {
            if (hasPendingDatabase)
            {
                TryDeleteFile(pendingDatabasePath);
            }

            return false;
        }

        try
        {
            var metadata = await ReadMetadataAsync(
                pendingMetadataPath,
                cancellationToken);
            ValidateSafetyBackup(metadata);

            var knownMigrations = _dbContext.Database
                .GetMigrations()
                .ToHashSet(StringComparer.Ordinal);

            if (!hasPendingDatabase)
            {
                await SqliteLibraryDatabaseValidator.ValidateAsync(
                    liveDatabasePath,
                    metadata.Manifest,
                    knownMigrations,
                    cancellationToken);
                TryDeleteFile(pendingMetadataPath);
                return true;
            }

            await SqliteLibraryDatabaseValidator.ValidateAsync(
                pendingDatabasePath,
                metadata.Manifest,
                knownMigrations,
                cancellationToken);

            await _dbContext.Database.CloseConnectionAsync();
            SqliteConnection.ClearAllPools();
            DeleteLiveDatabaseSidecarFiles(liveDatabasePath);

            if (File.Exists(liveDatabasePath))
            {
                File.Replace(
                    pendingDatabasePath,
                    liveDatabasePath,
                    destinationBackupFileName: null);
            }
            else
            {
                File.Move(pendingDatabasePath, liveDatabasePath);
            }

            TryDeleteFile(pendingMetadataPath);
            return true;
        }
        catch (InvalidLibraryBackupException)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is JsonException or
            SqliteException or
            FormatException or
            InvalidDataException)
        {
            throw new InvalidLibraryBackupException(
                "The pending restore is invalid and was not applied.",
                exception);
        }
    }

    private string GetLiveDatabasePath()
    {
        if (_dbContext.Database.GetDbConnection() is not
            SqliteConnection connection ||
            string.IsNullOrWhiteSpace(connection.DataSource) ||
            connection.DataSource == ":memory:")
        {
            throw new InvalidOperationException(
                "Restore requires a file-based SQLite database.");
        }

        return Path.GetFullPath(connection.DataSource);
    }

    private static async Task<LibraryRestorePendingMetadata>
        ReadMetadataAsync(
            string metadataPath,
            CancellationToken cancellationToken)
    {
        var metadataFile = new FileInfo(metadataPath);
        if (metadataFile.Length <= 0 ||
            metadataFile.Length > MaximumMetadataSizeInBytes)
        {
            throw new InvalidLibraryBackupException(
                "The pending restore metadata has an invalid size.");
        }

        await using var metadataStream = new FileStream(
            metadataPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);
        var metadata = await JsonSerializer.DeserializeAsync<
            LibraryRestorePendingMetadata>(
                metadataStream,
                MetadataJsonOptions,
                cancellationToken);

        if (metadata is null || metadata.Manifest is null)
        {
            throw new InvalidLibraryBackupException(
                "The pending restore metadata is invalid.");
        }

        return metadata;
    }

    private static void ValidateSafetyBackup(
        LibraryRestorePendingMetadata metadata)
    {
        if (string.IsNullOrWhiteSpace(metadata.SafetyBackupPath) ||
            !File.Exists(metadata.SafetyBackupPath))
        {
            throw new InvalidLibraryBackupException(
                "The safety backup for the pending restore is missing.");
        }
    }

    private static void DeleteLiveDatabaseSidecarFiles(
        string liveDatabasePath)
    {
        DeleteFileIfPresent(liveDatabasePath + "-wal");
        DeleteFileIfPresent(liveDatabasePath + "-shm");
    }

    private static void DeleteFileIfPresent(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    private static void TryDeleteFile(string filePath)
    {
        try
        {
            DeleteFileIfPresent(filePath);
        }
        catch
        {
            // A leftover marker is safely rechecked on the next startup.
        }
    }
}
