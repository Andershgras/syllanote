using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Syllanote.Application.Notebooks.MoveNotebook;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Persistence;
using Syllanote.Infrastructure.Repositories;

namespace Syllanote.Tests.Infrastructure;

[TestClass]
public class NotebookOrderingPersistenceSqliteTests
{
    [TestMethod]
    public async Task MoveUpAsync_PersistsNotebookOrder()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SyllanoteDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new SyllanoteDbContext(options);
        await context.Database.MigrateAsync();

        var first = new Notebook("First", 0);
        var second = new Notebook("Second", 1);
        context.Notebooks.AddRange(first, second);
        await context.SaveChangesAsync();

        var repository = new NotebookRepository(context);
        var service = new MoveNotebookService(repository);
        await service.MoveUpAsync(second);
        context.ChangeTracker.Clear();

        var notebooks = await repository.GetAllAsync();

        Assert.AreEqual(second.Id, notebooks[0].Id);
        Assert.AreEqual(first.Id, notebooks[1].Id);
    }
}
