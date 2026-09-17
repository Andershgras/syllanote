using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Sections.Pages.RenamePage;

public class RenamePageService
{
    private readonly IPageRepository _pageRepository;

    public RenamePageService(IPageRepository pageRepository)
    {
        _pageRepository = pageRepository;
    }

    public async Task RenameAsync(Page page, string title)
    {
        page.Rename(title);
        await _pageRepository.UpdateAsync(page);
    }
}
