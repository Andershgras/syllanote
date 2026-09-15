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

        await _notebookRepository.AddAsync(notebook);

        return notebook;
    }
}