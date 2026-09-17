using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Sections.Pages.CreatePage;

public class CreatePageService
{
    private readonly IPageRepository _pageRepository;

    public CreatePageService(IPageRepository pageRepository)
    {
        _pageRepository = pageRepository;
    }

    public async Task<Page> CreateAsync(
        Guid sectionId,
        string title)
    {
        var page = new Page(sectionId, title);

        await _pageRepository.AddAsync(page);

        return page;
    }
}