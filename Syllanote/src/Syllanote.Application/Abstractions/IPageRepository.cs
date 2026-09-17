using Syllanote.Domain.Entities;

namespace Syllanote.Application.Abstractions;

public interface IPageRepository
{
    Task AddAsync(Page page);
    Task<IReadOnlyList<Page>> GetBySectionIdAsync(Guid sectionId);
    Task UpdateAsync(Page page);
}