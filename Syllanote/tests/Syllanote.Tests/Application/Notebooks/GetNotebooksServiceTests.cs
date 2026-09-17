using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.GetNotebooks;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Notebooks;

[TestClass]
public class GetNotebooksServiceTests
{
    private class FakeNotebookRepository : INotebookRepository
    {
        public IReadOnlyList<Notebook> NotebooksToReturn { get; set; }
            = [];

        public Task AddAsync(Notebook notebook)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Notebook>> GetAllAsync()
        {
            return Task.FromResult(NotebooksToReturn);
        }

        public Task UpdateAsync(Notebook notebook)
        {
            return Task.CompletedTask;
        }
    }

    [TestMethod]
    public async Task GetAllAsync_ReturnsNotebooksFromRepository()
    {
        // Arrange
        var repository = new FakeNotebookRepository();

        repository.NotebooksToReturn =
        [
            new Notebook("Machine Learning"),
            new Notebook("Software Architecture")
        ];

        var service = new GetNotebooksService(repository);

        // Act
        var notebooks = await service.GetAllAsync();

        // Assert
        Assert.AreEqual(2, notebooks.Count);
        Assert.AreEqual("Machine Learning", notebooks[0].Name);
        Assert.AreEqual("Software Architecture", notebooks[1].Name);
    }
}
