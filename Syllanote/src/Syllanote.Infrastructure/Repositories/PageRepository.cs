using Microsoft.EntityFrameworkCore;
using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Persistence;

namespace Syllanote.Infrastructure.Repositories;

public class PageRepository : IPageRepository
{
    private readonly SyllanoteDbContext _dbContext;

    public PageRepository(SyllanoteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Page page)
    {
        _dbContext.Pages.Add(page);

        await _dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Page>> GetBySectionIdAsync(
        Guid sectionId)
    {
        return await _dbContext.Pages
            .Where(page => page.SectionId == sectionId)
            .OrderBy(page => page.CreatedAt)
            .ToListAsync();
    }
}