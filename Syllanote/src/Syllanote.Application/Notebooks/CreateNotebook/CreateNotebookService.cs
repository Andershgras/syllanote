using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.CreateNotebook;

public class CreateNotebookService
{
    private readonly INotebookRepository _notebookRepository;

    public CreateNotebookService(INotebookRepository notebookRepository)
    {
        _notebookRepository = notebookRepository;
    }

    public async Task<Notebook> CreateAsync(string name)
    {
        var notebook = new Notebook(name);
        var notebooks = await _notebookRepository.GetAllAsync();
        var sortOrder = notebooks.Count == 0
            ? 0
            : notebooks.Max(notebook => notebook.SortOrder) + 1;
        notebook.ChangeSortOrder(sortOrder);

        await _notebookRepository.AddAsync(notebook);

        return notebook;
    }
}
