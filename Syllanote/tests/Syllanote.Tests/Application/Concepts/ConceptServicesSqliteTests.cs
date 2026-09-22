using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Syllanote.Application.Notebooks.Concepts;
using Syllanote.Application.Notebooks.Concepts.CreateConcept;
using Syllanote.Application.Notebooks.Concepts.DeleteConcept;
using Syllanote.Application.Notebooks.Concepts.GetConcepts;
using Syllanote.Application.Notebooks.Concepts.UpdateConcept;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Persistence;
using Syllanote.Infrastructure.Repositories;

namespace Syllanote.Tests.Application.Concepts;

[TestClass]
public class ConceptServicesSqliteTests
{
    [TestMethod]
    public async Task Create_RejectsDuplicateNameWithinNotebookButAllowsAnotherNotebook()
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
        var create = new CreateConceptService(repository);
        await create.CreateAsync(firstNotebook.Id, "Årsag", "First definition");

        var error = await Assert.ThrowsExceptionAsync<DuplicateConceptNameException>(
            () => create.CreateAsync(firstNotebook.Id, "a\u030arsag", "Duplicate"));
        Assert.AreEqual(
            "A concept with this name already exists in this notebook.",
            error.Message);

        await create.CreateAsync(secondNotebook.Id, "årsag", "Other definition");
        var get = new GetConceptsService(repository);
        Assert.AreEqual(1, (await get.GetByNotebookIdAsync(firstNotebook.Id)).Count);
        Assert.AreEqual(1, (await get.GetByNotebookIdAsync(secondNotebook.Id)).Count);
    }

    [TestMethod]
    public async Task Update_RejectsDuplicateAndInvalidDefinitionWithoutChangingConcept()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.MigrateAsync();

        var notebook = new Notebook("Programming");
        context.Notebooks.Add(notebook);
        await context.SaveChangesAsync();

        var repository = new ConceptRepository(context);
        var create = new CreateConceptService(repository);
        var first = await create.CreateAsync(notebook.Id, "Singleton", "Original definition");
        await create.CreateAsync(notebook.Id, "Polymorphism", "Other definition");
        var update = new UpdateConceptService(repository);

        await Assert.ThrowsExceptionAsync<DuplicateConceptNameException>(
            () => update.UpdateAsync(first, "POLYMORPHISM", "Changed definition"));
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => update.UpdateAsync(first, "New name", " "));
        Assert.AreEqual("Singleton", first.Name);
        Assert.AreEqual("Original definition", first.Definition);

        await update.UpdateAsync(first, "  Design Pattern  ", "Updated definition");
        context.ChangeTracker.Clear();
        var saved = await repository.GetByNormalizedNameAsync(
            notebook.Id, "DESIGN PATTERN");
        Assert.IsNotNull(saved);
        Assert.AreEqual("Design Pattern", saved.Name);
        Assert.AreEqual("Updated definition", saved.Definition);

        await new DeleteConceptService(repository).DeleteAsync(saved);
        Assert.AreEqual(1, (await new GetConceptsService(repository)
            .GetByNotebookIdAsync(notebook.Id)).Count);
    }

    private static SyllanoteDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<SyllanoteDbContext>()
            .UseSqlite(connection)
            .Options;

        return new SyllanoteDbContext(options);
    }
}
