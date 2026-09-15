using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.CreateNotebook;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Notebooks;

[TestClass]
public class CreateNotebookServiceTests
{
    private class FakeNotebookRepository : INotebookRepository
    {
        public Notebook? AddedNotebook { get; private set; }
        public int AddCallCount { get; private set; }
        public Task AddAsync(Notebook notebook)
        {
            AddCallCount++;
            AddedNotebook = notebook;

            return Task.CompletedTask;
        }
    }
    [TestMethod]
    public async Task CreateAsync_WithValidName_CreatesAndAddsNotebook()
    {
        // Arrange
        var repository = new FakeNotebookRepository();
        var service = new CreateNotebookService(repository);

        // Act
        var notebook = await service.CreateAsync("My notebook");

        // Assert
        Assert.AreEqual("My notebook", notebook.Name);
        Assert.AreSame(notebook, repository.AddedNotebook);
    }
    [TestMethod]
    public async Task CreateAsync_WithInvalidName_DoesNotAddNotebook()
    {
        // Arrange
        var repository = new FakeNotebookRepository();
        var service = new CreateNotebookService(repository);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => service.CreateAsync(""));

        Assert.AreEqual(0, repository.AddCallCount);
    }
}