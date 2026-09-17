using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.DeleteNotebook;

public class DeleteNotebookService
{
    private readonly INotebookRepository _notebookRepository;

    public DeleteNotebookService(INotebookRepository notebookRepository)
    {
        _notebookRepository = notebookRepository;
    }

    public Task DeleteAsync(Notebook notebook)
    {
        return _notebookRepository.DeleteAsync(notebook);
    }
}
