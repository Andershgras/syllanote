using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Sections.Pages.GetPages;

public class GetPagesService
{
    private readonly IPageRepository _pageRepository;

    public GetPagesService(IPageRepository pageRepository)
    {
        _pageRepository = pageRepository;
    }

    public async Task<IReadOnlyList<Page>> GetBySectionIdAsync(
        Guid sectionId)
    {
        return await _pageRepository.GetBySectionIdAsync(sectionId);
    }
}