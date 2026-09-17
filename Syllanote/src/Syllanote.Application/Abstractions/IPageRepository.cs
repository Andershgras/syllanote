using Syllanote.Domain.Entities;

using Syllanote.Application.Notebooks.Sections.Pages.SearchPages;

namespace Syllanote.Application.Abstractions;

public interface IPageRepository
{
    Task AddAsync(Page page);
    Task<IReadOnlyList<Page>> GetBySectionIdAsync(Guid sectionId);
    Task<IReadOnlyList<SearchPageResult>> SearchAsync(string searchText);
    Task UpdateAsync(Page page);
    Task DeleteAsync(Page page);
}
