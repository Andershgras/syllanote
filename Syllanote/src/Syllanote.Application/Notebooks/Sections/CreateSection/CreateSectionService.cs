using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Sections.CreateSection;

public class CreateSectionService
{
    private readonly ISectionRepository _sectionRepository;

    public CreateSectionService(ISectionRepository sectionRepository)
    {
        _sectionRepository = sectionRepository;
    }

    public async Task<Section> CreateAsync(
        Guid notebookId,
        string name)
    {
        var section = new Section(notebookId, name);
        var sections =
            await _sectionRepository.GetByNotebookIdAsync(notebookId);
        var sortOrder = sections.Count == 0
            ? 0
            : sections.Max(item => item.SortOrder) + 1;
        section.ChangeSortOrder(sortOrder);

        await _sectionRepository.AddAsync(section);

        return section;
    }
}
