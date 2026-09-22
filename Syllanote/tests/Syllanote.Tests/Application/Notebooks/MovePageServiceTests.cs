using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Sections.Pages.MovePage;
using Syllanote.Application.Notebooks.Sections.Pages.SearchPages;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Notebooks;

[TestClass]
public class MovePageServiceTests
{
    [TestMethod]
    public async Task MoveUpAsync_WithPreviousPage_SwapsTheirOrder()
    {
        var sectionId = Guid.NewGuid();
        var first = new Page(sectionId, "First", 0);
        var second = new Page(sectionId, "Second", 1);
        var repository = new FakePageRepository([first, second]);
        var service = new MovePageService(repository);

        var moved = await service.MoveUpAsync(second);

        Assert.IsTrue(moved);
        Assert.AreEqual(1, first.SortOrder);
        Assert.AreEqual(0, second.SortOrder);
        CollectionAssert.AreEquivalent(
            new[] { first, second },
            repository.UpdatedPages.ToArray());
    }

    [TestMethod]
    public async Task MoveDownAsync_WithNextPage_SwapsTheirOrder()
    {
        var sectionId = Guid.NewGuid();
        var first = new Page(sectionId, "First", 0);
        var second = new Page(sectionId, "Second", 1);
        var repository = new FakePageRepository([first, second]);
        var service = new MovePageService(repository);

        var moved = await service.MoveDownAsync(first);

        Assert.IsTrue(moved);
        Assert.AreEqual(1, first.SortOrder);
        Assert.AreEqual(0, second.SortOrder);
    }

    [TestMethod]
    public async Task MoveUpAsync_WithFirstPage_DoesNotUpdateRepository()
    {
        var sectionId = Guid.NewGuid();
        var first = new Page(sectionId, "First", 0);
        var second = new Page(sectionId, "Second", 1);
        var repository = new FakePageRepository([first, second]);
        var service = new MovePageService(repository);

        var moved = await service.MoveUpAsync(first);

        Assert.IsFalse(moved);
        Assert.AreEqual(0, repository.UpdatedPages.Count);
    }

    [TestMethod]
    public async Task MoveUpAsync_OnlyLoadsPagesFromSameSection()
    {
        var sectionId = Guid.NewGuid();
        var first = new Page(sectionId, "First", 0);
        var second = new Page(sectionId, "Second", 1);
        var repository = new FakePageRepository([first, second]);
        var service = new MovePageService(repository);

        await service.MoveUpAsync(second);

        Assert.AreEqual(sectionId, repository.RequestedSectionId);
    }

    [TestMethod]
    public async Task MoveUpAsync_WithLegacySortOrders_NormalizesAllPages()
    {
        var sectionId = Guid.NewGuid();
        var first = new Page(sectionId, "First");
        var second = new Page(sectionId, "Second");
        var third = new Page(sectionId, "Third");
        var repository = new FakePageRepository([first, second, third]);
        var service = new MovePageService(repository);

        var moved = await service.MoveUpAsync(second);

        Assert.IsTrue(moved);
        Assert.AreEqual(1, first.SortOrder);
        Assert.AreEqual(0, second.SortOrder);
        Assert.AreEqual(2, third.SortOrder);
    }

    private sealed class FakePageRepository : IPageRepository
    {
        private readonly IReadOnlyList<Page> _pages;

        public FakePageRepository(IReadOnlyList<Page> pages)
        {
            _pages = pages;
        }

        public Guid RequestedSectionId { get; private set; }

        public List<Page> UpdatedPages { get; } = [];

        public Task AddAsync(Page page) => Task.CompletedTask;

        public Task<IReadOnlyList<Page>> GetBySectionIdAsync(Guid sectionId)
        {
            RequestedSectionId = sectionId;
            return Task.FromResult(_pages);
        }

        public Task<IReadOnlyList<SearchPageResult>> SearchAsync(string searchText)
        {
            return Task.FromResult<IReadOnlyList<SearchPageResult>>([]);
        }

        public Task UpdateAsync(Page page)
        {
            UpdatedPages.Add(page);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Page page) => Task.CompletedTask;
    }
}
