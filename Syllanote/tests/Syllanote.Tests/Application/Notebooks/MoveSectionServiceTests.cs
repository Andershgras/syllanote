using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Sections.MoveSection;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Notebooks;

[TestClass]
public class MoveSectionServiceTests
{
    [TestMethod]
    public async Task MoveUpAsync_WithPreviousSection_SwapsTheirOrder()
    {
        var notebookId = Guid.NewGuid();
        var first = new Section(notebookId, "First", 0);
        var second = new Section(notebookId, "Second", 1);
        var repository = new FakeSectionRepository([first, second]);
        var service = new MoveSectionService(repository);

        var moved = await service.MoveUpAsync(second);

        Assert.IsTrue(moved);
        Assert.AreEqual(1, first.SortOrder);
        Assert.AreEqual(0, second.SortOrder);
        CollectionAssert.AreEquivalent(
            new[] { first, second },
            repository.UpdatedSections.ToArray());
    }

    [TestMethod]
    public async Task MoveDownAsync_WithNextSection_SwapsTheirOrder()
    {
        var notebookId = Guid.NewGuid();
        var first = new Section(notebookId, "First", 0);
        var second = new Section(notebookId, "Second", 1);
        var repository = new FakeSectionRepository([first, second]);
        var service = new MoveSectionService(repository);

        var moved = await service.MoveDownAsync(first);

        Assert.IsTrue(moved);
        Assert.AreEqual(1, first.SortOrder);
        Assert.AreEqual(0, second.SortOrder);
    }

    [TestMethod]
    public async Task MoveUpAsync_WithFirstSection_DoesNotUpdateRepository()
    {
        var notebookId = Guid.NewGuid();
        var first = new Section(notebookId, "First", 0);
        var second = new Section(notebookId, "Second", 1);
        var repository = new FakeSectionRepository([first, second]);
        var service = new MoveSectionService(repository);

        var moved = await service.MoveUpAsync(first);

        Assert.IsFalse(moved);
        Assert.AreEqual(0, repository.UpdatedSections.Count);
    }

    [TestMethod]
    public async Task MoveUpAsync_OnlyLoadsSectionsFromSameNotebook()
    {
        var notebookId = Guid.NewGuid();
        var first = new Section(notebookId, "First", 0);
        var second = new Section(notebookId, "Second", 1);
        var repository = new FakeSectionRepository([first, second]);
        var service = new MoveSectionService(repository);

        await service.MoveUpAsync(second);

        Assert.AreEqual(notebookId, repository.RequestedNotebookId);
    }

    [TestMethod]
    public async Task MoveUpAsync_WithLegacySortOrders_NormalizesAllSections()
    {
        var notebookId = Guid.NewGuid();
        var first = new Section(notebookId, "First");
        var second = new Section(notebookId, "Second");
        var third = new Section(notebookId, "Third");
        var repository = new FakeSectionRepository([first, second, third]);
        var service = new MoveSectionService(repository);

        var moved = await service.MoveUpAsync(second);

        Assert.IsTrue(moved);
        Assert.AreEqual(1, first.SortOrder);
        Assert.AreEqual(0, second.SortOrder);
        Assert.AreEqual(2, third.SortOrder);
    }

    private sealed class FakeSectionRepository : ISectionRepository
    {
        private readonly IReadOnlyList<Section> _sections;

        public FakeSectionRepository(IReadOnlyList<Section> sections)
        {
            _sections = sections;
        }

        public Guid RequestedNotebookId { get; private set; }

        public List<Section> UpdatedSections { get; } = [];

        public Task AddAsync(Section section) => Task.CompletedTask;

        public Task<IReadOnlyList<Section>> GetByNotebookIdAsync(Guid notebookId)
        {
            RequestedNotebookId = notebookId;
            return Task.FromResult(_sections);
        }

        public Task UpdateAsync(Section section)
        {
            UpdatedSections.Add(section);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Section section) => Task.CompletedTask;
    }
}
