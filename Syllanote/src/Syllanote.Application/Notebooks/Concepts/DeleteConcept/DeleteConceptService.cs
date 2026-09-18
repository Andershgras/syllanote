using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Concepts.DeleteConcept;

public class DeleteConceptService
{
    private readonly IConceptRepository _conceptRepository;

    public DeleteConceptService(IConceptRepository conceptRepository)
    {
        _conceptRepository = conceptRepository;
    }

    public Task DeleteAsync(Concept concept)
    {
        return _conceptRepository.DeleteAsync(concept);
    }
}
