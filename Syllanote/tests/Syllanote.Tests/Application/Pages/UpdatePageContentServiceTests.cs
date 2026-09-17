using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Sections.Pages.UpdatePageContent;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Pages;

[TestClass]
public class UpdatePageContentServiceTests
{
    [TestMethod]
    public async Task UpdateAsync_ShouldUpdateContentAndRepository()
    {
        // Arrange
        var repository = new FakePageRepository();
        var service = new UpdatePageContentService(repository);

        var page = new Page(
            Guid.NewGuid(),
            "Confusion Matrix");

        // Act
        await service.UpdateAsync(
            page,
            "A confusion matrix contains TP, TN, FP and FN.");

        // Assert
        Assert.AreEqual(
            "A confusion matrix contains TP, TN, FP and FN.",
            page.Content);

        Assert.AreEqual(1, repository.UpdateCallCount);
        Assert.AreSame(page, repository.UpdatedPage);
    }


    private class FakePageRepository : IPageRepository
    {
        public Page? UpdatedPage { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task AddAsync(Page page)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Page>> GetBySectionIdAsync(
            Guid sectionId)
        {
            IReadOnlyList<Page> pages = [];

            return Task.FromResult(pages);
        }

        public Task UpdateAsync(Page page)
        {
            UpdatedPage = page;
            UpdateCallCount++;

            return Task.CompletedTask;
        }
    }
}