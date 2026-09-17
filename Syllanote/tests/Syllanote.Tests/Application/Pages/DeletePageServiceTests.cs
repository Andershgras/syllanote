using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Sections.Pages.DeletePage;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Pages;

[TestClass]
public class DeletePageServiceTests
{
    [TestMethod]
    public async Task DeleteAsync_DeletesRequestedPage()
    {
        var repository = new FakePageRepository();
        var service = new DeletePageService(repository);
        var page = new Page(Guid.NewGuid(), "Old note");

        await service.DeleteAsync(page);

        Assert.AreSame(page, repository.DeletedPage);
        Assert.AreEqual(1, repository.DeleteCallCount);
    }

    private class FakePageRepository : IPageRepository
    {
        public Page? DeletedPage { get; private set; }
        public int DeleteCallCount { get; private set; }

        public Task AddAsync(Page page) => Task.CompletedTask;

        public Task<IReadOnlyList<Page>> GetBySectionIdAsync(Guid sectionId)
        {
            IReadOnlyList<Page> pages = [];
            return Task.FromResult(pages);
        }

        public Task<IReadOnlyList<Syllanote.Application.Notebooks.Sections.Pages.SearchPages.SearchPageResult>> SearchAsync(string searchText)
            => Task.FromResult<IReadOnlyList<Syllanote.Application.Notebooks.Sections.Pages.SearchPages.SearchPageResult>>([]);

        public Task UpdateAsync(Page page) => Task.CompletedTask;

        public Task DeleteAsync(Page page)
        {
            DeletedPage = page;
            DeleteCallCount++;
            return Task.CompletedTask;
        }
    }
}
