using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Syllanote.Application.Backups;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Backups;
using Syllanote.Infrastructure.Persistence;
using System.IO.Compression;
using System.Text.Json;

namespace Syllanote.Tests.Infrastructure;

[TestClass]
public class SqliteLibraryBackupValidatorTests
{
    private static readonly JsonSerializerOptions ManifestJsonOptions =
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

    [TestMethod]
    public async Task ValidateAsync_WithCompleteBackup_ReturnsManifest()
    {
        var testDirectory = CreateTestDirectory();

        try
        {
            var (context, backupPath) = await CreateBackupAsync(testDirectory);
            await using (context)
            {
                var validator = new SqliteLibraryBackupValidator(context);

                var result = await validator.ValidateAsync(backupPath);

                Assert.AreEqual(Path.GetFullPath(backupPath), result.FilePath);
                Assert.AreEqual(
                    new FileInfo(backupPath).Length,
                    result.SizeInBytes);
                Assert.AreEqual(
                    LibraryBackupFormat.ProductName,
                    result.Manifest.Product);
                Assert.AreEqual(
                    LibraryBackupFormat.CurrentVersion,
                    result.Manifest.FormatVersion);
            }
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    [TestMethod]
    public async Task ValidateAsync_WithChangedDatabase_RejectsBackup()
    {
        var testDirectory = CreateTestDirectory();

        try
        {
            var (context, backupPath) = await CreateBackupAsync(testDirectory);
            await using (context)
            {
                using (var archive = ZipFile.Open(
                    backupPath,
                    ZipArchiveMode.Update))
                {
                    archive.GetEntry(LibraryBackupFormat.DatabaseEntryName)!
                        .Delete();
                    var changedDatabase = archive.CreateEntry(
                        LibraryBackupFormat.DatabaseEntryName);
                    await using var stream = changedDatabase.Open();
                    await stream.WriteAsync("changed"u8.ToArray());
                }

                var validator = new SqliteLibraryBackupValidator(context);

                await Assert.ThrowsExceptionAsync<
                    InvalidLibraryBackupException>(
                    () => validator.ValidateAsync(backupPath));

                Assert.AreEqual(1, await context.Notebooks.CountAsync());
            }
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    [TestMethod]
    public async Task ValidateAsync_WithNewerFormat_RejectsBackup()
    {
        var testDirectory = CreateTestDirectory();

        try
        {
            var (context, backupPath) = await CreateBackupAsync(testDirectory);
            await using (context)
            {
                await ReplaceManifestAsync(
                    backupPath,
                    manifest => manifest with
                    {
                        FormatVersion =
                            LibraryBackupFormat.CurrentVersion + 1
                    });
                var validator = new SqliteLibraryBackupValidator(context);

                var exception = await Assert.ThrowsExceptionAsync<
                    InvalidLibraryBackupException>(
                    () => validator.ValidateAsync(backupPath));

                StringAssert.Contains(exception.Message, "newer version");
            }
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    private static async Task<(SyllanoteDbContext Context, string BackupPath)>
        CreateBackupAsync(string testDirectory)
    {
        var sourcePath = Path.Combine(testDirectory, "source.db");
        var backupPath = Path.Combine(
            testDirectory,
            $"notes{LibraryBackupFormat.FileExtension}");
        var context = CreateContext(sourcePath);
        await context.Database.MigrateAsync();
        context.Notebooks.Add(new Notebook("Algorithms"));
        await context.SaveChangesAsync();

        var service = new SqliteLibraryBackupService(context);
        await service.CreateAsync(backupPath);
        return (context, backupPath);
    }

    private static async Task ReplaceManifestAsync(
        string backupPath,
        Func<LibraryBackupManifest, LibraryBackupManifest> transform)
    {
        using var archive = ZipFile.Open(backupPath, ZipArchiveMode.Update);
        var existingEntry = archive.GetEntry(
            LibraryBackupFormat.ManifestEntryName)!;
        LibraryBackupManifest manifest;
        await using (var stream = existingEntry.Open())
        {
            manifest = (await JsonSerializer.DeserializeAsync<
                LibraryBackupManifest>(
                stream,
                ManifestJsonOptions))!;
        }

        existingEntry.Delete();
        var replacementEntry = archive.CreateEntry(
            LibraryBackupFormat.ManifestEntryName,
            CompressionLevel.Optimal);
        await using var replacementStream = replacementEntry.Open();
        await JsonSerializer.SerializeAsync(
            replacementStream,
            transform(manifest),
            ManifestJsonOptions);
    }

    private static SyllanoteDbContext CreateContext(string databasePath)
    {
        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Pooling = false
        }.ToString();
        var options = new DbContextOptionsBuilder<SyllanoteDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new SyllanoteDbContext(options);
    }

    private static string CreateTestDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"syllanote-backup-validator-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void DeleteTestDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}
