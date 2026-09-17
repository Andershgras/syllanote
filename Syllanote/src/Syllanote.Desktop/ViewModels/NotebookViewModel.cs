using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syllanote.Application.Notebooks.CreateNotebook;
using Syllanote.Application.Notebooks.GetNotebooks;
using Syllanote.Application.Notebooks.RenameNotebook;
using Syllanote.Application.Notebooks.Sections.CreateSection;
using Syllanote.Application.Notebooks.Sections.GetSections;
using Syllanote.Application.Notebooks.Sections.RenameSection;
using Syllanote.Application.Notebooks.Sections.Pages.CreatePage;
using Syllanote.Application.Notebooks.Sections.Pages.DeletePage;
using Syllanote.Application.Notebooks.Sections.Pages.GetPages;
using Syllanote.Application.Notebooks.Sections.Pages.RenamePage;
using Syllanote.Application.Notebooks.Sections.Pages.UpdatePageContent;
using Syllanote.Domain.Entities;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace Syllanote.Desktop.ViewModels;

public partial class NotebookViewModel : ObservableObject
{
    private CancellationTokenSource? _autoSaveCancellationTokenSource;
    private readonly SemaphoreSlim _pagePersistenceLock = new(1, 1);
    private bool _isLoadingPage;

    private readonly CreateNotebookService _createNotebookService;
    private readonly GetNotebooksService _getNotebooksService;
    private readonly RenameNotebookService _renameNotebookService;
    private readonly GetSectionsService _getSectionsService;
    private readonly CreateSectionService _createSectionService;
    private readonly RenameSectionService _renameSectionService;
    private readonly GetPagesService _getPagesService;
    private readonly CreatePageService _createPageService;
    private readonly DeletePageService _deletePageService;
    private readonly RenamePageService _renamePageService;
    private readonly UpdatePageContentService _updatePageContentService;

    public NotebookViewModel(
        CreateNotebookService createNotebookService,
        GetNotebooksService getNotebooksService,
        RenameNotebookService renameNotebookService,
        GetSectionsService getSectionsService,
        CreateSectionService createSectionService,
        RenameSectionService renameSectionService,
        GetPagesService getPagesService,
        CreatePageService createPageService,
        DeletePageService deletePageService,
        RenamePageService renamePageService,
        UpdatePageContentService updatePageContentService)
    {
        _createNotebookService = createNotebookService;
        _getNotebooksService = getNotebooksService;
        _renameNotebookService = renameNotebookService;
        _getSectionsService = getSectionsService;
        _createSectionService = createSectionService;
        _renameSectionService = renameSectionService;
        _getPagesService = getPagesService;
        _createPageService = createPageService;
        _deletePageService = deletePageService;
        _renamePageService = renamePageService;
        _updatePageContentService = updatePageContentService;
    }
    public ObservableCollection<Notebook> Notebooks { get; } = [];
    public ObservableCollection<Section> Sections { get; } = [];
    public ObservableCollection<Page> Pages { get; } = [];

    [ObservableProperty]
    private string _newNotebookName = string.Empty;

    [ObservableProperty]
    private string _selectedNotebookName = string.Empty;
    [ObservableProperty]
    private string _newSectionName = string.Empty;

    [ObservableProperty]
    private string _selectedSectionName = string.Empty;
    [ObservableProperty]
    private string _newPageTitle = string.Empty;

    [ObservableProperty]
    private string _pageContent = string.Empty;

    [ObservableProperty]
    private string _selectedPageTitle = string.Empty;

    [ObservableProperty]
    private Notebook? _selectedNotebook;
    [ObservableProperty]
    private Section? _selectedSection;
    [ObservableProperty]
    private Page? _selectedPage;

    [ObservableProperty]
    private bool _isPageDirty;

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

    partial void OnSelectedNotebookChanged(Notebook? value)
    {
        SelectedNotebookName = value?.Name ?? string.Empty;
    }

    [RelayCommand]
    private async Task RenameNotebookAsync()
    {
        if (SelectedNotebook is null ||
            string.IsNullOrWhiteSpace(SelectedNotebookName))
        {
            return;
        }

        _autoSaveCancellationTokenSource?.Cancel();
        await SaveCurrentPageAsync();

        await _renameNotebookService.RenameAsync(
            SelectedNotebook,
            SelectedNotebookName);

        var index = Notebooks.IndexOf(SelectedNotebook);
        if (index >= 0)
        {
            Notebooks[index] = SelectedNotebook;
        }

        OnPropertyChanged(nameof(SelectedNotebook));
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
        await SaveCurrentPageAsync();

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

    partial void OnSelectedSectionChanged(Section? value)
    {
        SelectedSectionName = value?.Name ?? string.Empty;
    }

    [RelayCommand]
    private async Task RenameSectionAsync()
    {
        if (SelectedSection is null ||
            string.IsNullOrWhiteSpace(SelectedSectionName))
        {
            return;
        }

        _autoSaveCancellationTokenSource?.Cancel();
        await SaveCurrentPageAsync();

        await _renameSectionService.RenameAsync(
            SelectedSection,
            SelectedSectionName);

        var index = Sections.IndexOf(SelectedSection);
        if (index >= 0)
        {
            Sections[index] = SelectedSection;
        }

        OnPropertyChanged(nameof(SelectedSection));
    }
    [RelayCommand]
    private async Task LoadPagesAsync()
    {
        await SaveCurrentPageAsync();

        SelectedPage = null;
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
    partial void OnSelectedPageChanged(Page? value)
    {
        _autoSaveCancellationTokenSource?.Cancel();

        _isLoadingPage = true;

        SelectedPageTitle = value?.Title ?? string.Empty;
        PageContent = value?.Content ?? string.Empty;

        _isLoadingPage = false;

        IsPageDirty = false;
    }

    [RelayCommand]
    private async Task RenamePageAsync()
    {
        if (SelectedPage is null ||
            string.IsNullOrWhiteSpace(SelectedPageTitle))
        {
            return;
        }

        _autoSaveCancellationTokenSource?.Cancel();
        await SaveCurrentPageAsync();

        await _renamePageService.RenameAsync(
            SelectedPage,
            SelectedPageTitle);

        var index = Pages.IndexOf(SelectedPage);
        if (index >= 0)
        {
            Pages[index] = SelectedPage;
        }
    }

    [RelayCommand]
    private async Task DeletePageAsync()
    {
        var page = SelectedPage;
        if (page is null)
        {
            return;
        }

        _autoSaveCancellationTokenSource?.Cancel();

        await _pagePersistenceLock.WaitAsync();
        try
        {
            if (SelectedPage != page)
            {
                return;
            }

            await _deletePageService.DeleteAsync(page);
            SelectedPage = null;
            Pages.Remove(page);
        }
        finally
        {
            _pagePersistenceLock.Release();
        }
    }
    partial void OnPageContentChanged(string value)
    {
        if (_isLoadingPage)
        {
            return;
        }

        IsPageDirty = true;

        _ = ScheduleAutoSaveAsync();
    }

    [RelayCommand]
    private async Task SavePageAsync()
    {
        await SaveCurrentPageAsync();
    }
    public async Task SaveCurrentPageAsync()
    {
        await _pagePersistenceLock.WaitAsync();
        try
        {
            if (SelectedPage is null || !IsPageDirty)
            {
                return;
            }

            var page = SelectedPage;
            var content = PageContent;
            await _updatePageContentService.UpdateAsync(page, content);

            if (SelectedPage == page && PageContent == content)
            {
                IsPageDirty = false;
            }
        }
        finally
        {
            _pagePersistenceLock.Release();
        }
    }
    private async Task ScheduleAutoSaveAsync()
    {
        _autoSaveCancellationTokenSource?.Cancel();
        _autoSaveCancellationTokenSource?.Dispose();

        _autoSaveCancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            _autoSaveCancellationTokenSource.Token;

        try
        {
            await Task.Delay(
                TimeSpan.FromSeconds(1),
                cancellationToken);

            await SaveCurrentPageAsync();
        }
        catch (OperationCanceledException)
        {
            // Expected when the user continues typing.
        }
    }
    public async Task SelectPageAsync(Page? page)
    {
        if (page == SelectedPage)
        {
            return;
        }

        await SaveCurrentPageAsync();

        SelectedPage = page;
    }
}
