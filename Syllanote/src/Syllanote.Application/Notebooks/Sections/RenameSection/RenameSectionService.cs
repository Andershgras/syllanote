using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Sections.RenameSection;

public class RenameSectionService
{
    private readonly ISectionRepository _sectionRepository;

    public RenameSectionService(ISectionRepository sectionRepository)
    {
        _sectionRepository = sectionRepository;
    }

    public async Task RenameAsync(Section section, string name)
    {
        section.Rename(name);
        await _sectionRepository.UpdateAsync(section);
    }
}
