using Syllanote.Domain.Entities;

namespace Syllanote.Application.Abstractions;

public interface INotebookRepository
{
    Task AddAsync(Notebook notebook);
    
    Task<IReadOnlyList<Notebook>> GetAllAsync();
    Task UpdateAsync(Notebook notebook);
    Task DeleteAsync(Notebook notebook);
}
