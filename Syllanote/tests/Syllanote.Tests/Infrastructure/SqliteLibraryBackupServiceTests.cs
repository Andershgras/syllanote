using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Syllanote.Application.Backups;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Backups;
using Syllanote.Infrastructure.Persistence;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Syllanote.Tests.Infrastructure;

[TestClass]
public class SqliteLibraryBackupServiceTests
{
    [TestMethod]
    public async Task CreateAsync_CreatesVersionedArchiveWithCompleteSnapshot()
    {
        var testDirectory = CreateTestDirectory();

        try
        {
            var sourcePath = Path.Combine(testDirectory, "source.db");
            var backupPath = Path.Combine(
                testDirectory,
                $"notes{LibraryBackupFormat.FileExtension}");
            var extractedDatabasePath = Path.Combine(
                testDirectory,
                "extracted.db");

            await using var sourceContext = CreateContext(sourcePath);
            await sourceContext.Database.MigrateAsync();
            await AddRepresentativeLibraryAsync(sourceContext);

            var service = new SqliteLibraryBackupService(sourceContext);
            var result = await service.CreateAsync(backupPath);

            Assert.AreEqual(Path.GetFullPath(backupPath), result.FilePath);
            Assert.AreEqual(
                new FileInfo(backupPath).Length,
                result.SizeInBytes);
            Assert.IsTrue(result.SizeInBytes > 0);

            using var archive = ZipFile.OpenRead(backupPath);
            var manifestEntry = archive.GetEntry(
                LibraryBackupFormat.ManifestEntryName);
            var databaseEntry = archive.GetEntry(
                LibraryBackupFormat.DatabaseEntryName);

            Assert.IsNotNull(manifestEntry);
            Assert.IsNotNull(databaseEntry);

            await using var manifestStream = manifestEntry.Open();
            var manifest = await JsonSerializer.DeserializeAsync<
                LibraryBackupManifest>(
                manifestStream,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            Assert.IsNotNull(manifest);
            Assert.AreEqual(
                LibraryBackupFormat.ProductName,
                manifest.Product);
            Assert.AreEqual(
                LibraryBackupFormat.CurrentVersion,
                manifest.FormatVersion);
            Assert.AreEqual(
                LibraryBackupFormat.DatabaseEntryName,
                manifest.DatabaseEntry);
            Assert.AreEqual(databaseEntry.Length, manifest.DatabaseSizeInBytes);
            Assert.IsTrue(manifest.AppliedMigrations.Length > 0);

            await using (var databaseStream = databaseEntry.Open())
            {
                var hash = Convert.ToHexString(
                    await SHA256.HashDataAsync(databaseStream));
                Assert.AreEqual(manifest.DatabaseSha256, hash);
            }

            databaseEntry.ExtractToFile(extractedDatabasePath);
            await AssertRepresentativeLibraryAsync(extractedDatabasePath);
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    [TestMethod]
    public async Task CreateAsync_WhenDestinationExists_ReplacesIt()
    {
        var testDirectory = CreateTestDirectory();

        try
        {
            var sourcePath = Path.Combine(testDirectory, "source.db");
            var backupPath = Path.Combine(
                testDirectory,
                $"notes{LibraryBackupFormat.FileExtension}");
            await File.WriteAllTextAsync(backupPath, "old backup");

            await using var sourceContext = CreateContext(sourcePath);
            await sourceContext.Database.MigrateAsync();
            var service = new SqliteLibraryBackupService(sourceContext);

            await service.CreateAsync(backupPath);

            using var archive = ZipFile.OpenRead(backupPath);
            Assert.IsNotNull(archive.GetEntry(
                LibraryBackupFormat.ManifestEntryName));
            Assert.IsNotNull(archive.GetEntry(
                LibraryBackupFormat.DatabaseEntryName));
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    [TestMethod]
    public async Task CreateAsync_WithDifferentExtension_RejectsDestination()
    {
        var testDirectory = CreateTestDirectory();

        try
        {
            var sourcePath = Path.Combine(testDirectory, "source.db");
            var backupPath = Path.Combine(testDirectory, "notes.zip");
            await using var sourceContext = CreateContext(sourcePath);
            var service = new SqliteLibraryBackupService(sourceContext);

            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => service.CreateAsync(backupPath));

            Assert.IsFalse(File.Exists(backupPath));
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
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

    private static async Task AddRepresentativeLibraryAsync(
        SyllanoteDbContext context)
    {
        var notebook = new Notebook("Algorithms", 2);
        var section = new Section(notebook.Id, "Graphs", 1);
        var page = new Page(section.Id, "Breadth-first search", 3);
        page.UpdateContent(
            "BFS visits neighbouring nodes first.",
            @"{\rtf1 BFS visits \b neighbouring\b0 nodes first.}");
        var concept = new Concept(
            notebook.Id,
            "Breadth-first search",
            "A graph traversal algorithm.");

        context.AddRange(notebook, section, page, concept);
        await context.SaveChangesAsync();
    }

    private static async Task AssertRepresentativeLibraryAsync(
        string databasePath)
    {
        await using var context = CreateContext(databasePath);

        Assert.AreEqual(1, await context.Notebooks.CountAsync());
        Assert.AreEqual(1, await context.Sections.CountAsync());
        Assert.AreEqual(1, await context.Pages.CountAsync());
        Assert.AreEqual(1, await context.Concepts.CountAsync());

        var notebook = await context.Notebooks.SingleAsync();
        var section = await context.Sections.SingleAsync();
        var page = await context.Pages.SingleAsync();
        var concept = await context.Concepts.SingleAsync();

        Assert.AreEqual("Algorithms", notebook.Name);
        Assert.AreEqual(2, notebook.SortOrder);
        Assert.AreEqual("Graphs", section.Name);
        Assert.AreEqual(1, section.SortOrder);
        Assert.AreEqual("Breadth-first search", page.Title);
        Assert.AreEqual(3, page.SortOrder);
        Assert.AreEqual(
            "BFS visits neighbouring nodes first.",
            page.Content);
        Assert.AreEqual(
            @"{\rtf1 BFS visits \b neighbouring\b0 nodes first.}",
            page.FormattedContent);
        Assert.AreEqual("Breadth-first search", concept.Name);
        Assert.AreEqual(
            "A graph traversal algorithm.",
            concept.Definition);
    }

    private static string CreateTestDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            $"syllanote-backup-tests-{Guid.NewGuid():N}");
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
