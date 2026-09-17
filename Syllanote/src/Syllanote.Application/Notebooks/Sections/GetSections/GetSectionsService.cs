using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Sections.GetSections;

public class GetSectionsService
{
    private readonly ISectionRepository _sectionRepository;

    public GetSectionsService(ISectionRepository sectionRepository)
    {
        _sectionRepository = sectionRepository;
    }

    public async Task<IReadOnlyList<Section>> GetByNotebookIdAsync(
        Guid notebookId)
    {
        return await _sectionRepository.GetByNotebookIdAsync(notebookId);
    }
}