using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Syllanote.Application.Notebooks.Sections.Pages.MovePage;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Persistence;
using Syllanote.Infrastructure.Repositories;

namespace Syllanote.Tests.Infrastructure;

[TestClass]
public class PageOrderingPersistenceSqliteTests
{
    [TestMethod]
    public async Task MoveUpAsync_PersistsPageOrderWithinSection()
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
        var first = new Page(section.Id, "First", 0);
        var second = new Page(section.Id, "Second", 1);
        context.Notebooks.Add(notebook);
        context.Sections.Add(section);
        context.Pages.AddRange(first, second);
        await context.SaveChangesAsync();

        var repository = new PageRepository(context);
        var service = new MovePageService(repository);
        await service.MoveUpAsync(second);
        context.ChangeTracker.Clear();

        var pages = await repository.GetBySectionIdAsync(section.Id);

        Assert.AreEqual(second.Id, pages[0].Id);
        Assert.AreEqual(first.Id, pages[1].Id);
    }
}
