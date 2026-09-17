using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.RenameNotebook;

public class RenameNotebookService
{
    private readonly INotebookRepository _notebookRepository;

    public RenameNotebookService(INotebookRepository notebookRepository)
    {
        _notebookRepository = notebookRepository;
    }

    public async Task RenameAsync(Notebook notebook, string name)
    {
        notebook.Rename(name);
        await _notebookRepository.UpdateAsync(notebook);
    }
}
