using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syllanote.Desktop.ViewModels;
using Syllanote.Application.Notebooks.Sections.Pages.SearchPages;
using System;
using System.Threading;

namespace Syllanote.Desktop
{
    public sealed partial class MainWindow : Window
    {
        private bool _isRenamingSelection;
        private bool _isSearchNavigationInProgress;
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
                await ViewModel.NavigateToSearchResultAsync(result);
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
                Content = $"Delete \"{notebook.Name}\" and all its sections and pages? This cannot be undone.",
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
