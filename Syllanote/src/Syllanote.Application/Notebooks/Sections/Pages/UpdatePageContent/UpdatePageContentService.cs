using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Sections.Pages.UpdatePageContent;

public class UpdatePageContentService
{
    private readonly IPageRepository _pageRepository;

    public UpdatePageContentService(IPageRepository pageRepository)
    {
        _pageRepository = pageRepository;
    }

    public async Task UpdateAsync(Page page, string content)
    {
        page.UpdateContent(content);

        await _pageRepository.UpdateAsync(page);
    }
}