using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Concepts.UpdateConcept;

public class UpdateConceptService
{
    private readonly IConceptRepository _conceptRepository;

    public UpdateConceptService(IConceptRepository conceptRepository)
    {
        _conceptRepository = conceptRepository;
    }

    public async Task UpdateAsync(Concept concept, string name, string definition)
    {
        var normalizedName = Concept.NormalizeName(name);
        var existing = await _conceptRepository.GetByNormalizedNameAsync(
            concept.NotebookId, normalizedName);
        if (existing is not null && existing.Id != concept.Id)
        {
            throw new DuplicateConceptNameException();
        }

        concept.Update(name, definition);
        await _conceptRepository.UpdateAsync(concept);
    }
}
