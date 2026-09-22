using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Syllanote.Application.Notebooks.Sections.MoveSection;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Persistence;
using Syllanote.Infrastructure.Repositories;

namespace Syllanote.Tests.Infrastructure;

[TestClass]
public class SectionOrderingPersistenceSqliteTests
{
    [TestMethod]
    public async Task MoveUpAsync_PersistsSectionOrderWithinNotebook()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SyllanoteDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new SyllanoteDbContext(options);
        await context.Database.MigrateAsync();

        var notebook = new Notebook("Notebook");
        var first = new Section(notebook.Id, "First", 0);
        var second = new Section(notebook.Id, "Second", 1);
        context.Notebooks.Add(notebook);
        context.Sections.AddRange(first, second);
        await context.SaveChangesAsync();

        var repository = new SectionRepository(context);
        var service = new MoveSectionService(repository);
        await service.MoveUpAsync(second);
        context.ChangeTracker.Clear();

        var sections = await repository.GetByNotebookIdAsync(notebook.Id);

        Assert.AreEqual(second.Id, sections[0].Id);
        Assert.AreEqual(first.Id, sections[1].Id);
    }
}
