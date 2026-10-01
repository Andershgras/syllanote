using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Syllanote.Application.Notebooks.CreateNotebook;
using Syllanote.Application.Notebooks.Concepts.CreateConcept;
using Syllanote.Application.Notebooks.Concepts.DeleteConcept;
using Syllanote.Application.Notebooks.Concepts.FindConceptReferences;
using Syllanote.Application.Notebooks.Concepts.GetConcepts;
using Syllanote.Application.Notebooks.Concepts.Recognition;
using Syllanote.Application.Notebooks.Concepts.UpdateConcept;
using Syllanote.Application.Notebooks.DeleteNotebook;
using Syllanote.Application.Notebooks.GetNotebooks;
using Syllanote.Application.Notebooks.MoveNotebook;
using Syllanote.Application.Notebooks.RenameNotebook;
using Syllanote.Application.Notebooks.Sections.CreateSection;
using Syllanote.Application.Notebooks.Sections.DeleteSection;
using Syllanote.Application.Notebooks.Sections.GetSections;
using Syllanote.Application.Notebooks.Sections.MoveSection;
using Syllanote.Application.Notebooks.Sections.RenameSection;
using Syllanote.Application.Notebooks.Sections.Pages.CreatePage;
using Syllanote.Application.Notebooks.Sections.Pages.DeletePage;
using Syllanote.Application.Notebooks.Sections.Pages.GetPages;
using Syllanote.Application.Notebooks.Sections.Pages.MovePage;
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
    private int _conceptReferencesLoadVersion;

    private readonly CreateNotebookService _createNotebookService;
    private readonly DeleteNotebookService _deleteNotebookService;
    private readonly GetNotebooksService _getNotebooksService;
    private readonly MoveNotebookService _moveNotebookService;
    private readonly RenameNotebookService _renameNotebookService;
    private readonly GetSectionsService _getSectionsService;
    private readonly CreateSectionService _createSectionService;
    private readonly DeleteSectionService _deleteSectionService;
    private readonly MoveSectionService _moveSectionService;
    private readonly RenameSectionService _renameSectionService;
    private readonly GetPagesService _getPagesService;
    private readonly CreatePageService _createPageService;
    private readonly DeletePageService _deletePageService;
    private readonly MovePageService _movePageService;
    private readonly RenamePageService _renamePageService;
    private readonly UpdatePageContentService _updatePageContentService;
    private readonly SearchPagesService _searchPagesService;
    private readonly CreateConceptService _createConceptService;
    private readonly GetConceptsService _getConceptsService;
    private readonly UpdateConceptService _updateConceptService;
    private readonly DeleteConceptService _deleteConceptService;
    private readonly FindConceptReferencesService _findConceptReferencesService;
    private readonly RecognizeConceptsService _recognizeConceptsService;

    public NotebookViewModel(
        CreateNotebookService createNotebookService,
        DeleteNotebookService deleteNotebookService,
        GetNotebooksService getNotebooksService,
        MoveNotebookService moveNotebookService,
        RenameNotebookService renameNotebookService,
        GetSectionsService getSectionsService,
        CreateSectionService createSectionService,
        DeleteSectionService deleteSectionService,
        MoveSectionService moveSectionService,
        RenameSectionService renameSectionService,
        GetPagesService getPagesService,
        CreatePageService createPageService,
        DeletePageService deletePageService,
        MovePageService movePageService,
        RenamePageService renamePageService,
        UpdatePageContentService updatePageContentService,
        SearchPagesService searchPagesService,
        CreateConceptService createConceptService,
        GetConceptsService getConceptsService,
        UpdateConceptService updateConceptService,
        DeleteConceptService deleteConceptService,
        FindConceptReferencesService findConceptReferencesService,
        RecognizeConceptsService recognizeConceptsService)
    {
        _createNotebookService = createNotebookService;
        _deleteNotebookService = deleteNotebookService;
        _getNotebooksService = getNotebooksService;
        _moveNotebookService = moveNotebookService;
        _renameNotebookService = renameNotebookService;
        _getSectionsService = getSectionsService;
        _createSectionService = createSectionService;
        _deleteSectionService = deleteSectionService;
        _moveSectionService = moveSectionService;
        _renameSectionService = renameSectionService;
        _getPagesService = getPagesService;
        _createPageService = createPageService;
        _deletePageService = deletePageService;
        _movePageService = movePageService;
        _renamePageService = renamePageService;
        _updatePageContentService = updatePageContentService;
        _searchPagesService = searchPagesService;
        _createConceptService = createConceptService;
        _getConceptsService = getConceptsService;
        _updateConceptService = updateConceptService;
        _deleteConceptService = deleteConceptService;
        _findConceptReferencesService = findConceptReferencesService;
        _recognizeConceptsService = recognizeConceptsService;
    }
    public ObservableCollection<Notebook> Notebooks { get; } = [];
    public ObservableCollection<Section> Sections { get; } = [];
    public ObservableCollection<Page> Pages { get; } = [];
    public ObservableCollection<SearchPageResult> SearchResults { get; } = [];
    public ObservableCollection<Concept> Concepts { get; } = [];
    public ObservableCollection<ConceptReference> ConceptReferences { get; } = [];
    public ObservableCollection<ConceptMatch> ConceptMatches { get; } = [];

    public string SearchText { get; set; } = string.Empty;

    private string _searchMessage = string.Empty;
    public string SearchMessage
    {
        get => _searchMessage;
        private set => SetProperty(ref _searchMessage, value);
    }

    private string _pageSaveErrorMessage = string.Empty;
    public string PageSaveErrorMessage
    {
        get => _pageSaveErrorMessage;
        private set
        {
            if (SetProperty(ref _pageSaveErrorMessage, value))
            {
                OnPropertyChanged(nameof(HasPageSaveError));
            }
        }
    }

    public bool HasPageSaveError =>
        !string.IsNullOrWhiteSpace(PageSaveErrorMessage);

    [ObservableProperty]
    public partial string NewNotebookName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedNotebookName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewSectionName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedSectionName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string NewPageTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PageContent { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PageFormattedContent { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedPageTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial Notebook? SelectedNotebook { get; set; }

    [ObservableProperty]
    public partial Section? SelectedSection { get; set; }

    [ObservableProperty]
    public partial Page? SelectedPage { get; set; }

    [ObservableProperty]
    public partial bool IsPageDirty { get; set; }

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
        _conceptReferencesLoadVersion++;
        ConceptReferences.Clear();
        ConceptMatches.Clear();
    }

    [RelayCommand]
    private async Task MoveSelectedNotebookUpAsync()
    {
        var notebook = SelectedNotebook;
        if (notebook is null)
        {
            return;
        }

        var index = Notebooks.IndexOf(notebook);
        if (index > 0 && await _moveNotebookService.MoveUpAsync(notebook))
        {
            Notebooks.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private async Task MoveSelectedNotebookDownAsync()
    {
        var notebook = SelectedNotebook;
        if (notebook is null)
        {
            return;
        }

        var index = Notebooks.IndexOf(notebook);
        if (index >= 0 &&
            index < Notebooks.Count - 1 &&
            await _moveNotebookService.MoveDownAsync(notebook))
        {
            Notebooks.Move(index, index + 1);
        }
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

        if (SelectedPage is not null)
        {
            RefreshConceptMatches(SelectedPage, PageContent);
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

    public async Task LoadConceptReferencesAsync(Concept? concept)
    {
        var loadVersion = ++_conceptReferencesLoadVersion;
        ConceptReferences.Clear();

        if (concept is null ||
            SelectedNotebook?.Id != concept.NotebookId ||
            !Concepts.Any(item => item.Id == concept.Id))
        {
            return;
        }

        var references = await _findConceptReferencesService.FindAsync(concept);
        if (loadVersion != _conceptReferencesLoadVersion ||
            SelectedNotebook?.Id != concept.NotebookId ||
            !Concepts.Any(item => item.Id == concept.Id))
        {
            return;
        }

        foreach (var reference in references)
        {
            ConceptReferences.Add(reference);
        }
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
        if (!await SaveCurrentPageAsync())
        {
            return;
        }

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
        var notebook = SelectedNotebook;
        if (!await SaveCurrentPageAsync())
        {
            return;
        }

        if (SelectedNotebook != notebook)
        {
            return;
        }

        if (notebook is null)
        {
            SelectedPage = null;
            Pages.Clear();
            SelectedSection = null;
            Sections.Clear();
            return;
        }

        await LoadConceptsAsync(notebook.Id);

        if (SelectedNotebook != notebook)
        {
            return;
        }

        var sections =
            await _getSectionsService.GetByNotebookIdAsync(
                notebook.Id);

        if (SelectedNotebook != notebook)
        {
            return;
        }

        SelectedPage = null;
        Pages.Clear();
        SelectedSection = null;
        Sections.Clear();

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
    private async Task MoveSelectedSectionUpAsync()
    {
        var section = SelectedSection;
        if (section is null)
        {
            return;
        }

        var index = Sections.IndexOf(section);
        if (index > 0 && await _moveSectionService.MoveUpAsync(section))
        {
            Sections.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private async Task MoveSelectedSectionDownAsync()
    {
        var section = SelectedSection;
        if (section is null)
        {
            return;
        }

        var index = Sections.IndexOf(section);
        if (index >= 0 &&
            index < Sections.Count - 1 &&
            await _moveSectionService.MoveDownAsync(section))
        {
            Sections.Move(index, index + 1);
        }
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
        if (!await SaveCurrentPageAsync())
        {
            return;
        }

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
        var section = SelectedSection;
        if (!await SaveCurrentPageAsync())
        {
            return;
        }

        if (SelectedSection != section)
        {
            return;
        }

        if (section is null ||
            section.NotebookId != SelectedNotebook?.Id)
        {
            SelectedPage = null;
            Pages.Clear();
            return;
        }

        var pages =
            await _getPagesService.GetBySectionIdAsync(
                section.Id);

        if (SelectedSection != section ||
            section.NotebookId != SelectedNotebook?.Id)
        {
            return;
        }

        SelectedPage = null;
        Pages.Clear();

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
        PageFormattedContent = value?.FormattedContent ?? string.Empty;

        _isLoadingPage = false;

        IsPageDirty = false;

        if (value is not null)
        {
            RefreshConceptMatches(value, PageContent);
        }
    }

    [RelayCommand]
    private async Task MoveSelectedPageUpAsync()
    {
        var page = SelectedPage;
        if (page is null)
        {
            return;
        }

        var index = Pages.IndexOf(page);
        if (index > 0 && await _movePageService.MoveUpAsync(page))
        {
            Pages.Move(index, index - 1);
        }
    }

    [RelayCommand]
    private async Task MoveSelectedPageDownAsync()
    {
        var page = SelectedPage;
        if (page is null)
        {
            return;
        }

        var index = Pages.IndexOf(page);
        if (index >= 0 &&
            index < Pages.Count - 1 &&
            await _movePageService.MoveDownAsync(page))
        {
            Pages.Move(index, index + 1);
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
        if (!await SaveCurrentPageAsync())
        {
            return;
        }

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

    partial void OnPageFormattedContentChanged(string value)
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

    public async Task<bool> SaveCurrentPageAsync()
    {
        await _pagePersistenceLock.WaitAsync();
        try
        {
            if (SelectedPage is null || !IsPageDirty)
            {
                return true;
            }

            var page = SelectedPage;
            var content = PageContent;
            var formattedContent = PageFormattedContent;
            await _updatePageContentService.UpdateAsync(
                page,
                content,
                formattedContent);

            if (SelectedPage == page &&
                PageContent == content &&
                PageFormattedContent == formattedContent)
            {
                IsPageDirty = false;
                RefreshConceptMatches(page, content);
            }

            PageSaveErrorMessage = string.Empty;
            return true;
        }
        catch (Exception)
        {
            PageSaveErrorMessage =
                "Your changes couldn't be saved. They are still in the editor. " +
                "Check that the database is available, then try again.";
            return false;
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
    public async Task<bool> SelectPageAsync(Page? page)
    {
        if (page == SelectedPage)
        {
            return true;
        }

        if (!await SaveCurrentPageAsync())
        {
            return false;
        }

        SelectedPage = page;
        return true;
    }

    [RelayCommand]
    private async Task SearchPagesAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            SearchResults.Clear();
            SearchMessage = "Enter a word or phrase to search.";
            return;
        }

        if (!await SaveCurrentPageAsync())
        {
            SearchResults.Clear();
            SearchMessage =
                "Search was not run because the current page could not be saved.";
            return;
        }
        var results = await _searchPagesService.SearchAsync(SearchText);

        SearchResults.Clear();
        foreach (var result in results)
        {
            SearchResults.Add(result);
        }

        SearchMessage = results.Count switch
        {
            0 => "No pages found.",
            1 => "1 page found.",
            _ => $"{results.Count} pages found."
        };
    }

    public async Task<bool> NavigateToSearchResultAsync(SearchPageResult result)
    {
        var notebook = Notebooks.FirstOrDefault(item => item.Id == result.NotebookId);
        if (notebook is null)
        {
            SearchResults.Remove(result);
            SearchMessage = "This page is no longer available. Search again.";
            return false;
        }

        if (!await SaveCurrentPageAsync())
        {
            return false;
        }
        SelectedNotebook = notebook;
        await LoadSectionsAsync();
        SelectedPage = null;

        var section = Sections.FirstOrDefault(item => item.Id == result.SectionId);
        if (section is null)
        {
            SearchResults.Remove(result);
            SearchMessage = "This page is no longer available. Search again.";
            return false;
        }

        SelectedSection = section;
        await LoadPagesAsync();

        var page = Pages.FirstOrDefault(item => item.Id == result.PageId);
        if (page is null)
        {
            SearchResults.Remove(result);
            SearchMessage = "This page is no longer available. Search again.";
            return false;
        }

        if (!await SelectPageAsync(page))
        {
            return false;
        }

        SearchMessage = string.Empty;
        return true;
    }

    public async Task<bool> NavigateToConceptReferenceAsync(
        ConceptReference reference)
    {
        var notebook = SelectedNotebook;
        if (notebook is null)
        {
            ConceptReferences.Remove(reference);
            return false;
        }

        var section = Sections.FirstOrDefault(item =>
            item.Id == reference.SectionId &&
            item.NotebookId == notebook.Id);
        if (section is null)
        {
            ConceptReferences.Remove(reference);
            return false;
        }

        if (!await SaveCurrentPageAsync())
        {
            return false;
        }
        if (SelectedNotebook != notebook)
        {
            return false;
        }

        SelectedSection = section;
        await LoadPagesAsync();
        if (SelectedNotebook != notebook || SelectedSection != section)
        {
            return false;
        }

        var page = Pages.FirstOrDefault(item => item.Id == reference.PageId);
        if (page is null)
        {
            ConceptReferences.Remove(reference);
            return false;
        }

        return await SelectPageAsync(page);
    }
}
