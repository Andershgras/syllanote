using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Persistence;
using Syllanote.Infrastructure.Repositories;

namespace Syllanote.Tests.Infrastructure;

[TestClass]
public class ConceptPersistenceSqliteTests
{
    [TestMethod]
    public async Task Repository_FiltersByNotebookAndPersistsEditsAndDeletion()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.MigrateAsync();

        var firstNotebook = new Notebook("Programming");
        var secondNotebook = new Notebook("Architecture");
        context.Notebooks.AddRange(firstNotebook, secondNotebook);
        await context.SaveChangesAsync();

        var repository = new ConceptRepository(context);
        var concept = new Concept(firstNotebook.Id, "Dependency Injection", "Old definition");
        await repository.AddAsync(concept);
        await repository.AddAsync(new Concept(secondNotebook.Id, "Dependency Injection", "Other definition"));
        context.ChangeTracker.Clear();

        var concepts = await repository.GetByNotebookIdAsync(firstNotebook.Id);
        Assert.AreEqual(concept.Id, concepts.Single().Id);
        var loaded = await repository.GetByNormalizedNameAsync(
            firstNotebook.Id,
            concept.NormalizedName);
        Assert.IsNotNull(loaded);
        Assert.AreEqual(concept.Id, loaded.Id);

        loaded.Rename("Inversion of Control");
        loaded.UpdateDefinition("New definition");
        await repository.UpdateAsync(loaded);
        context.ChangeTracker.Clear();

        Assert.IsNull(await repository.GetByNormalizedNameAsync(
            firstNotebook.Id,
            concept.NormalizedName));
        var renamed = await repository.GetByNormalizedNameAsync(
            firstNotebook.Id,
            "INVERSION OF CONTROL");
        Assert.IsNotNull(renamed);
        Assert.AreEqual("New definition", renamed.Definition);

        await repository.DeleteAsync(renamed);
        context.ChangeTracker.Clear();

        Assert.AreEqual(0, (await repository.GetByNotebookIdAsync(firstNotebook.Id)).Count);
        Assert.AreEqual(1, (await repository.GetByNotebookIdAsync(secondNotebook.Id)).Count);
    }

    [TestMethod]
    public async Task SameNameWithDifferentCaseAndUnicodeForm_IsRejectedWithinNotebook()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.MigrateAsync();

        var notebook = new Notebook("Software Design");
        context.Notebooks.Add(notebook);
        context.Concepts.Add(new Concept(notebook.Id, "Årsag", "First definition"));
        await context.SaveChangesAsync();

        context.Concepts.Add(new Concept(notebook.Id, "a\u030arsag", "Second definition"));

        await Assert.ThrowsExceptionAsync<DbUpdateException>(
            async () => await context.SaveChangesAsync());
    }

    [TestMethod]
    public async Task SameName_IsAllowedInDifferentNotebooks()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.MigrateAsync();

        var firstNotebook = new Notebook("Programming");
        var secondNotebook = new Notebook("Architecture");
        context.Notebooks.AddRange(firstNotebook, secondNotebook);
        context.Concepts.AddRange(
            new Concept(firstNotebook.Id, "Singleton", "First definition"),
            new Concept(secondNotebook.Id, "singleton", "Second definition"));

        await context.SaveChangesAsync();

        Assert.AreEqual(2, await context.Concepts.CountAsync());
    }

    [TestMethod]
    public async Task DeletingNotebook_CascadesToItsConceptsOnly()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.MigrateAsync();

        var deletedNotebook = new Notebook("Old course");
        var retainedNotebook = new Notebook("Current course");
        context.Notebooks.AddRange(deletedNotebook, retainedNotebook);
        context.Concepts.AddRange(
            new Concept(deletedNotebook.Id, "Polymorphism", "Old definition"),
            new Concept(retainedNotebook.Id, "Polymorphism", "Current definition"));
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var notebookToDelete = await context.Notebooks
            .SingleAsync(notebook => notebook.Id == deletedNotebook.Id);
        context.Notebooks.Remove(notebookToDelete);
        await context.SaveChangesAsync();

        Assert.AreEqual(0, await context.Concepts
            .CountAsync(concept => concept.NotebookId == deletedNotebook.Id));
        Assert.AreEqual(1, await context.Concepts
            .CountAsync(concept => concept.NotebookId == retainedNotebook.Id));
    }

    private static SyllanoteDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<SyllanoteDbContext>()
            .UseSqlite(connection)
            .Options;

        return new SyllanoteDbContext(options);
    }
}
