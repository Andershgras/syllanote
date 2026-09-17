using Syllanote.Domain.Entities;

namespace Syllanote.Application.Abstractions;

public interface ISectionRepository
{
    Task AddAsync(Section section);
    Task<IReadOnlyList<Section>> GetByNotebookIdAsync(Guid notebookId);
    Task UpdateAsync(Section section);
    Task DeleteAsync(Section section);
}
