using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Sections.MoveSection;

public class MoveSectionService
{
    private readonly ISectionRepository _sectionRepository;

    public MoveSectionService(ISectionRepository sectionRepository)
    {
        _sectionRepository = sectionRepository;
    }

    public Task<bool> MoveUpAsync(Section section)
    {
        return MoveAsync(section, -1);
    }

    public Task<bool> MoveDownAsync(Section section)
    {
        return MoveAsync(section, 1);
    }

    private async Task<bool> MoveAsync(Section section, int offset)
    {
        var sections = (await _sectionRepository
            .GetByNotebookIdAsync(section.NotebookId))
            .ToList();
        var currentIndex = sections.FindIndex(item => item.Id == section.Id);
        var targetIndex = currentIndex + offset;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= sections.Count)
        {
            return false;
        }

        (sections[currentIndex], sections[targetIndex]) =
            (sections[targetIndex], sections[currentIndex]);

        for (var index = 0; index < sections.Count; index++)
        {
            var item = sections[index];
            if (item.SortOrder == index)
            {
                continue;
            }

            item.ChangeSortOrder(index);
            await _sectionRepository.UpdateAsync(item);
        }

        return true;
    }
}
