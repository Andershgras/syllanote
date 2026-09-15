using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syllanote.Application.Notebooks.CreateNotebook;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Syllanote.Domain.Entities;
using Syllanote.Application.Notebooks.GetNotebooks;

namespace Syllanote.Desktop.ViewModels;

public partial class NotebookViewModel : ObservableObject
{
    private readonly CreateNotebookService _createNotebookService;
    private readonly GetNotebooksService _getNotebooksService;

    public NotebookViewModel(
        CreateNotebookService createNotebookService,
        GetNotebooksService getNotebooksService)
    {
        _createNotebookService = createNotebookService;
        _getNotebooksService = getNotebooksService;
    }
    public ObservableCollection<Notebook> Notebooks { get; } = [];

    [ObservableProperty]
    private string _newNotebookName = string.Empty;

    [RelayCommand]
    private async Task CreateNotebookAsync()
    {
        if (string.IsNullOrWhiteSpace(NewNotebookName))
        {
            return;
        }

        var notebook =
            await _createNotebookService.CreateAsync(NewNotebookName);

        Notebooks.Add(notebook);

        NewNotebookName = string.Empty;
    }
    [RelayCommand]
    private async Task LoadNotebooksAsync()
    {
        var notebooks = await _getNotebooksService.GetAllAsync();

        Notebooks.Clear();

        foreach (var notebook in notebooks)
        {
            Notebooks.Add(notebook);
        }
    }
}