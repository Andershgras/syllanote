using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Syllanote.Infrastructure.Repositories;

public class NotebookRepository : INotebookRepository
{
    private readonly SyllanoteDbContext _dbContext;

    public NotebookRepository(SyllanoteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Notebook notebook)
    {
        _dbContext.Notebooks.Add(notebook);

        await _dbContext.SaveChangesAsync();
    }
    public async Task<IReadOnlyList<Notebook>> GetAllAsync()
    {
        return await _dbContext.Notebooks
            .OrderBy(notebook => notebook.SortOrder)
            .ThenBy(notebook => notebook.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateAsync(Notebook notebook)
    {
        _dbContext.Notebooks.Update(notebook);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Notebook notebook)
    {
        _dbContext.Notebooks.Remove(notebook);
        await _dbContext.SaveChangesAsync();
    }
}
