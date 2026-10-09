using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Persistence;
using Syllanote.Infrastructure.Repositories;

namespace Syllanote.Tests.Infrastructure;

[TestClass]
public class SectionColorPersistenceSqliteTests
{
    [TestMethod]
    public async Task UpdateAsync_PersistsSectionColor()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SyllanoteDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new SyllanoteDbContext(options);
        await context.Database.MigrateAsync();

        var notebook = new Notebook("Notebook");
        var section = new Section(notebook.Id, "Section");
        context.AddRange(notebook, section);
        await context.SaveChangesAsync();

        section.ChangeColor(SectionColorKeys.Blue);
        var repository = new SectionRepository(context);
        await repository.UpdateAsync(section);
        context.ChangeTracker.Clear();

        var persistedSection = (await repository.GetByNotebookIdAsync(
            notebook.Id)).Single();

        Assert.AreEqual(SectionColorKeys.Blue, persistedSection.ColorKey);
    }

    [TestMethod]
    public async Task LatestMigration_UpgradesExistingSectionWithNoColor()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SyllanoteDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new SyllanoteDbContext(options);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync("20260922232639_AddPageSortOrder");

        var notebookId = Guid.NewGuid();
        var sectionId = Guid.NewGuid();
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Notebooks (Id, Name, SortOrder, CreatedAt)
            VALUES ({notebookId}, {"Notebook"}, {0}, {DateTime.UtcNow});
            """);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO Sections (Id, NotebookId, Name, SortOrder, CreatedAt)
            VALUES ({sectionId}, {notebookId}, {"Existing section"}, {0}, {DateTime.UtcNow});
            """);

        await migrator.MigrateAsync();
        context.ChangeTracker.Clear();

        var section = await context.Sections.SingleAsync();

        Assert.AreEqual(sectionId, section.Id);
        Assert.IsNull(section.ColorKey);
    }
}
