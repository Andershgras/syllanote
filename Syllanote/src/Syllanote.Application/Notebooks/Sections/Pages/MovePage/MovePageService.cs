using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Sections.Pages.MovePage;

public class MovePageService
{
    private readonly IPageRepository _pageRepository;

    public MovePageService(IPageRepository pageRepository)
    {
        _pageRepository = pageRepository;
    }

    public Task<bool> MoveUpAsync(Page page)
    {
        return MoveAsync(page, -1);
    }

    public Task<bool> MoveDownAsync(Page page)
    {
        return MoveAsync(page, 1);
    }

    private async Task<bool> MoveAsync(Page page, int offset)
    {
        var pages = (await _pageRepository
            .GetBySectionIdAsync(page.SectionId))
            .ToList();
        var currentIndex = pages.FindIndex(item => item.Id == page.Id);
        var targetIndex = currentIndex + offset;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= pages.Count)
        {
            return false;
        }

        (pages[currentIndex], pages[targetIndex]) =
            (pages[targetIndex], pages[currentIndex]);

        for (var index = 0; index < pages.Count; index++)
        {
            var item = pages[index];
            if (item.SortOrder == index)
            {
                continue;
            }

            item.ChangeSortOrder(index);
            await _pageRepository.UpdateAsync(item);
        }

        return true;
    }
}
