using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.DeleteNotebook;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Notebooks;

[TestClass]
public class DeleteNotebookServiceTests
{
    [TestMethod]
    public async Task DeleteAsync_DeletesRequestedNotebook()
    {
        var repository = new FakeNotebookRepository();
        var service = new DeleteNotebookService(repository);
        var notebook = new Notebook("Old notebook");

        await service.DeleteAsync(notebook);

        Assert.AreSame(notebook, repository.DeletedNotebook);
        Assert.AreEqual(1, repository.DeleteCallCount);
    }

    private class FakeNotebookRepository : INotebookRepository
    {
        public Notebook? DeletedNotebook { get; private set; }
        public int DeleteCallCount { get; private set; }

        public Task AddAsync(Notebook notebook) => Task.CompletedTask;

        public Task<IReadOnlyList<Notebook>> GetAllAsync()
        {
            IReadOnlyList<Notebook> notebooks = [];
            return Task.FromResult(notebooks);
        }

        public Task UpdateAsync(Notebook notebook) => Task.CompletedTask;

        public Task DeleteAsync(Notebook notebook)
        {
            DeletedNotebook = notebook;
            DeleteCallCount++;
            return Task.CompletedTask;
        }
    }
}
