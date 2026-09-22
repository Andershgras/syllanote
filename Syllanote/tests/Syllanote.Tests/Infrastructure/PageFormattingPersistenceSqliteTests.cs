using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Persistence;
using Syllanote.Infrastructure.Repositories;

namespace Syllanote.Tests.Infrastructure;

[TestClass]
public class PageFormattingPersistenceSqliteTests
{
    [TestMethod]
    public async Task Repository_PersistsPlainAndFormattedContent()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.MigrateAsync();

        var notebook = new Notebook("Software Design");
        var section = new Section(notebook.Id, "Principles");
        var page = new Page(section.Id, "SOLID");
        page.UpdateContent(
            "Single Responsibility Principle",
            @"{\rtf1 \b Single Responsibility Principle\b0}");

        context.Notebooks.Add(notebook);
        context.Sections.Add(section);
        await context.SaveChangesAsync();

        var repository = new PageRepository(context);
        await repository.AddAsync(page);
        context.ChangeTracker.Clear();

        var loadedPage = (await repository.GetBySectionIdAsync(section.Id)).Single();

        Assert.AreEqual(
            "Single Responsibility Principle",
            loadedPage.Content);
        Assert.AreEqual(
            @"{\rtf1 \b Single Responsibility Principle\b0}",
            loadedPage.FormattedContent);
    }

    private static SyllanoteDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<SyllanoteDbContext>()
            .UseSqlite(connection)
            .Options;

        return new SyllanoteDbContext(options);
    }
}
