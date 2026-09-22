using Microsoft.EntityFrameworkCore;
using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;
using Syllanote.Infrastructure.Persistence;
using Syllanote.Application.Notebooks.Sections.Pages.SearchPages;

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
            .OrderBy(page => page.SortOrder)
            .ThenBy(page => page.CreatedAt)
            .ToListAsync();
    }
    public async Task<IReadOnlyList<SearchPageResult>> SearchAsync(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return [];
        }

        const int maxResults = 100;
        var escapedText = searchText.Trim()
            .Replace("\\", "\\\\")
            .Replace("%", "\\%")
            .Replace("_", "\\_");
        var pattern = $"%{escapedText}%";

        return await (
            from page in _dbContext.Pages.AsNoTracking()
            join section in _dbContext.Sections.AsNoTracking()
                on page.SectionId equals section.Id
            join notebook in _dbContext.Notebooks.AsNoTracking()
                on section.NotebookId equals notebook.Id
            where EF.Functions.Like(page.Title, pattern, "\\") ||
                  EF.Functions.Like(page.Content, pattern, "\\")
            orderby notebook.Name, section.Name, page.Title, page.Id
            select new SearchPageResult(
                page.Id,
                page.Title,
                section.Id,
                section.Name,
                notebook.Id,
                notebook.Name))
            .Take(maxResults)
            .ToListAsync();
    }
    public async Task UpdateAsync(Page page)
    {
        _dbContext.Pages.Update(page);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Page page)
    {
        _dbContext.Pages.Remove(page);
        await _dbContext.SaveChangesAsync();
    }
}
