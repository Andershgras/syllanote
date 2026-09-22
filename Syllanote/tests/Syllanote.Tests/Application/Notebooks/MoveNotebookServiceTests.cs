using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.MoveNotebook;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Notebooks;

[TestClass]
public class MoveNotebookServiceTests
{
    [TestMethod]
    public async Task MoveUpAsync_WithPreviousNotebook_SwapsTheirOrder()
    {
        var first = new Notebook("First", 0);
        var second = new Notebook("Second", 1);
        var repository = new FakeNotebookRepository([first, second]);
        var service = new MoveNotebookService(repository);

        var moved = await service.MoveUpAsync(second);

        Assert.IsTrue(moved);
        Assert.AreEqual(1, first.SortOrder);
        Assert.AreEqual(0, second.SortOrder);
        CollectionAssert.AreEquivalent(
            new[] { first, second },
            repository.UpdatedNotebooks.ToArray());
    }

    [TestMethod]
    public async Task MoveDownAsync_WithNextNotebook_SwapsTheirOrder()
    {
        var first = new Notebook("First", 0);
        var second = new Notebook("Second", 1);
        var repository = new FakeNotebookRepository([first, second]);
        var service = new MoveNotebookService(repository);

        var moved = await service.MoveDownAsync(first);

        Assert.IsTrue(moved);
        Assert.AreEqual(1, first.SortOrder);
        Assert.AreEqual(0, second.SortOrder);
    }

    [TestMethod]
    public async Task MoveUpAsync_WithFirstNotebook_DoesNotUpdateRepository()
    {
        var first = new Notebook("First", 0);
        var second = new Notebook("Second", 1);
        var repository = new FakeNotebookRepository([first, second]);
        var service = new MoveNotebookService(repository);

        var moved = await service.MoveUpAsync(first);

        Assert.IsFalse(moved);
        Assert.AreEqual(0, repository.UpdatedNotebooks.Count);
    }

    [TestMethod]
    public async Task MoveUpAsync_WithLegacySortOrders_NormalizesAllNotebooks()
    {
        var first = new Notebook("First");
        var second = new Notebook("Second");
        var third = new Notebook("Third");
        var repository = new FakeNotebookRepository([first, second, third]);
        var service = new MoveNotebookService(repository);

        var moved = await service.MoveUpAsync(second);

        Assert.IsTrue(moved);
        Assert.AreEqual(1, first.SortOrder);
        Assert.AreEqual(0, second.SortOrder);
        Assert.AreEqual(2, third.SortOrder);
    }

    private sealed class FakeNotebookRepository : INotebookRepository
    {
        private readonly IReadOnlyList<Notebook> _notebooks;

        public FakeNotebookRepository(IReadOnlyList<Notebook> notebooks)
        {
            _notebooks = notebooks;
        }

        public List<Notebook> UpdatedNotebooks { get; } = [];

        public Task AddAsync(Notebook notebook) => Task.CompletedTask;

        public Task<IReadOnlyList<Notebook>> GetAllAsync()
        {
            return Task.FromResult(_notebooks);
        }

        public Task UpdateAsync(Notebook notebook)
        {
            UpdatedNotebooks.Add(notebook);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Notebook notebook) => Task.CompletedTask;
    }
}
