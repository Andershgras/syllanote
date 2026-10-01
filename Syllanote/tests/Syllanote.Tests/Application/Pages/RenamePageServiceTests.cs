using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Sections.Pages.RenamePage;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Pages;

[TestClass]
public class RenamePageServiceTests
{
    [TestMethod]
    public async Task RenameAsync_WithValidTitle_UpdatesPageAndRepository()
    {
        var repository = new FakePageRepository();
        var service = new RenamePageService(repository);
        var page = new Page(Guid.NewGuid(), "Old title");

        await service.RenameAsync(page, "New title");

        Assert.AreEqual("New title", page.Title);
        Assert.AreSame(page, repository.UpdatedPage);
        Assert.AreEqual(1, repository.UpdateCallCount);
    }

    [TestMethod]
    public async Task RenameAsync_WithInvalidTitle_DoesNotUpdateRepository()
    {
        var repository = new FakePageRepository();
        var service = new RenamePageService(repository);
        var page = new Page(Guid.NewGuid(), "Original title");

        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => service.RenameAsync(page, " "));

        Assert.AreEqual("Original title", page.Title);
        Assert.AreEqual(0, repository.UpdateCallCount);
    }

    [TestMethod]
    public async Task RenameAsync_WhenRepositoryFails_RestoresOriginalTitle()
    {
        var repository = new FakePageRepository
        {
            UpdateException = new InvalidOperationException("Database unavailable")
        };
        var service = new RenamePageService(repository);
        var page = new Page(Guid.NewGuid(), "Original title");

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => service.RenameAsync(page, "Unsaved title"));

        Assert.AreEqual("Original title", page.Title);
        Assert.AreEqual(1, repository.UpdateCallCount);
    }

    private class FakePageRepository : IPageRepository
    {
        public Page? UpdatedPage { get; private set; }
        public int UpdateCallCount { get; private set; }
        public Exception? UpdateException { get; init; }

        public Task AddAsync(Page page) => Task.CompletedTask;

        public Task<IReadOnlyList<Page>> GetBySectionIdAsync(Guid sectionId)
        {
            IReadOnlyList<Page> pages = [];
            return Task.FromResult(pages);
        }

        public Task<IReadOnlyList<Syllanote.Application.Notebooks.Sections.Pages.SearchPages.SearchPageResult>> SearchAsync(string searchText)
            => Task.FromResult<IReadOnlyList<Syllanote.Application.Notebooks.Sections.Pages.SearchPages.SearchPageResult>>([]);

        public Task UpdateAsync(Page page)
        {
            UpdatedPage = page;
            UpdateCallCount++;

            if (UpdateException is not null)
            {
                return Task.FromException(UpdateException);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Page page) => Task.CompletedTask;
    }
}
