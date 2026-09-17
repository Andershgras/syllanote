using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Sections.Pages.SearchPages;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Pages;

[TestClass]
public class SearchPagesServiceTests
{
    private class FakePageRepository : IPageRepository
    {
        public string? SearchText { get; private set; }
        public IReadOnlyList<SearchPageResult> Results { get; set; } = [];

        public Task<IReadOnlyList<SearchPageResult>> SearchAsync(string searchText)
        {
            SearchText = searchText;
            return Task.FromResult(Results);
        }

        public Task AddAsync(Page page) => Task.CompletedTask;
        public Task<IReadOnlyList<Page>> GetBySectionIdAsync(Guid sectionId)
            => Task.FromResult<IReadOnlyList<Page>>([]);
        public Task UpdateAsync(Page page) => Task.CompletedTask;
        public Task DeleteAsync(Page page) => Task.CompletedTask;
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow("  ")]
    public async Task EmptySearch_ReturnsNoResultsWithoutCallingRepository(string searchText)
    {
        var repository = new FakePageRepository();
        var results = await new SearchPagesService(repository).SearchAsync(searchText);

        Assert.AreEqual(0, results.Count);
        Assert.IsNull(repository.SearchText);
    }

    [TestMethod]
    public async Task Search_TrimsTextAndReturnsRepositoryResults()
    {
        var expected = new SearchPageResult(
            Guid.NewGuid(), "Page", Guid.NewGuid(), "Section", Guid.NewGuid(), "Notebook");
        var repository = new FakePageRepository { Results = [expected] };

        var results = await new SearchPagesService(repository).SearchAsync("  page  ");

        Assert.AreEqual("page", repository.SearchText);
        Assert.AreSame(expected, results[0]);
    }
}
