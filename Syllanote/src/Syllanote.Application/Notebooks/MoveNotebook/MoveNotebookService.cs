using Syllanote.Application.Abstractions;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.MoveNotebook;

public class MoveNotebookService
{
    private readonly INotebookRepository _notebookRepository;

    public MoveNotebookService(INotebookRepository notebookRepository)
    {
        _notebookRepository = notebookRepository;
    }

    public Task<bool> MoveUpAsync(Notebook notebook)
    {
        return MoveAsync(notebook, -1);
    }

    public Task<bool> MoveDownAsync(Notebook notebook)
    {
        return MoveAsync(notebook, 1);
    }

    private async Task<bool> MoveAsync(Notebook notebook, int offset)
    {
        var notebooks = (await _notebookRepository.GetAllAsync()).ToList();
        var currentIndex = notebooks.FindIndex(item => item.Id == notebook.Id);
        var targetIndex = currentIndex + offset;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= notebooks.Count)
        {
            return false;
        }

        (notebooks[currentIndex], notebooks[targetIndex]) =
            (notebooks[targetIndex], notebooks[currentIndex]);

        for (var index = 0; index < notebooks.Count; index++)
        {
            var item = notebooks[index];
            if (item.SortOrder == index)
            {
                continue;
            }

            item.ChangeSortOrder(index);
            await _notebookRepository.UpdateAsync(item);
        }

        return true;
    }
}
