using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.GetNotebooks;

public class GetNotebooksService
{
    private readonly INotebookRepository _notebookRepository;

    public GetNotebooksService(INotebookRepository notebookRepository)
    {
        _notebookRepository = notebookRepository;
    }

    public async Task<IReadOnlyList<Notebook>> GetAllAsync()
    {
        return await _notebookRepository.GetAllAsync();
    }
}