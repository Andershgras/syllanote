using Syllanote.Domain.Entities;

namespace Syllanote.Application.Abstractions;

public interface INotebookRepository
{
    Task AddAsync(Notebook notebook);
}