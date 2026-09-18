using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syllanote.Desktop.ViewModels;
using Syllanote.Application.Notebooks.Concepts;
using Syllanote.Application.Notebooks.Sections.Pages.SearchPages;
using Syllanote.Domain.Entities;
using System;
using System.Linq;
using System.Threading;

namespace Syllanote.Desktop
{
    public sealed partial class MainWindow : Window
    {
        private bool _isRenamingSelection;
        private bool _isSearchNavigationInProgress;
        private bool _isConceptOperationInProgress;
        private readonly SemaphoreSlim _selectionNavigationLock = new(1, 1);
        private int _selectionNavigationVersion;

        public NotebookViewModel ViewModel { get; }
        private void SetNavigationEnabled(bool isEnabled)
        {
            NotebookListView.IsEnabled = isEnabled;
            SectionListView.IsEnabled = isEnabled;
            PagesListView.IsEnabled = isEnabled;
        }

        public MainWindow(NotebookViewModel viewModel)
        {
            InitializeComponent();
            Title = "Syllanote";

            ViewModel = viewModel;
            ViewModel.Notebooks.CollectionChanged += (_, _) => UpdateNotebookEmptyState();
            ViewModel.Sections.CollectionChanged += (_, _) => UpdateSectionState();
            ViewModel.Pages.CollectionChanged += (_, _) => UpdatePageState();
            ViewModel.Concepts.CollectionChanged += (_, _) => UpdateConceptEditorState();
            ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.SelectedNotebook) ||
                    e.PropertyName == nameof(ViewModel.SelectedSection))
                {
                    UpdateSectionState();
                    UpdatePageState();
                }
                else if (e.PropertyName == nameof(ViewModel.SelectedPage))
                {
                    UpdatePageState();
                }
            };
            UpdateNotebookEmptyState();
            UpdateSectionState();
            UpdatePageState();
            UpdateConceptEditorState();
        }

        private void ShowPageEditor()
        {
            ConceptDictionaryPanel.Visibility = Visibility.Collapsed;
            PageEditorPanel.Visibility = Visibility.Visible;
        }

        private void UpdateConceptEditorState()
        {
            var selected = ConceptsListView.SelectedItem as Concept;
            var hasCurrentConcept = selected is not null &&
                selected.NotebookId == ViewModel.SelectedNotebook?.Id;
            SaveConceptButton.Content = hasCurrentConcept ? "Save" : "Create";
            SaveConceptButton.IsEnabled = !_isConceptOperationInProgress &&
                ViewModel.SelectedNotebook is not null &&
                !string.IsNullOrWhiteSpace(ConceptNameTextBox.Text) &&
                !string.IsNullOrWhiteSpace(ConceptDefinitionTextBox.Text);
            DeleteConceptButton.IsEnabled = !_isConceptOperationInProgress &&
                hasCurrentConcept;
            ConceptEmptyState.Visibility = ViewModel.Concepts.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void SetConceptOperationInProgress(bool isInProgress)
        {
            _isConceptOperationInProgress = isInProgress;
            SetNavigationEnabled(!isInProgress);
            SearchButton.IsEnabled = !isInProgress;
            SearchTextBox.IsEnabled = !isInProgress;
            NotebookActionsButton.IsEnabled = !isInProgress &&
                ViewModel.SelectedNotebook is not null;
            NewConceptButton.IsEnabled = !isInProgress;
            ConceptsListView.IsEnabled = !isInProgress;
            ConceptNameTextBox.IsEnabled = !isInProgress;
            ConceptDefinitionTextBox.IsEnabled = !isInProgress;
            UpdateConceptEditorState();
        }
        private void UpdateNotebookEmptyState()
        {
            NotebookEmptyState.Visibility = ViewModel.Notebooks.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        private void UpdateSectionState()
        {
            var hasNotebook = ViewModel.SelectedNotebook is not null;
            NewSectionButton.IsEnabled = hasNotebook;
            SectionActionsButton.IsEnabled =
                ViewModel.SelectedSection is not null &&
                ViewModel.SelectedSection.NotebookId == ViewModel.SelectedNotebook?.Id;
            SectionEmptyState.Text = hasNotebook
                ? "No sections yet. Create one to get started."
                : "Select a notebook to see its sections.";
            SectionEmptyState.Visibility = !hasNotebook || ViewModel.Sections.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
        private void UpdatePageState()
        {
            var hasSection =
                ViewModel.SelectedSection is not null &&
                ViewModel.SelectedSection.NotebookId == ViewModel.SelectedNotebook?.Id;
            var hasPage = IsSelectedPageCurrent();
            NewPageButton.IsEnabled = hasSection;
            PageActionsButton.IsEnabled = hasPage;
            PageEmptyState.Text = hasSection
                ? "No pages yet. Create one to get started."
                : "Select a section to see its pages.";
            PageEmptyState.Visibility = !hasSection || ViewModel.Pages.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
            EditorPageTitle.Visibility = hasPage ? Visibility.Visible : Visibility.Collapsed;
            PageContentTextBox.Visibility = hasPage ? Visibility.Visible : Visibility.Collapsed;
            EditorEmptyState.Visibility = hasPage ? Visibility.Collapsed : Visibility.Visible;
        }
        private bool IsSelectedPageCurrent()
        {
            var page = ViewModel.SelectedPage;
            return page is not null &&
                page.SectionId == ViewModel.SelectedSection?.Id &&
                ViewModel.SelectedSection?.NotebookId == ViewModel.SelectedNotebook?.Id &&
                ReferenceEquals(PagesListView.SelectedItem, page);
        }
        private async void RootGrid_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await ViewModel.LoadNotebooksCommand.ExecuteAsync(null);
        }

        private async void ConceptDictionaryMenuItem_Click(
            object sender, RoutedEventArgs e)
        {
            if (_isRenamingSelection || _isSearchNavigationInProgress ||
                _isConceptOperationInProgress ||
                ViewModel.SelectedNotebook is not Notebook notebook)
            {
                return;
            }

            SetConceptOperationInProgress(true);
            await _selectionNavigationLock.WaitAsync();
            try
            {
                if (ViewModel.SelectedNotebook != notebook)
                {
                    return;
                }

                await ViewModel.SaveCurrentPageAsync();
                await ViewModel.LoadConceptsAsync(notebook.Id);
                if (ViewModel.SelectedNotebook != notebook)
                {
                    return;
                }

                ConceptsListView.SelectedItem = null;
                ConceptNameTextBox.Text = string.Empty;
                ConceptDefinitionTextBox.Text = string.Empty;
                ConceptMessage.Text = string.Empty;
                PageEditorPanel.Visibility = Visibility.Collapsed;
                ConceptDictionaryPanel.Visibility = Visibility.Visible;
            }
            finally
            {
                _selectionNavigationLock.Release();
                SetConceptOperationInProgress(false);
            }
        }

        private void ConceptBackButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_isConceptOperationInProgress)
            {
                ShowPageEditor();
            }
        }

        private void NewConceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConceptOperationInProgress)
            {
                return;
            }

            ConceptsListView.SelectedItem = null;
            ConceptNameTextBox.Text = string.Empty;
            ConceptDefinitionTextBox.Text = string.Empty;
            ConceptMessage.Text = string.Empty;
            UpdateConceptEditorState();
        }

        private void ConceptsListView_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel is null)
            {
                return;
            }

            if (ConceptsListView.SelectedItem is Concept concept)
            {
                ConceptNameTextBox.Text = concept.Name;
                ConceptDefinitionTextBox.Text = concept.Definition;
            }
            else
            {
                ConceptNameTextBox.Text = string.Empty;
                ConceptDefinitionTextBox.Text = string.Empty;
            }

            ConceptMessage.Text = string.Empty;
            UpdateConceptEditorState();
        }

        private void ConceptInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (ViewModel is not null)
            {
                ConceptMessage.Text = string.Empty;
                UpdateConceptEditorState();
            }
        }

        private async void SaveConceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConceptOperationInProgress ||
                ViewModel.SelectedNotebook is not Notebook notebook)
            {
                return;
            }

            var selected = ConceptsListView.SelectedItem as Concept;
            if (selected is not null && selected.NotebookId != notebook.Id)
            {
                return;
            }

            var name = ConceptNameTextBox.Text;
            var definition = ConceptDefinitionTextBox.Text;
            SetConceptOperationInProgress(true);
            await _selectionNavigationLock.WaitAsync();
            try
            {
                if (ViewModel.SelectedNotebook != notebook)
                {
                    return;
                }

                var saved = selected;
                if (saved is null)
                {
                    saved = await ViewModel.CreateConceptAsync(
                        notebook.Id, name, definition);
                }
                else
                {
                    await ViewModel.UpdateConceptAsync(saved, name, definition);
                }

                await ViewModel.LoadConceptsAsync(notebook.Id);
                ConceptsListView.SelectedItem = ViewModel.Concepts
                    .FirstOrDefault(concept => concept.Id == saved.Id);
                ConceptMessage.Text = "Concept saved.";
            }
            catch (DuplicateConceptNameException ex)
            {
                ConceptMessage.Text = ex.Message;
            }
            catch (ArgumentException ex)
            {
                ConceptMessage.Text = ex.Message;
            }
            finally
            {
                _selectionNavigationLock.Release();
                SetConceptOperationInProgress(false);
            }
        }

        private async void DeleteConceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConceptOperationInProgress ||
                ViewModel.SelectedNotebook is not Notebook notebook ||
                ConceptsListView.SelectedItem is not Concept concept ||
                concept.NotebookId != notebook.Id)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = "Delete concept?",
                Content = $"Delete \"{concept.Name}\"? This cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            if (await dialog.ShowAsync() != ContentDialogResult.Primary ||
                ViewModel.SelectedNotebook != notebook ||
                ConceptsListView.SelectedItem != concept)
            {
                return;
            }

            SetConceptOperationInProgress(true);
            await _selectionNavigationLock.WaitAsync();
            try
            {
                if (ViewModel.SelectedNotebook != notebook)
                {
                    return;
                }

                await ViewModel.DeleteConceptAsync(concept);
                await ViewModel.LoadConceptsAsync(notebook.Id);
                ConceptMessage.Text = "Concept deleted.";
            }
            finally
            {
                _selectionNavigationLock.Release();
                SetConceptOperationInProgress(false);
            }
        }
        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isRenamingSelection || _isSearchNavigationInProgress)
            {
                return;
            }

            SearchButton.IsEnabled = false;
            try
            {
                ViewModel.SearchText = SearchTextBox.Text;
                await ViewModel.SearchPagesCommand.ExecuteAsync(null);
            }
            finally
            {
                SearchButton.IsEnabled = true;
            }
        }

        private async void SearchResultsListView_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (_isRenamingSelection || _isSearchNavigationInProgress ||
                SearchResultsListView.SelectedItem is not SearchPageResult result)
            {
                return;
            }

            _isSearchNavigationInProgress = true;
            _selectionNavigationVersion++;
            SetNavigationEnabled(false);
            SearchButton.IsEnabled = false;
            SearchTextBox.IsEnabled = false;
            SearchResultsListView.IsEnabled = false;
            await _selectionNavigationLock.WaitAsync();
            try
            {
                if (await ViewModel.NavigateToSearchResultAsync(result))
                {
                    ShowPageEditor();
                }
                NotebookListView.SelectedItem = ViewModel.SelectedNotebook;
                SectionListView.SelectedItem = ViewModel.SelectedSection;
                PagesListView.SelectedItem = ViewModel.SelectedPage;
                NotebookActionsButton.IsEnabled = ViewModel.SelectedNotebook is not null;
                UpdatePageState();
                SearchResultsListView.SelectedItem = null;
            }
            finally
            {
                _selectionNavigationLock.Release();
                SearchResultsListView.IsEnabled = true;
                SearchTextBox.IsEnabled = true;
                SearchButton.IsEnabled = true;
                SetNavigationEnabled(true);
                _isSearchNavigationInProgress = false;
            }
        }
        private async void NewNotebookButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection) return;

            var nameTextBox = new TextBox
            {
                Header = "Notebook name",
                PlaceholderText = "e.g. Machine Learning"
            };

            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = "New notebook",
                Content = nameTextBox,
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = false
            };

            nameTextBox.TextChanged += (_, _) =>
                dialog.IsPrimaryButtonEnabled =
                    !string.IsNullOrWhiteSpace(nameTextBox.Text);

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                ViewModel.NewNotebookName = nameTextBox.Text;
                await ViewModel.CreateNotebookCommand.ExecuteAsync(null);
            }
        }
        private async void NewSectionButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection) return;

            var notebook = ViewModel.SelectedNotebook;
            if (notebook is null)
            {
                return;
            }

            var nameTextBox = new TextBox
            {
                Header = "Section name",
                PlaceholderText = "e.g. Classification"
            };

            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = "New section",
                Content = nameTextBox,
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = false
            };

            nameTextBox.TextChanged += (_, _) =>
                dialog.IsPrimaryButtonEnabled =
                    !string.IsNullOrWhiteSpace(nameTextBox.Text);

            if (await dialog.ShowAsync() == ContentDialogResult.Primary &&
                ViewModel.SelectedNotebook == notebook)
            {
                ViewModel.NewSectionName = nameTextBox.Text;
                await ViewModel.CreateSectionCommand.ExecuteAsync(null);
            }
        }
        private async void NewPageButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection) return;

            var section = ViewModel.SelectedSection;
            if (section is null ||
                section.NotebookId != ViewModel.SelectedNotebook?.Id)
            {
                return;
            }

            var titleTextBox = new TextBox
            {
                Header = "Page title",
                PlaceholderText = "e.g. Confusion Matrix"
            };

            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = "New page",
                Content = titleTextBox,
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                IsPrimaryButtonEnabled = false
            };

            titleTextBox.TextChanged += (_, _) =>
                dialog.IsPrimaryButtonEnabled =
                    !string.IsNullOrWhiteSpace(titleTextBox.Text);

            if (await dialog.ShowAsync() == ContentDialogResult.Primary &&
                ViewModel.SelectedSection == section &&
                ViewModel.SelectedNotebook?.Id == section.NotebookId)
            {
                ViewModel.NewPageTitle = titleTextBox.Text;
                await ViewModel.CreatePageCommand.ExecuteAsync(null);
            }
        }
        private async void NotebookListView_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_isRenamingSelection || _isSearchNavigationInProgress)
            {
                return;
            }

            var navigationVersion = _selectionNavigationVersion;
            await _selectionNavigationLock.WaitAsync();
            try
            {
                if (navigationVersion != _selectionNavigationVersion ||
                    _isSearchNavigationInProgress)
                {
                    return;
                }

                ShowPageEditor();
                NotebookActionsButton.IsEnabled = ViewModel.SelectedNotebook is not null;
                await ViewModel.LoadSectionsCommand.ExecuteAsync(null);
            }
            finally
            {
                _selectionNavigationLock.Release();
            }
        }
        private async void RenameNotebookMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection) return;

            var notebook = ViewModel.SelectedNotebook;
            if (notebook is null)
            {
                return;
            }

            var nameTextBox = new TextBox
            {
                Header = "Notebook name",
                Text = notebook.Name
            };

            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = "Rename notebook",
                Content = nameTextBox,
                PrimaryButtonText = "Rename",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            nameTextBox.TextChanged += (_, _) =>
                dialog.IsPrimaryButtonEnabled =
                    !string.IsNullOrWhiteSpace(nameTextBox.Text);

            if (await dialog.ShowAsync() == ContentDialogResult.Primary &&
                ViewModel.SelectedNotebook == notebook)
            {
                ViewModel.SelectedNotebookName = nameTextBox.Text;
                _isRenamingSelection = true;
                SetNavigationEnabled(false);
                try
                {
                    await ViewModel.RenameNotebookCommand.ExecuteAsync(null);
                    NotebookListView.SelectedItem = notebook;
                    ViewModel.SelectedNotebook = notebook;
                }
                finally
                {
                    SetNavigationEnabled(true);
                    _isRenamingSelection = false;
                }
            }
        }
        private async void SectionListView_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_isRenamingSelection || _isSearchNavigationInProgress)
            {
                return;
            }

            var navigationVersion = _selectionNavigationVersion;
            await _selectionNavigationLock.WaitAsync();
            try
            {
                if (navigationVersion != _selectionNavigationVersion ||
                    _isSearchNavigationInProgress)
                {
                    return;
                }

                await ViewModel.LoadPagesCommand.ExecuteAsync(null);
            }
            finally
            {
                _selectionNavigationLock.Release();
            }
        }
        private async void RenameSectionMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection) return;

            var section = ViewModel.SelectedSection;
            if (section is null ||
                section.NotebookId != ViewModel.SelectedNotebook?.Id)
            {
                return;
            }

            var nameTextBox = new TextBox
            {
                Header = "Section name",
                Text = section.Name
            };

            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = "Rename section",
                Content = nameTextBox,
                PrimaryButtonText = "Rename",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            nameTextBox.TextChanged += (_, _) =>
                dialog.IsPrimaryButtonEnabled =
                    !string.IsNullOrWhiteSpace(nameTextBox.Text);

            if (await dialog.ShowAsync() == ContentDialogResult.Primary &&
                ViewModel.SelectedSection == section &&
                ViewModel.SelectedNotebook?.Id == section.NotebookId)
            {
                ViewModel.SelectedSectionName = nameTextBox.Text;
                _isRenamingSelection = true;
                SetNavigationEnabled(false);
                try
                {
                    await ViewModel.RenameSectionCommand.ExecuteAsync(null);
                    SectionListView.SelectedItem = section;
                    ViewModel.SelectedSection = section;
                }
                finally
                {
                    SetNavigationEnabled(true);
                    _isRenamingSelection = false;
                }
            }
        }
        private void PageContentTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                ViewModel.PageContent = textBox.Text;
            }
        }
        private async void PageListView_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_isRenamingSelection || _isSearchNavigationInProgress)
            {
                return;
            }

            if (sender is not ListView listView)
            {
                return;
            }

            var page =
                listView.SelectedItem as Syllanote.Domain.Entities.Page;

            var navigationVersion = _selectionNavigationVersion;
            await _selectionNavigationLock.WaitAsync();
            try
            {
                if (navigationVersion != _selectionNavigationVersion ||
                    _isSearchNavigationInProgress)
                {
                    return;
                }

                if (page is not null)
                {
                    ShowPageEditor();
                }
                UpdatePageState();
                await ViewModel.SelectPageAsync(page);
            }
            finally
            {
                _selectionNavigationLock.Release();
            }
        }

        private async void RenamePageMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection) return;

            var page = ViewModel.SelectedPage;
            if (page is null || !IsSelectedPageCurrent())
            {
                return;
            }

            var titleTextBox = new TextBox
            {
                Header = "Page title",
                Text = page.Title
            };

            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = "Rename page",
                Content = titleTextBox,
                PrimaryButtonText = "Rename",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary
            };

            titleTextBox.TextChanged += (_, _) =>
                dialog.IsPrimaryButtonEnabled =
                    !string.IsNullOrWhiteSpace(titleTextBox.Text);

            if (await dialog.ShowAsync() == ContentDialogResult.Primary &&
                ViewModel.SelectedPage == page &&
                IsSelectedPageCurrent())
            {
                ViewModel.SelectedPageTitle = titleTextBox.Text;
                _isRenamingSelection = true;
                SetNavigationEnabled(false);
                try
                {
                    await ViewModel.RenamePageCommand.ExecuteAsync(null);
                    PagesListView.SelectedItem = page;
                    UpdatePageState();
                }
                finally
                {
                    SetNavigationEnabled(true);
                    _isRenamingSelection = false;
                }
            }
        }

        private async void DeletePageButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection) return;

            var page = ViewModel.SelectedPage;
            if (page is null || !IsSelectedPageCurrent())
            {
                return;
            }

            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = "Delete page?",
                Content = $"Delete \"{page.Title}\"? This cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary &&
                ViewModel.SelectedPage == page &&
                IsSelectedPageCurrent())
            {
                await ViewModel.DeletePageCommand.ExecuteAsync(null);
            }
        }

        private async void DeleteSectionButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection) return;

            var section = ViewModel.SelectedSection;
            if (section is null ||
                section.NotebookId != ViewModel.SelectedNotebook?.Id)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = "Delete section?",
                Content = $"Delete \"{section.Name}\" and all its pages? This cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary &&
                ViewModel.SelectedSection == section &&
                ViewModel.SelectedNotebook?.Id == section.NotebookId)
            {
                await ViewModel.DeleteSectionCommand.ExecuteAsync(null);
            }
        }

        private async void DeleteNotebookButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection) return;

            var notebook = ViewModel.SelectedNotebook;
            if (notebook is null)
            {
                return;
            }

            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = "Delete notebook?",
                Content = $"Delete \"{notebook.Name}\" and all its sections, pages, and concepts? This cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            if (await dialog.ShowAsync() == ContentDialogResult.Primary &&
                ViewModel.SelectedNotebook == notebook)
            {
                await ViewModel.DeleteNotebookCommand.ExecuteAsync(null);
            }
        }
    }
}
