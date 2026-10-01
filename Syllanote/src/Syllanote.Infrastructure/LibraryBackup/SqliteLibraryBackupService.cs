using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Syllanote.Application.Backups;
using Syllanote.Infrastructure.Persistence;
using System.Data;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Syllanote.Infrastructure.Backups;

public sealed class SqliteLibraryBackupService : ILibraryBackupService
{
    private static readonly JsonSerializerOptions ManifestJsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

    private readonly SyllanoteDbContext _dbContext;

    public SqliteLibraryBackupService(SyllanoteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LibraryBackupResult> CreateAsync(
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var fullDestinationPath = ValidateDestinationPath(destinationPath);
        var destinationDirectory = Path.GetDirectoryName(fullDestinationPath)!;
        var workDirectory = Path.Combine(
            Path.GetTempPath(),
            "Syllanote",
            "Backups",
            Guid.NewGuid().ToString("N"));
        var snapshotPath = Path.Combine(
            workDirectory,
            LibraryBackupFormat.DatabaseEntryName);
        var temporaryArchivePath = Path.Combine(
            destinationDirectory,
            $".{Path.GetFileName(fullDestinationPath)}.{Guid.NewGuid():N}.tmp");

        Directory.CreateDirectory(workDirectory);

        try
        {
            var appliedMigrations = (await _dbContext.Database
                .GetAppliedMigrationsAsync(cancellationToken))
                .ToArray();

            await CreateDatabaseSnapshotAsync(
                snapshotPath,
                cancellationToken);

            var databaseInfo = new FileInfo(snapshotPath);
            var databaseSha256 = await CalculateSha256Async(
                snapshotPath,
                cancellationToken);
            var createdAtUtc = DateTimeOffset.UtcNow;
            var manifest = new LibraryBackupManifest(
                LibraryBackupFormat.ProductName,
                LibraryBackupFormat.CurrentVersion,
                createdAtUtc,
                LibraryBackupFormat.DatabaseEntryName,
                databaseInfo.Length,
                databaseSha256,
                appliedMigrations);

            await CreateArchiveAsync(
                temporaryArchivePath,
                snapshotPath,
                manifest,
                cancellationToken);

            MoveCompletedArchive(
                temporaryArchivePath,
                fullDestinationPath);

            return new LibraryBackupResult(
                fullDestinationPath,
                new FileInfo(fullDestinationPath).Length,
                createdAtUtc);
        }
        finally
        {
            TryDeleteFile(temporaryArchivePath);
            TryDeleteDirectory(workDirectory);
        }
    }

    private string ValidateDestinationPath(string destinationPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPath);

        if (!destinationPath.EndsWith(
                LibraryBackupFormat.FileExtension,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                $"Backup files must use the " +
                $"'{LibraryBackupFormat.FileExtension}' extension.",
                nameof(destinationPath));
        }

        var fullDestinationPath = Path.GetFullPath(destinationPath);
        var destinationDirectory = Path.GetDirectoryName(fullDestinationPath);
        if (string.IsNullOrEmpty(destinationDirectory) ||
            !Directory.Exists(destinationDirectory))
        {
            throw new DirectoryNotFoundException(
                "The selected backup folder does not exist.");
        }

        if (_dbContext.Database.GetDbConnection() is not
            SqliteConnection connection)
        {
            throw new InvalidOperationException(
                "The configured database is not a SQLite database.");
        }

        if (!string.IsNullOrWhiteSpace(connection.DataSource) &&
            connection.DataSource != ":memory:" &&
            string.Equals(
                Path.GetFullPath(connection.DataSource),
                fullDestinationPath,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "The live database cannot be used as the backup destination.",
                nameof(destinationPath));
        }

        return fullDestinationPath;
    }

    private async Task CreateDatabaseSnapshotAsync(
        string snapshotPath,
        CancellationToken cancellationToken)
    {
        if (_dbContext.Database.GetDbConnection() is not
            SqliteConnection sourceConnection)
        {
            throw new InvalidOperationException(
                "The configured database is not a SQLite database.");
        }

        var sourceWasOpen = sourceConnection.State == ConnectionState.Open;
        if (!sourceWasOpen)
        {
            await sourceConnection.OpenAsync(cancellationToken);
        }

        try
        {
            var destinationConnectionString =
                new SqliteConnectionStringBuilder
                {
                    DataSource = snapshotPath,
                    Pooling = false
                }.ToString();
            await using var destinationConnection = new SqliteConnection(
                destinationConnectionString);
            await destinationConnection.OpenAsync(cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            sourceConnection.BackupDatabase(destinationConnection);
        }
        finally
        {
            if (!sourceWasOpen)
            {
                await sourceConnection.CloseAsync();
            }
        }
    }

    private static async Task<string> CalculateSha256Async(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }

    private static async Task CreateArchiveAsync(
        string archivePath,
        string snapshotPath,
        LibraryBackupManifest manifest,
        CancellationToken cancellationToken)
    {
        using var archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);

        var manifestEntry = archive.CreateEntry(
            LibraryBackupFormat.ManifestEntryName,
            CompressionLevel.Optimal);
        await using (var manifestStream = manifestEntry.Open())
        {
            await JsonSerializer.SerializeAsync(
                manifestStream,
                manifest,
                ManifestJsonOptions,
                cancellationToken);
        }

        var databaseEntry = archive.CreateEntry(
            LibraryBackupFormat.DatabaseEntryName,
            CompressionLevel.Optimal);
        await using var databaseEntryStream = databaseEntry.Open();
        await using var databaseStream = File.OpenRead(snapshotPath);
        await databaseStream.CopyToAsync(
            databaseEntryStream,
            cancellationToken);
    }

    private static void MoveCompletedArchive(
        string temporaryArchivePath,
        string destinationPath)
    {
        if (File.Exists(destinationPath))
        {
            File.Replace(temporaryArchivePath, destinationPath, null);
            return;
        }

        File.Move(temporaryArchivePath, destinationPath);
    }

    private static void TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // A completed or failed backup must not be masked by cleanup errors.
        }
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
            // A completed or failed backup must not be masked by cleanup errors.
        }
    }
}
