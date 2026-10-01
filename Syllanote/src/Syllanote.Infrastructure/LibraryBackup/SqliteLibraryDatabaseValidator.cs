using Microsoft.Data.Sqlite;
using Syllanote.Application.Backups;
using System.Security.Cryptography;

namespace Syllanote.Infrastructure.Backups;

internal static class SqliteLibraryDatabaseValidator
{
    public static async Task ValidateAsync(
        string databasePath,
        LibraryBackupManifest manifest,
        IReadOnlySet<string> knownMigrations,
        CancellationToken cancellationToken)
    {
        ValidateManifest(manifest);

        var databaseFile = new FileInfo(databasePath);
        if (!databaseFile.Exists ||
            databaseFile.Length != manifest.DatabaseSizeInBytes)
        {
            throw new InvalidLibraryBackupException(
                "The backup database size does not match its manifest.");
        }

        await ValidateHashAsync(
            databasePath,
            manifest.DatabaseSha256,
            cancellationToken);

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

        if (databaseMigrations.Any(migration =>
                !knownMigrations.Contains(migration)))
        {
            throw new InvalidLibraryBackupException(
                "This backup requires a newer version of Syllanote.");
        }
    }

    private static void ValidateManifest(LibraryBackupManifest manifest)
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
            string.IsNullOrWhiteSpace(manifest.DatabaseSha256) ||
            manifest.AppliedMigrations is null)
        {
            throw new InvalidLibraryBackupException(
                "The backup manifest does not match its database.");
        }
    }

    private static async Task ValidateHashAsync(
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
}
