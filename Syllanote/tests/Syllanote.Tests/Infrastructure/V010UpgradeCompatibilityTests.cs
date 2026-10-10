using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Syllanote.Application.Backups;
using Syllanote.Infrastructure.Backups;
using Syllanote.Infrastructure.Persistence;

namespace Syllanote.Tests.Infrastructure;

[TestClass]
public class V010UpgradeCompatibilityTests
{
    // This is the final migration contained in the published v0.1.0 tag.
    private const string V010LastMigration =
        "20260922232639_AddPageSortOrder";
    private const string V020SectionColorMigration =
        "20261009231947_AddSectionColorKey";

    [TestMethod]
    public async Task MigrateAsync_WithRepresentativeV010Library_PreservesData()
    {
        var testDirectory = CreateTestDirectory();

        try
        {
            var databasePath = Path.Combine(testDirectory, "v0.1.0.db");
            var fixture = await CreateV010LibraryAsync(databasePath);

            await using var context = CreateContext(databasePath);
            await new DatabaseMigrationService(context).MigrateAsync();

            await AssertUpgradedLibraryAsync(context, fixture);
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    [TestMethod]
    public async Task RestoreAsync_WithRepresentativeV010Backup_PreservesData()
    {
        var testDirectory = CreateTestDirectory();

        try
        {
            var sourcePath = Path.Combine(testDirectory, "v0.1.0-source.db");
            var backupPath = Path.Combine(
                testDirectory,
                $"v0.1.0{LibraryBackupFormat.FileExtension}");
            var liveDatabasePath = Path.Combine(testDirectory, "live.db");
            var fixture = await CreateV010LibraryAsync(sourcePath);

            await using (var sourceContext = CreateContext(sourcePath))
            {
                await new SqliteLibraryBackupService(sourceContext)
                    .CreateAsync(backupPath);
            }

            await using (var liveContext = CreateContext(liveDatabasePath))
            {
                await liveContext.Database.MigrateAsync();
                liveContext.Notebooks.Add(new("Library replaced by restore"));
                await liveContext.SaveChangesAsync();

                var backupService = new SqliteLibraryBackupService(
                    liveContext);
                var backupValidator = new SqliteLibraryBackupValidator(
                    liveContext);
                var restoreService = new SqliteLibraryRestoreService(
                    liveContext,
                    backupService,
                    backupValidator);

                var preparation = await restoreService.PrepareAsync(
                    backupPath);

                Assert.AreEqual(
                    V010LastMigration,
                    preparation.Manifest.AppliedMigrations[^1]);
                CollectionAssert.DoesNotContain(
                    preparation.Manifest.AppliedMigrations,
                    V020SectionColorMigration);
            }

            await using var startupContext = CreateContext(liveDatabasePath);
            var restoreApplied = await new PendingDatabaseRestoreService(
                startupContext).ApplyAsync();
            await new DatabaseMigrationService(startupContext).MigrateAsync();

            Assert.IsTrue(restoreApplied);
            await AssertUpgradedLibraryAsync(startupContext, fixture);
        }
        finally
        {
            DeleteTestDirectory(testDirectory);
        }
    }

    private static async Task<V010Fixture> CreateV010LibraryAsync(
        string databasePath)
    {
        var fixture = new V010Fixture(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());
        var createdAt = new DateTime(
            2026,
            9,
            30,
            12,
            0,
            0,
            DateTimeKind.Utc);
        var updatedAt = createdAt.AddHours(1);

        await using var context = CreateContext(databasePath);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(V010LastMigration);

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Notebooks (Id, Name, CreatedAt, SortOrder)
            VALUES ({fixture.PrimaryNotebookId}, {"Algorithms"},
                {createdAt}, {2});
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Notebooks (Id, Name, CreatedAt, SortOrder)
            VALUES ({fixture.SecondaryNotebookId}, {"Databases"},
                {createdAt}, {0});
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Sections (Id, NotebookId, Name, CreatedAt, SortOrder)
            VALUES ({fixture.PrimarySectionId},
                {fixture.PrimaryNotebookId}, {"Graphs"}, {createdAt}, {3});
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Sections (Id, NotebookId, Name, CreatedAt, SortOrder)
            VALUES ({fixture.SecondarySectionId},
                {fixture.PrimaryNotebookId}, {"Complexity"}, {createdAt}, {1});
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Pages
                (Id, SectionId, Title, Content, CreatedAt, UpdatedAt,
                    FormattedContent, SortOrder)
            VALUES ({fixture.FormattedPageId}, {fixture.PrimarySectionId},
                {"Breadth-first search"},
                {"BFS visits neighbouring nodes first."}, {createdAt},
                {updatedAt},
                {@"{\rtf1\ansi BFS visits \b neighbouring\b0 nodes first.}"},
                {4});
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Pages
                (Id, SectionId, Title, Content, CreatedAt, UpdatedAt,
                    FormattedContent, SortOrder)
            VALUES ({fixture.EmptyPageId}, {fixture.SecondarySectionId},
                {"Asymptotic notation"}, {string.Empty}, {createdAt},
                {createdAt}, {string.Empty}, {0});
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Concepts
                (Id, NotebookId, Name, NormalizedName, Definition, CreatedAt,
                    UpdatedAt)
            VALUES ({fixture.ConceptId}, {fixture.PrimaryNotebookId},
                {"Breadth-first search"}, {"BREADTH-FIRST SEARCH"},
                {"A graph traversal algorithm."}, {createdAt}, {updatedAt});
            """);

        return fixture;
    }

    private static async Task AssertUpgradedLibraryAsync(
        SyllanoteDbContext context,
        V010Fixture fixture)
    {
        context.ChangeTracker.Clear();

        var appliedMigrations = (await context.Database
            .GetAppliedMigrationsAsync()).ToList();
        CollectionAssert.Contains(appliedMigrations, V010LastMigration);
        CollectionAssert.Contains(
            appliedMigrations,
            V020SectionColorMigration);

        var notebooks = await context.Notebooks
            .OrderBy(notebook => notebook.SortOrder)
            .ToListAsync();
        var sections = await context.Sections
            .OrderBy(section => section.SortOrder)
            .ToListAsync();
        var pages = await context.Pages
            .OrderBy(page => page.SortOrder)
            .ToListAsync();
        var concepts = await context.Concepts.ToListAsync();

        Assert.AreEqual(2, notebooks.Count);
        Assert.AreEqual(fixture.SecondaryNotebookId, notebooks[0].Id);
        Assert.AreEqual("Databases", notebooks[0].Name);
        Assert.AreEqual(0, notebooks[0].SortOrder);
        Assert.AreEqual(fixture.PrimaryNotebookId, notebooks[1].Id);
        Assert.AreEqual("Algorithms", notebooks[1].Name);
        Assert.AreEqual(2, notebooks[1].SortOrder);

        Assert.AreEqual(2, sections.Count);
        Assert.AreEqual(fixture.SecondarySectionId, sections[0].Id);
        Assert.AreEqual(fixture.PrimaryNotebookId, sections[0].NotebookId);
        Assert.AreEqual("Complexity", sections[0].Name);
        Assert.AreEqual(1, sections[0].SortOrder);
        Assert.IsNull(sections[0].ColorKey);
        Assert.AreEqual(fixture.PrimarySectionId, sections[1].Id);
        Assert.AreEqual(fixture.PrimaryNotebookId, sections[1].NotebookId);
        Assert.AreEqual("Graphs", sections[1].Name);
        Assert.AreEqual(3, sections[1].SortOrder);
        Assert.IsNull(sections[1].ColorKey);

        Assert.AreEqual(2, pages.Count);
        Assert.AreEqual(fixture.EmptyPageId, pages[0].Id);
        Assert.AreEqual(fixture.SecondarySectionId, pages[0].SectionId);
        Assert.AreEqual("Asymptotic notation", pages[0].Title);
        Assert.AreEqual(string.Empty, pages[0].Content);
        Assert.AreEqual(string.Empty, pages[0].FormattedContent);
        Assert.AreEqual(0, pages[0].SortOrder);
        Assert.AreEqual(fixture.FormattedPageId, pages[1].Id);
        Assert.AreEqual(fixture.PrimarySectionId, pages[1].SectionId);
        Assert.AreEqual("Breadth-first search", pages[1].Title);
        Assert.AreEqual(
            "BFS visits neighbouring nodes first.",
            pages[1].Content);
        Assert.AreEqual(
            @"{\rtf1\ansi BFS visits \b neighbouring\b0 nodes first.}",
            pages[1].FormattedContent);
        Assert.AreEqual(4, pages[1].SortOrder);

        Assert.AreEqual(1, concepts.Count);
        Assert.AreEqual(fixture.ConceptId, concepts[0].Id);
        Assert.AreEqual(fixture.PrimaryNotebookId, concepts[0].NotebookId);
        Assert.AreEqual("Breadth-first search", concepts[0].Name);
        Assert.AreEqual("BREADTH-FIRST SEARCH", concepts[0].NormalizedName);
        Assert.AreEqual(
            "A graph traversal algorithm.",
            concepts[0].Definition);
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
            $"syllanote-v010-upgrade-tests-{Guid.NewGuid():N}");
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

    private sealed record V010Fixture(
        Guid PrimaryNotebookId,
        Guid SecondaryNotebookId,
        Guid PrimarySectionId,
        Guid SecondarySectionId,
        Guid FormattedPageId,
        Guid EmptyPageId,
        Guid ConceptId);
}
