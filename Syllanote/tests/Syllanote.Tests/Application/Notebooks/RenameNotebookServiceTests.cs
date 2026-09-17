using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.RenameNotebook;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Notebooks;

[TestClass]
public class RenameNotebookServiceTests
{
    [TestMethod]
    public async Task RenameAsync_WithValidName_UpdatesNotebookAndRepository()
    {
        var repository = new FakeNotebookRepository();
        var service = new RenameNotebookService(repository);
        var notebook = new Notebook("Old name");

        await service.RenameAsync(notebook, "New name");

        Assert.AreEqual("New name", notebook.Name);
        Assert.AreSame(notebook, repository.UpdatedNotebook);
        Assert.AreEqual(1, repository.UpdateCallCount);
    }

    [TestMethod]
    public async Task RenameAsync_WithInvalidName_DoesNotUpdateRepository()
    {
        var repository = new FakeNotebookRepository();
        var service = new RenameNotebookService(repository);
        var notebook = new Notebook("Original name");

        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => service.RenameAsync(notebook, " "));

        Assert.AreEqual("Original name", notebook.Name);
        Assert.AreEqual(0, repository.UpdateCallCount);
    }

    private class FakeNotebookRepository : INotebookRepository
    {
        public Notebook? UpdatedNotebook { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task AddAsync(Notebook notebook) => Task.CompletedTask;

        public Task<IReadOnlyList<Notebook>> GetAllAsync()
        {
            IReadOnlyList<Notebook> notebooks = [];
            return Task.FromResult(notebooks);
        }

        public Task UpdateAsync(Notebook notebook)
        {
            UpdatedNotebook = notebook;
            UpdateCallCount++;
            return Task.CompletedTask;
        }
    }
}
