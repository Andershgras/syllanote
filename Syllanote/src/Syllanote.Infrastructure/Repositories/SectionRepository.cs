using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Syllanote.Infrastructure.Repositories;

public class SectionRepository : ISectionRepository
{
    private readonly SyllanoteDbContext _dbContext;

    public SectionRepository(SyllanoteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Section section)
    {
        _dbContext.Sections.Add(section);

        await _dbContext.SaveChangesAsync();
    }
    public async Task<IReadOnlyList<Section>> GetByNotebookIdAsync(
    Guid notebookId)
    {
        return await _dbContext.Sections
            .Where(section => section.NotebookId == notebookId)
            .OrderBy(section => section.CreatedAt)
            .ToListAsync();
    }

    public async Task UpdateAsync(Section section)
    {
        _dbContext.Sections.Update(section);
        await _dbContext.SaveChangesAsync();
    }
}
