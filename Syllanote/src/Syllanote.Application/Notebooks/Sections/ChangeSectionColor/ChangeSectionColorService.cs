using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Sections.ChangeSectionColor;

public class ChangeSectionColorService
{
    private readonly ISectionRepository _sectionRepository;

    public ChangeSectionColorService(ISectionRepository sectionRepository)
    {
        _sectionRepository = sectionRepository;
    }

    public async Task ChangeAsync(Section section, string? colorKey)
    {
        if (section.ColorKey == colorKey)
        {
            return;
        }

        var originalColorKey = section.ColorKey;
        section.ChangeColor(colorKey);
        try
        {
            await _sectionRepository.UpdateAsync(section);
        }
        catch
        {
            section.ChangeColor(originalColorKey);
            throw;
        }
    }
}
