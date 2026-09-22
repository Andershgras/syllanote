using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syllanote.Application.Notebooks.CreateNotebook;
using Syllanote.Application.Notebooks.Concepts.CreateConcept;
using Syllanote.Application.Notebooks.Concepts.DeleteConcept;
using Syllanote.Application.Notebooks.Concepts.GetConcepts;
using Syllanote.Application.Notebooks.Concepts.Recognition;
using Syllanote.Application.Notebooks.Concepts.UpdateConcept;
using Syllanote.Application.Notebooks.DeleteNotebook;
using Syllanote.Application.Notebooks.GetNotebooks;
using Syllanote.Application.Notebooks.RenameNotebook;
using Syllanote.Application.Notebooks.Sections.CreateSection;
using Syllanote.Application.Notebooks.Sections.DeleteSection;
using Syllanote.Application.Notebooks.Sections.GetSections;
using Syllanote.Application.Notebooks.Sections.RenameSection;
using Syllanote.Application.Notebooks.Sections.Pages.CreatePage;
using Syllanote.Application.Notebooks.Sections.Pages.DeletePage;
using Syllanote.Application.Notebooks.Sections.Pages.GetPages;
using Syllanote.Application.Notebooks.Sections.Pages.RenamePage;
using Syllanote.Application.Notebooks.Sections.Pages.UpdatePageContent;
using Syllanote.Application.Notebooks.Sections.Pages.SearchPages;
using Syllanote.Domain.Entities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Syllanote.Desktop.ViewModels;

public partial class NotebookViewModel : ObservableObject
{
    private CancellationTokenSource? _autoSaveCancellationTokenSource;
    private readonly SemaphoreSlim _pagePersistenceLock = new(1, 1);
    private bool _isLoadingPage;

    private readonly CreateNotebookService _createNotebookService;
    private readonly DeleteNotebookService _deleteNotebookService;
    private readonly GetNotebooksService _getNotebooksService;
    private readonly RenameNotebookService _renameNotebookService;
    private readonly GetSectionsService _getSectionsService;
    private readonly CreateSectionService _createSectionService;
    private readonly DeleteSectionService _deleteSectionService;
    private readonly RenameSectionService _renameSectionService;
    private readonly GetPagesService _getPagesService;
    private readonly CreatePageService _createPageService;
    private readonly DeletePageService _deletePageService;
    private readonly RenamePageService _renamePageService;
    private readonly UpdatePageContentService _updatePageContentService;
    private readonly SearchPagesService _searchPagesService;
    private readonly CreateConceptService _createConceptService;
    private readonly GetConceptsService _getConceptsService;
    private readonly UpdateConceptService _updateConceptService;
    private readonly DeleteConceptService _deleteConceptService;
    private readonly RecognizeConceptsService _recognizeConceptsService;

    public NotebookViewModel(
        CreateNotebookService createNotebookService,
        DeleteNotebookService deleteNotebookService,
        GetNotebooksService getNotebooksService,
        RenameNotebookService renameNotebookService,
        GetSectionsService getSectionsService,
        CreateSectionService createSectionService,
        DeleteSectionService deleteSectionService,
        RenameSectionService renameSectionService,
        GetPagesService getPagesService,
        CreatePageService createPageService,
        DeletePageService deletePageService,
        RenamePageService renamePageService,
        UpdatePageContentService updatePageContentService,
        SearchPagesService searchPagesService,
        CreateConceptService createConceptService,
        GetConceptsService getConceptsService,
        UpdateConceptService updateConceptService,
        DeleteConceptService deleteConceptService,
        RecognizeConceptsService recognizeConceptsService)
    {
        _createNotebookService = createNotebookService;
        _deleteNotebookService = deleteNotebookService;
        _getNotebooksService = getNotebooksService;
        _renameNotebookService = renameNotebookService;
        _getSectionsService = getSectionsService;
        _createSectionService = createSectionService;
        _deleteSectionService = deleteSectionService;
        _renameSectionService = renameSectionService;
        _getPagesService = getPagesService;
        _createPageService = createPageService;
        _deletePageService = deletePageService;
        _renamePageService = renamePageService;
        _updatePageContentService = updatePageContentService;
        _searchPagesService = searchPagesService;
        _createConceptService = createConceptService;
        _getConceptsService = getConceptsService;
        _updateConceptService = updateConceptService;
        _deleteConceptService = deleteConceptService;
        _recognizeConceptsService = recognizeConceptsService;
    }
    public ObservableCollection<Notebook> Notebooks { get; } = [];
    public ObservableCollection<Section> Sections { get; } = [];
    public ObservableCollection<Page> Pages { get; } = [];
    public ObservableCollection<SearchPageResult> SearchResults { get; } = [];
    public ObservableCollection<Concept> Concepts { get; } = [];
    public ObservableCollection<ConceptMatch> ConceptMatches { get; } = [];

    public string SearchText { get; set; } = string.Empty;

    private string _searchMessage = string.Empty;
    public string SearchMessage
    {
        get => _searchMessage;
        private set => SetProperty(ref _searchMessage, value);
    }

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
        Concepts.Clear();
        ConceptMatches.Clear();
    }

    public async Task LoadConceptsAsync(Guid notebookId)
    {
        var concepts = await _getConceptsService.GetByNotebookIdAsync(notebookId);
        if (SelectedNotebook?.Id != notebookId)
        {
            return;
        }

        Concepts.Clear();
        foreach (var concept in concepts)
        {
            Concepts.Add(concept);
        }
    }

    public Task<Concept> CreateConceptAsync(
        Guid notebookId, string name, string definition)
    {
        return _createConceptService.CreateAsync(notebookId, name, definition);
    }

    public Task UpdateConceptAsync(Concept concept, string name, string definition)
    {
        return _updateConceptService.UpdateAsync(concept, name, definition);
    }

    public Task DeleteConceptAsync(Concept concept)
    {
        return _deleteConceptService.DeleteAsync(concept);
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
    private async Task DeleteNotebookAsync()
    {
        var notebook = SelectedNotebook;
        if (notebook is null)
        {
            return;
        }

        _autoSaveCancellationTokenSource?.Cancel();

        await _pagePersistenceLock.WaitAsync();
        try
        {
            if (SelectedNotebook != notebook)
            {
                return;
            }

            await _deleteNotebookService.DeleteAsync(notebook);
            SelectedPage = null;
            Pages.Clear();
            SelectedSection = null;
            Sections.Clear();
            SelectedNotebook = null;
            Notebooks.Remove(notebook);
        }
        finally
        {
            _pagePersistenceLock.Release();
        }
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

        await LoadConceptsAsync(SelectedNotebook.Id);

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
    private async Task DeleteSectionAsync()
    {
        var section = SelectedSection;
        if (section is null)
        {
            return;
        }

        _autoSaveCancellationTokenSource?.Cancel();

        await _pagePersistenceLock.WaitAsync();
        try
        {
            if (SelectedSection != section)
            {
                return;
            }

            await _deleteSectionService.DeleteAsync(section);
            SelectedPage = null;
            Pages.Clear();
            SelectedSection = null;
            Sections.Remove(section);
        }
        finally
        {
            _pagePersistenceLock.Release();
        }
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
        ConceptMatches.Clear();

        _isLoadingPage = true;

        SelectedPageTitle = value?.Title ?? string.Empty;
        PageContent = value?.Content ?? string.Empty;

        _isLoadingPage = false;

        IsPageDirty = false;

        if (value is not null)
        {
            RefreshConceptMatches(value, PageContent);
        }
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
                RefreshConceptMatches(page, content);
            }
        }
        finally
        {
            _pagePersistenceLock.Release();
        }
    }
    private void RefreshConceptMatches(Page page, string content)
    {
        if (SelectedPage != page || PageContent != content)
        {
            return;
        }

        var matches = _recognizeConceptsService.Recognize(content, Concepts);

        ConceptMatches.Clear();
        foreach (var match in matches)
        {
            ConceptMatches.Add(match);
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

    [RelayCommand]
    private async Task SearchPagesAsync()
    {
        await SaveCurrentPageAsync();
        var results = await _searchPagesService.SearchAsync(SearchText);

        SearchResults.Clear();
        foreach (var result in results)
        {
            SearchResults.Add(result);
        }

        SearchMessage = string.IsNullOrWhiteSpace(SearchText)
            ? string.Empty
            : results.Count == 0 ? "No pages found." : string.Empty;
    }

    public async Task<bool> NavigateToSearchResultAsync(SearchPageResult result)
    {
        var notebook = Notebooks.FirstOrDefault(item => item.Id == result.NotebookId);
        if (notebook is null)
        {
            SearchMessage = "This page is no longer available. Search again.";
            return false;
        }

        await SaveCurrentPageAsync();
        SelectedNotebook = notebook;
        await LoadSectionsAsync();
        SelectedPage = null;

        var section = Sections.FirstOrDefault(item => item.Id == result.SectionId);
        if (section is null)
        {
            SearchMessage = "This page is no longer available. Search again.";
            return false;
        }

        SelectedSection = section;
        await LoadPagesAsync();

        var page = Pages.FirstOrDefault(item => item.Id == result.PageId);
        if (page is null)
        {
            SearchMessage = "This page is no longer available. Search again.";
            return false;
        }

        await SelectPageAsync(page);
        SearchMessage = string.Empty;
        return true;
    }
}
