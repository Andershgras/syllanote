using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Sections.Pages.GetPages;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Notebooks;

[TestClass]
public class GetPagesServiceTests
{
    private class FakePageRepository : IPageRepository
    {
        public Guid? RequestedSectionId { get; private set; }

        public IReadOnlyList<Page> PagesToReturn { get; set; } = [];

        public Task AddAsync(Page page)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Page>> GetBySectionIdAsync(
            Guid sectionId)
        {
            RequestedSectionId = sectionId;

            return Task.FromResult(PagesToReturn);
        }
        public Task<IReadOnlyList<Syllanote.Application.Notebooks.Sections.Pages.SearchPages.SearchPageResult>> SearchAsync(string searchText)
            => Task.FromResult<IReadOnlyList<Syllanote.Application.Notebooks.Sections.Pages.SearchPages.SearchPageResult>>([]);

        public Task UpdateAsync(Page page)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Page page) => Task.CompletedTask;
    }

    [TestMethod]
    public async Task GetBySectionIdAsync_ReturnsPagesForRequestedSection()
    {
        // Arrange
        var sectionId = Guid.NewGuid();

        var repository = new FakePageRepository
        {
            PagesToReturn =
            [
                new Page(sectionId, "Introduction"),
                new Page(sectionId, "Confusion Matrix")
            ]
        };

        var service = new GetPagesService(repository);

        // Act
        var pages =
            await service.GetBySectionIdAsync(sectionId);

        // Assert
        Assert.AreEqual(
            sectionId,
            repository.RequestedSectionId);

        Assert.AreEqual(2, pages.Count);
        Assert.AreEqual("Introduction", pages[0].Title);
        Assert.AreEqual("Confusion Matrix", pages[1].Title);
    }
}
