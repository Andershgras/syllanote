using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Syllanote.Application.Backups;
using Syllanote.Infrastructure.Persistence;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Syllanote.Infrastructure.Backups;

public sealed class SqliteLibraryBackupValidator : ILibraryBackupValidator
{
    private const long MaximumManifestSizeInBytes = 64 * 1024;
    private static readonly JsonSerializerOptions ManifestJsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly SyllanoteDbContext _dbContext;

    public SqliteLibraryBackupValidator(SyllanoteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LibraryBackupValidationResult> ValidateAsync(
        string backupPath,
        CancellationToken cancellationToken = default)
    {
        var fullBackupPath = ValidateBackupPath(backupPath);
        var validationDirectory = Path.Combine(
            Path.GetTempPath(),
            "Syllanote",
            "BackupValidation",
            Guid.NewGuid().ToString("N"));
        var extractedDatabasePath = Path.Combine(
            validationDirectory,
            LibraryBackupFormat.DatabaseEntryName);

        Directory.CreateDirectory(validationDirectory);

        try
        {
            try
            {
                using var archive = ZipFile.OpenRead(fullBackupPath);
                var (manifestEntry, databaseEntry) =
                    GetRequiredEntries(archive);
                var manifest = await ReadManifestAsync(
                    manifestEntry,
                    cancellationToken);

                ValidateManifest(manifest, databaseEntry);
                await ExtractDatabaseAsync(
                    databaseEntry,
                    extractedDatabasePath,
                    cancellationToken);
                await ValidateDatabaseHashAsync(
                    extractedDatabasePath,
                    manifest.DatabaseSha256,
                    cancellationToken);
                await ValidateDatabaseAsync(
                    extractedDatabasePath,
                    manifest,
                    cancellationToken);

                return new LibraryBackupValidationResult(
                    fullBackupPath,
                    new FileInfo(fullBackupPath).Length,
                    manifest);
            }
            catch (InvalidLibraryBackupException)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is InvalidDataException or
                JsonException or
                SqliteException or
                FormatException)
            {
                throw new InvalidLibraryBackupException(
                    "The selected file is not a valid Syllanote backup.",
                    exception);
            }
        }
        finally
        {
            TryDeleteDirectory(validationDirectory);
        }
    }

    private static string ValidateBackupPath(string backupPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(backupPath);

        if (!backupPath.EndsWith(
                LibraryBackupFormat.FileExtension,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidLibraryBackupException(
                $"Select a '{LibraryBackupFormat.FileExtension}' file.");
        }

        var fullBackupPath = Path.GetFullPath(backupPath);
        if (!File.Exists(fullBackupPath))
        {
            throw new FileNotFoundException(
                "The selected backup file could not be found.",
                fullBackupPath);
        }

        return fullBackupPath;
    }

    private static (ZipArchiveEntry Manifest, ZipArchiveEntry Database)
        GetRequiredEntries(ZipArchive archive)
    {
        var manifestEntries = archive.Entries
            .Where(entry => entry.FullName ==
                LibraryBackupFormat.ManifestEntryName)
            .ToArray();
        var databaseEntries = archive.Entries
            .Where(entry => entry.FullName ==
                LibraryBackupFormat.DatabaseEntryName)
            .ToArray();

        if (archive.Entries.Count != 2 ||
            manifestEntries.Length != 1 ||
            databaseEntries.Length != 1)
        {
            throw new InvalidLibraryBackupException(
                "The backup does not contain the expected Syllanote files.");
        }

        return (manifestEntries[0], databaseEntries[0]);
    }

    private static async Task<LibraryBackupManifest> ReadManifestAsync(
        ZipArchiveEntry manifestEntry,
        CancellationToken cancellationToken)
    {
        if (manifestEntry.Length <= 0 ||
            manifestEntry.Length > MaximumManifestSizeInBytes)
        {
            throw new InvalidLibraryBackupException(
                "The backup manifest has an invalid size.");
        }

        await using var manifestStream = manifestEntry.Open();
        var manifest = await JsonSerializer.DeserializeAsync<
            LibraryBackupManifest>(
            manifestStream,
            ManifestJsonOptions,
            cancellationToken);

        return manifest ?? throw new InvalidLibraryBackupException(
            "The backup manifest is missing or invalid.");
    }

    private static void ValidateManifest(
        LibraryBackupManifest manifest,
        ZipArchiveEntry databaseEntry)
    {
        if (manifest.Product != LibraryBackupFormat.ProductName)
        {
            throw new InvalidLibraryBackupException(
                "The selected file was not created by Syllanote.");
        }

        if (manifest.FormatVersion > LibraryBackupFormat.CurrentVersion)
        {
            throw new InvalidLibraryBackupException(
                "This backup was created by a newer version of Syllanote.");
        }

        if (manifest.FormatVersion != LibraryBackupFormat.CurrentVersion)
        {
            throw new InvalidLibraryBackupException(
                "This backup format is not supported by this version of " +
                "Syllanote.");
        }

        if (manifest.DatabaseEntry !=
                LibraryBackupFormat.DatabaseEntryName ||
            manifest.DatabaseSizeInBytes <= 0 ||
            manifest.DatabaseSizeInBytes != databaseEntry.Length ||
            string.IsNullOrWhiteSpace(manifest.DatabaseSha256) ||
            manifest.AppliedMigrations is null)
        {
            throw new InvalidLibraryBackupException(
                "The backup manifest does not match its database.");
        }
    }

    private static async Task ExtractDatabaseAsync(
        ZipArchiveEntry databaseEntry,
        string destinationPath,
        CancellationToken cancellationToken)
    {
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

    private static async Task ValidateDatabaseHashAsync(
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
                "The backup database checksum does not match its manifest.");
        }
    }

    private async Task ValidateDatabaseAsync(
        string databasePath,
        LibraryBackupManifest manifest,
        CancellationToken cancellationToken)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString();
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await ValidateIntegrityAsync(connection, cancellationToken);
        await ValidateForeignKeysAsync(connection, cancellationToken);

        var databaseMigrations = await ReadDatabaseMigrationsAsync(
            connection,
            cancellationToken);
        if (!databaseMigrations.SequenceEqual(manifest.AppliedMigrations))
        {
            throw new InvalidLibraryBackupException(
                "The backup migration history does not match its manifest.");
        }

        var knownMigrations = _dbContext.Database
            .GetMigrations()
            .ToHashSet(StringComparer.Ordinal);
        if (databaseMigrations.Any(migration =>
                !knownMigrations.Contains(migration)))
        {
            throw new InvalidLibraryBackupException(
                "This backup requires a newer version of Syllanote.");
        }
    }

    private static async Task ValidateIntegrityAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA integrity_check;";
        var result = await command.ExecuteScalarAsync(cancellationToken);

        if (!string.Equals(result as string, "ok",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidLibraryBackupException(
                "The backup database failed SQLite's integrity check.");
        }
    }

    private static async Task ValidateForeignKeysAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA foreign_key_check;";
        await using var reader = await command.ExecuteReaderAsync(
            cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            throw new InvalidLibraryBackupException(
                "The backup database contains invalid relationships.");
        }
    }

    private static async Task<string[]> ReadDatabaseMigrationsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT MigrationId FROM __EFMigrationsHistory " +
            "ORDER BY MigrationId;";
        await using var reader = await command.ExecuteReaderAsync(
            cancellationToken);
        var migrations = new List<string>();

        while (await reader.ReadAsync(cancellationToken))
        {
            migrations.Add(reader.GetString(0));
        }

        return [.. migrations];
    }

    private static void TryDeleteDirectory(string directoryPath)
    {
        try
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, recursive: true);
            }
        }
        catch
        {
            // Validation results must not be masked by temporary cleanup errors.
        }
    }
}
