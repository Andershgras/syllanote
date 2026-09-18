using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Concepts.CreateConcept;

public class CreateConceptService
{
    private readonly IConceptRepository _conceptRepository;

    public CreateConceptService(IConceptRepository conceptRepository)
    {
        _conceptRepository = conceptRepository;
    }

    public async Task<Concept> CreateAsync(Guid notebookId, string name, string definition)
    {
        var concept = new Concept(notebookId, name, definition);
        var existing = await _conceptRepository.GetByNormalizedNameAsync(
            notebookId, concept.NormalizedName);
        if (existing is not null)
        {
            throw new DuplicateConceptNameException();
        }

        await _conceptRepository.AddAsync(concept);
        return concept;
    }
}
