using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Syllanote.Application.Backups;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Backups;
using Syllanote.Infrastructure.Persistence;
using System.IO.Compression;

namespace Syllanote.Tests.Infrastructure;

[TestClass]
public class SqliteLibraryRestoreServiceTests
{
    [TestMethod]
    public async Task PrepareAsync_WithValidBackup_CreatesSafetyAndPendingCopies()
    {
        var testDirectory = CreateTestDirectory();

        try
        {
            var backupPath = await CreateBackupAsync(
                testDirectory,
                "Restored library");
            var liveDatabasePath = Path.Combine(testDirectory, "live.db");
            await using var liveContext = CreateContext(liveDatabasePath);
            await AddNotebookAsync(liveContext, "Current library");
            var service = CreateRestoreService(liveContext);

            var result = await service.PrepareAsync(backupPath);

            Assert.IsTrue(File.Exists(result.SafetyBackupPath));
            Assert.IsTrue(File.Exists(result.PendingDatabasePath));
            Assert.AreEqual(
                "Current library",
                (await liveContext.Notebooks.SingleAsync()).Name);
            Assert.AreEqual(
                "Restored library",
                await ReadSingleNotebookNameAsync(
                    result.PendingDatabasePath));

            var extractedSafetyPath = Path.Combine(
                testDirectory,
                "safety.db");
            ExtractDatabase(result.SafetyBackupPath, extractedSafetyPath);
            Assert.AreEqual(
                "Current library",
                await ReadSingleNotebookNameAsync(extractedSafetyPath));
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    [TestMethod]
    public async Task PrepareAsync_AfterFourAttempts_KeepsThreeSafetyBackups()
    {
        var testDirectory = CreateTestDirectory();

        try
        {
            var backupPath = await CreateBackupAsync(
                testDirectory,
                "Restored library");
            var liveDatabasePath = Path.Combine(testDirectory, "live.db");
            await using var liveContext = CreateContext(liveDatabasePath);
            await AddNotebookAsync(liveContext, "Current library");
            var service = CreateRestoreService(liveContext);
            LibraryRestorePreparationResult? latestResult = null;

            for (var attempt = 0; attempt < 4; attempt++)
            {
                latestResult = await service.PrepareAsync(backupPath);
            }

            var safetyBackupDirectory = Path.Combine(
                testDirectory,
                LibraryRestorePolicy.SafetyBackupDirectoryName);
            var safetyBackups = Directory.GetFiles(
                safetyBackupDirectory,
                $"*{LibraryBackupFormat.FileExtension}");

            Assert.AreEqual(
                LibraryRestorePolicy.SafetyBackupRetentionLimit,
                safetyBackups.Length);
            Assert.IsNotNull(latestResult);
            Assert.IsTrue(File.Exists(latestResult.SafetyBackupPath));
            Assert.AreEqual(1, Directory.GetFiles(
                testDirectory,
                $"*{LibraryRestorePolicy.PendingRestoreMarker}*.db").Length);
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    [TestMethod]
    public async Task PrepareAsync_WithInvalidBackup_DoesNotCreateRestoreFiles()
    {
        var testDirectory = CreateTestDirectory();

        try
        {
            var invalidBackupPath = Path.Combine(
                testDirectory,
                $"invalid{LibraryBackupFormat.FileExtension}");
            await File.WriteAllTextAsync(
                invalidBackupPath,
                "not a Syllanote backup");
            var liveDatabasePath = Path.Combine(testDirectory, "live.db");
            await using var liveContext = CreateContext(liveDatabasePath);
            await AddNotebookAsync(liveContext, "Current library");
            var service = CreateRestoreService(liveContext);

            await Assert.ThrowsExceptionAsync<InvalidLibraryBackupException>(
                () => service.PrepareAsync(invalidBackupPath));

            Assert.IsFalse(Directory.Exists(Path.Combine(
                testDirectory,
                LibraryRestorePolicy.SafetyBackupDirectoryName)));
            Assert.AreEqual(0, Directory.GetFiles(
                testDirectory,
                $"*{LibraryRestorePolicy.PendingRestoreMarker}*.db").Length);
            Assert.AreEqual(
                "Current library",
                (await liveContext.Notebooks.SingleAsync()).Name);
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    private static SqliteLibraryRestoreService CreateRestoreService(
        SyllanoteDbContext context)
    {
        var backupService = new SqliteLibraryBackupService(context);
        var validator = new SqliteLibraryBackupValidator(context);
        return new SqliteLibraryRestoreService(
            context,
            backupService,
            validator);
    }

    private static async Task<string> CreateBackupAsync(
        string testDirectory,
        string notebookName)
    {
        var sourcePath = Path.Combine(
            testDirectory,
            $"source-{Guid.NewGuid():N}.db");
        var backupPath = Path.Combine(
            testDirectory,
            $"source-{Guid.NewGuid():N}" +
            LibraryBackupFormat.FileExtension);
        await using var context = CreateContext(sourcePath);
        await AddNotebookAsync(context, notebookName);
        var backupService = new SqliteLibraryBackupService(context);
        await backupService.CreateAsync(backupPath);
        return backupPath;
    }

    private static async Task AddNotebookAsync(
        SyllanoteDbContext context,
        string notebookName)
    {
        await context.Database.MigrateAsync();
        context.Notebooks.Add(new Notebook(notebookName));
        await context.SaveChangesAsync();
    }

    private static async Task<string> ReadSingleNotebookNameAsync(
        string databasePath)
    {
        await using var context = CreateContext(databasePath);
        return (await context.Notebooks.SingleAsync()).Name;
    }

    private static void ExtractDatabase(
        string backupPath,
        string destinationPath)
    {
        using var archive = ZipFile.OpenRead(backupPath);
        archive.GetEntry(LibraryBackupFormat.DatabaseEntryName)!
            .ExtractToFile(destinationPath);
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
            $"syllanote-restore-tests-{Guid.NewGuid():N}");
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
