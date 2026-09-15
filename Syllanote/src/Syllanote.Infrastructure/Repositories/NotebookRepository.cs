using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Persistence;

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
}