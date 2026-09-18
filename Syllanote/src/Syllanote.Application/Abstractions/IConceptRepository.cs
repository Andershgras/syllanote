using Syllanote.Domain.Entities;

namespace Syllanote.Application.Abstractions;

public interface IConceptRepository
{
    Task AddAsync(Concept concept);
    Task<IReadOnlyList<Concept>> GetByNotebookIdAsync(Guid notebookId);
    Task<Concept?> GetByNormalizedNameAsync(Guid notebookId, string normalizedName);
    Task UpdateAsync(Concept concept);
    Task DeleteAsync(Concept concept);
}
