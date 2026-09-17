using Syllanote.Application.Abstractions;

namespace Syllanote.Application.Notebooks.Sections.Pages.SearchPages;

public class SearchPagesService
{
    private readonly IPageRepository _pageRepository;

    public SearchPagesService(IPageRepository pageRepository)
    {
        _pageRepository = pageRepository;
    }

    public Task<IReadOnlyList<SearchPageResult>> SearchAsync(string searchText)
    {
        return string.IsNullOrWhiteSpace(searchText)
            ? Task.FromResult<IReadOnlyList<SearchPageResult>>([])
            : _pageRepository.SearchAsync(searchText.Trim());
    }
}
