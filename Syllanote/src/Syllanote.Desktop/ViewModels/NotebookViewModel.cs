using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syllanote.Application.Notebooks.CreateNotebook;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Syllanote.Domain.Entities;
using Syllanote.Application.Notebooks.GetNotebooks;
using Syllanote.Application.Notebooks.Sections.GetSections;
using Syllanote.Application.Notebooks.Sections.CreateSection;
using Syllanote.Application.Notebooks.Sections.Pages.GetPages;
using Syllanote.Application.Notebooks.Sections.Pages.CreatePage;

namespace Syllanote.Desktop.ViewModels;

public partial class NotebookViewModel : ObservableObject
{
    private readonly CreateNotebookService _createNotebookService;
    private readonly GetNotebooksService _getNotebooksService;
    private readonly GetSectionsService _getSectionsService;
    private readonly CreateSectionService _createSectionService;
    private readonly GetPagesService _getPagesService;
    private readonly CreatePageService _createPageService;

    public NotebookViewModel(
        CreateNotebookService createNotebookService,
        GetNotebooksService getNotebooksService,
        GetSectionsService getSectionsService,
        CreateSectionService createSectionService,
        GetPagesService getPagesService,
        CreatePageService createPageService)
    {
        _createNotebookService = createNotebookService;
        _getNotebooksService = getNotebooksService;
        _getSectionsService = getSectionsService;
        _createSectionService = createSectionService;
        _getPagesService = getPagesService;
        _createPageService = createPageService;
    }
    public ObservableCollection<Notebook> Notebooks { get; } = [];
    public ObservableCollection<Section> Sections { get; } = [];
    public ObservableCollection<Page> Pages { get; } = [];
    [ObservableProperty]
    private string _newNotebookName = string.Empty;
    [ObservableProperty]
    private string _newSectionName = string.Empty;
    [ObservableProperty]
    private string _newPageTitle = string.Empty;
    [ObservableProperty]
    private Notebook? _selectedNotebook;
    [ObservableProperty]
    private Section? _selectedSection;

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
    [RelayCommand]
    private async Task LoadSectionsAsync()
    {
        SelectedSection = null;
        Sections.Clear();
        Pages.Clear();

        if (SelectedNotebook is null)
        {
            return;
        }

        var sections =
            await _getSectionsService.GetByNotebookIdAsync(
                SelectedNotebook.Id);

        foreach (var section in sections)
        {
            Sections.Add(section);
        }
    }
    [RelayCommand]
    private async Task CreateSectionAsync()
    {
        if (SelectedNotebook is null ||
            string.IsNullOrWhiteSpace(NewSectionName))
        {
            return;
        }

        var section =
            await _createSectionService.CreateAsync(
                SelectedNotebook.Id,
                NewSectionName);

        Sections.Add(section);

        NewSectionName = string.Empty;
    }
    [RelayCommand]
    private async Task LoadPagesAsync()
    {
        Pages.Clear();

        if (SelectedSection is null)
        {
            return;
        }

        var pages =
            await _getPagesService.GetBySectionIdAsync(
                SelectedSection.Id);

        foreach (var page in pages)
        {
            Pages.Add(page);
        }
    }
    [RelayCommand]
    private async Task CreatePageAsync()
    {
        if (SelectedSection is null ||
            string.IsNullOrWhiteSpace(NewPageTitle))
        {
            return;
        }

        var page =
            await _createPageService.CreateAsync(
                SelectedSection.Id,
                NewPageTitle);

        Pages.Add(page);

        NewPageTitle = string.Empty;
    }
}