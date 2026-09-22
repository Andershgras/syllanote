using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Concepts.GetConcepts;

public class GetConceptsService
{
    private readonly IConceptRepository _conceptRepository;

    public GetConceptsService(IConceptRepository conceptRepository)
    {
        _conceptRepository = conceptRepository;
    }

    public Task<IReadOnlyList<Concept>> GetByNotebookIdAsync(Guid notebookId)
    {
        return _conceptRepository.GetByNotebookIdAsync(notebookId);
    }
}
