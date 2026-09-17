using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Sections.DeleteSection;

public class DeleteSectionService
{
    private readonly ISectionRepository _sectionRepository;

    public DeleteSectionService(ISectionRepository sectionRepository)
    {
        _sectionRepository = sectionRepository;
    }

    public Task DeleteAsync(Section section)
    {
        return _sectionRepository.DeleteAsync(section);
    }
}
