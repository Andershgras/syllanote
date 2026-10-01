using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Syllanote.Application.Backups;
using Syllanote.Infrastructure.Persistence;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Syllanote.Infrastructure.Backups;

public sealed class SqliteLibraryRestoreService : ILibraryRestoreService
{
    private static readonly JsonSerializerOptions MetadataJsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

    private readonly SyllanoteDbContext _dbContext;
    private readonly ILibraryBackupService _backupService;
    private readonly ILibraryBackupValidator _backupValidator;

    public SqliteLibraryRestoreService(
        SyllanoteDbContext dbContext,
        ILibraryBackupService backupService,
        ILibraryBackupValidator backupValidator)
    {
        _dbContext = dbContext;
        _backupService = backupService;
        _backupValidator = backupValidator;
    }

    public async Task<LibraryRestorePreparationResult> PrepareAsync(
        string backupPath,
        CancellationToken cancellationToken = default)
    {
        var validation = await _backupValidator.ValidateAsync(
            backupPath,
            cancellationToken);
        var liveDatabasePath = GetLiveDatabasePath();
        var databaseDirectory = Path.GetDirectoryName(liveDatabasePath)!;
        var safetyBackupDirectory = Path.Combine(
            databaseDirectory,
            LibraryRestorePolicy.SafetyBackupDirectoryName);
        var preparedAtUtc = DateTimeOffset.UtcNow;

        Directory.CreateDirectory(safetyBackupDirectory);

        var safetyBackupPath = CreateSafetyBackupPath(
            safetyBackupDirectory,
            preparedAtUtc);
        var safetyBackup = await _backupService.CreateAsync(
            safetyBackupPath,
            cancellationToken);
        PruneSafetyBackups(
            safetyBackupDirectory,
            safetyBackup.FilePath);

        var pendingDatabasePath = LibraryRestorePaths
            .GetPendingDatabasePath(liveDatabasePath);
        var pendingMetadataPath = LibraryRestorePaths
            .GetPendingMetadataPath(liveDatabasePath);
        var temporaryPendingPath = Path.Combine(
            databaseDirectory,
            $".{Path.GetFileName(pendingDatabasePath)}." +
            $"{Guid.NewGuid():N}.tmp");
        var temporaryMetadataPath = Path.Combine(
            databaseDirectory,
            $".{Path.GetFileName(pendingMetadataPath)}." +
            $"{Guid.NewGuid():N}.tmp");

        try
        {
            await ExtractPendingDatabaseAsync(
                validation.FilePath,
                temporaryPendingPath,
                cancellationToken);
            await VerifyPendingDatabaseHashAsync(
                temporaryPendingPath,
                validation.Manifest.DatabaseSha256,
                cancellationToken);
            var metadata = new LibraryRestorePendingMetadata(
                validation.FilePath,
                safetyBackup.FilePath,
                preparedAtUtc,
                validation.Manifest);
            await WriteMetadataAsync(
                temporaryMetadataPath,
                metadata,
                cancellationToken);

            DeleteFileIfPresent(pendingMetadataPath);
            File.Move(
                temporaryPendingPath,
                pendingDatabasePath,
                overwrite: true);
            File.Move(
                temporaryMetadataPath,
                pendingMetadataPath,
                overwrite: true);

            return new LibraryRestorePreparationResult(
                validation.FilePath,
                safetyBackup.FilePath,
                pendingDatabasePath,
                pendingMetadataPath,
                preparedAtUtc,
                validation.Manifest);
        }
        finally
        {
            TryDeleteFile(temporaryPendingPath);
            TryDeleteFile(temporaryMetadataPath);
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

    private static string CreateSafetyBackupPath(
        string safetyBackupDirectory,
        DateTimeOffset createdAtUtc)
    {
        var fileName =
            $"Syllanote-before-restore-" +
            $"{createdAtUtc:yyyyMMdd-HHmmssfff}Z-" +
            $"{Guid.NewGuid():N}" +
            LibraryBackupFormat.FileExtension;
        return Path.Combine(safetyBackupDirectory, fileName);
    }

    private static void PruneSafetyBackups(
        string safetyBackupDirectory,
        string newestBackupPath)
    {
        var olderBackups = Directory
            .EnumerateFiles(
                safetyBackupDirectory,
                $"*{LibraryBackupFormat.FileExtension}",
                SearchOption.TopDirectoryOnly)
            .Where(path => !string.Equals(
                Path.GetFullPath(path),
                Path.GetFullPath(newestBackupPath),
                StringComparison.OrdinalIgnoreCase))
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ThenByDescending(file => file.Name, StringComparer.Ordinal)
            .ToArray();

        var olderBackupsToKeep =
            LibraryRestorePolicy.SafetyBackupRetentionLimit - 1;
        foreach (var backup in olderBackups.Skip(olderBackupsToKeep))
        {
            backup.Delete();
        }
    }

    private static async Task ExtractPendingDatabaseAsync(
        string backupPath,
        string destinationPath,
        CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(backupPath);
        var databaseEntry = archive.GetEntry(
            LibraryBackupFormat.DatabaseEntryName) ??
            throw new InvalidLibraryBackupException(
                "The backup database is missing.");

        await using var source = databaseEntry.Open();
        await using var destination = new FileStream(
            destinationPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);
        await source.CopyToAsync(destination, cancellationToken);
    }

    private static async Task VerifyPendingDatabaseHashAsync(
        string databasePath,
        string expectedHash,
        CancellationToken cancellationToken)
    {
        await using var databaseStream = File.OpenRead(databasePath);
        var actualHash = Convert.ToHexString(
            await SHA256.HashDataAsync(
                databaseStream,
                cancellationToken));

        if (!string.Equals(
                expectedHash,
                actualHash,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidLibraryBackupException(
                "The backup changed while restore was being prepared.");
        }
    }

    private static async Task WriteMetadataAsync(
        string metadataPath,
        LibraryRestorePendingMetadata metadata,
        CancellationToken cancellationToken)
    {
        await using var metadataStream = new FileStream(
            metadataPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);
        await JsonSerializer.SerializeAsync(
            metadataStream,
            metadata,
            MetadataJsonOptions,
            cancellationToken);
        await metadataStream.FlushAsync(cancellationToken);
    }

    private static void TryDeleteFile(string filePath)
    {
        try
        {
            DeleteFileIfPresent(filePath);
        }
        catch
        {
            // The restore preparation result must not be masked by cleanup errors.
        }
    }

    private static void DeleteFileIfPresent(string filePath)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
}
