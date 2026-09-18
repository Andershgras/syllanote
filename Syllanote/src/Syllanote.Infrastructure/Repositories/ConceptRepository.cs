using Microsoft.EntityFrameworkCore;
using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Persistence;

namespace Syllanote.Infrastructure.Repositories;

public class ConceptRepository : IConceptRepository
{
    private readonly SyllanoteDbContext _dbContext;

    public ConceptRepository(SyllanoteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Concept concept)
    {
        _dbContext.Concepts.Add(concept);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Concept>> GetByNotebookIdAsync(Guid notebookId)
    {
        return await _dbContext.Concepts
            .Where(concept => concept.NotebookId == notebookId)
            .OrderBy(concept => concept.NormalizedName)
            .ThenBy(concept => concept.Id)
            .ToListAsync();
    }

    public Task<Concept?> GetByNormalizedNameAsync(
        Guid notebookId,
        string normalizedName)
    {
        return _dbContext.Concepts
            .FirstOrDefaultAsync(concept =>
                concept.NotebookId == notebookId &&
                concept.NormalizedName == normalizedName);
    }

    public async Task UpdateAsync(Concept concept)
    {
        _dbContext.Concepts.Update(concept);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Concept concept)
    {
        _dbContext.Concepts.Remove(concept);
        await _dbContext.SaveChangesAsync();
    }
}
