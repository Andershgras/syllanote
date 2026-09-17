using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Sections.Pages.DeletePage;

public class DeletePageService
{
    private readonly IPageRepository _pageRepository;

    public DeletePageService(IPageRepository pageRepository)
    {
        _pageRepository = pageRepository;
    }

    public Task DeleteAsync(Page page)
    {
        return _pageRepository.DeleteAsync(page);
    }
}
