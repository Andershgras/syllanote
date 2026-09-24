using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Syllanote.Desktop.ViewModels;
using Syllanote.Application.Notebooks.Concepts;
using Syllanote.Application.Notebooks.Concepts.FindConceptReferences;
using Syllanote.Application.Notebooks.Concepts.Recognition;
using Syllanote.Application.Notebooks.Sections.Pages.SearchPages;
using Syllanote.Domain.Entities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Syllanote.Desktop
{
    public sealed partial class MainWindow : Window
    {
        private const float NormalFontSizeInPoints = 10.5f;
        private const float Heading1FontSizeInPoints = 18;
        private const float Heading2FontSizeInPoints = 15;

        private bool _isRenamingSelection;
        private bool _isSearchNavigationInProgress;
        private bool _isConceptOperationInProgress;
        private bool _isUpdatingPageEditorContent;
        private bool _isApplyingConceptHighlighting;
        private bool _isSuppressingEditorChanges;
        private bool _isUpdatingFormattingToolbar;
        private bool _isConceptHighlightUpdateQueued;
        private int _editorChangeSuppressionVersion;
        private readonly SemaphoreSlim _selectionNavigationLock = new(1, 1);
        private int _selectionNavigationVersion;
        private Notebook? _notebookActionTarget;
        private Section? _sectionActionTarget;
        private Syllanote.Domain.Entities.Page? _pageActionTarget;

        public NotebookViewModel ViewModel { get; }
        public ObservableCollection<NotebookNavigationItem> NotebookNavigationItems { get; } = [];

        private void SetNavigationEnabled(bool isEnabled)
        {
            NotebookItemsControl.IsEnabled = isEnabled;
            PagesListView.IsEnabled = isEnabled;
        }

        public MainWindow(NotebookViewModel viewModel)
        {
            InitializeComponent();
            Title = "Syllanote";

            ViewModel = viewModel;
            ConceptDefinitionFlyout.OverlayInputPassThroughElement =
                PageContentRichEditBox;
            PageContentRichEditBox.AddHandler(
                UIElement.TappedEvent,
                new TappedEventHandler(PageContentRichEditBox_Tapped),
                true);
            ViewModel.Notebooks.CollectionChanged += (_, _) =>
            {
                RebuildNotebookNavigation();
                UpdateNotebookEmptyState();
            };
            ViewModel.Sections.CollectionChanged += (_, _) => RefreshSectionNavigation();
            ViewModel.Pages.CollectionChanged += (_, _) => UpdatePageState();
            ViewModel.Concepts.CollectionChanged += (_, _) => UpdateConceptEditorState();
            ViewModel.ConceptReferences.CollectionChanged +=
                (_, _) => UpdateConceptEditorState();
            ViewModel.ConceptMatches.CollectionChanged += (_, _) =>
            {
                HideConceptDefinition();
                QueueConceptHighlightRefresh();
            };
            ViewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ViewModel.SelectedNotebook) ||
                    e.PropertyName == nameof(ViewModel.SelectedSection))
                {
                    HideConceptDefinition();
                    RefreshSectionNavigation();
                    UpdatePageState();
                }
                else if (e.PropertyName == nameof(ViewModel.SelectedPage))
                {
                    HideConceptDefinition();
                    UpdatePageState();
                    SyncPageEditorContent(force: true);
                }
                else if (e.PropertyName == nameof(ViewModel.PageContent))
                {
                    HideConceptDefinition();
                    SyncPageEditorContent();
                }
            };
            UpdateNotebookEmptyState();
            RebuildNotebookNavigation();
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
            ConceptReferencesEmptyState.Text = hasCurrentConcept
                ? "This concept is not referenced on any pages."
                : "Select a concept to view references.";
            ConceptReferencesEmptyState.Visibility =
                ViewModel.ConceptReferences.Count == 0
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private void BeginInternalEditorChange()
        {
            _isSuppressingEditorChanges = true;
            _editorChangeSuppressionVersion++;
        }

        private void EndInternalEditorChange()
        {
            var suppressionVersion = _editorChangeSuppressionVersion;
            if (!DispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.Low,
                () =>
                {
                    if (_editorChangeSuppressionVersion == suppressionVersion)
                    {
                        _isSuppressingEditorChanges = false;
                    }
                }))
            {
                _isSuppressingEditorChanges = false;
            }
        }

        private void SetConceptOperationInProgress(bool isInProgress)
        {
            _isConceptOperationInProgress = isInProgress;
            SetNavigationEnabled(!isInProgress);
            SearchButton.IsEnabled = !isInProgress;
            SearchTextBox.IsEnabled = !isInProgress;
            NewConceptButton.IsEnabled = !isInProgress;
            ConceptsListView.IsEnabled = !isInProgress;
            ConceptReferencesListView.IsEnabled = !isInProgress;
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

        private void RebuildNotebookNavigation()
        {
            NotebookNavigationItems.Clear();

            for (var index = 0; index < ViewModel.Notebooks.Count; index++)
            {
                NotebookNavigationItems.Add(
                    NotebookNavigationItem.ForNotebook(
                        ViewModel.Notebooks[index],
                        canMoveUp: index > 0,
                        canMoveDown: index < ViewModel.Notebooks.Count - 1));
            }

            RefreshSectionNavigation();
        }

        private void RefreshSectionNavigation()
        {
            var selectedNotebookId = ViewModel.SelectedNotebook?.Id;

            foreach (var notebookItem in NotebookNavigationItems)
            {
                var isActive = notebookItem.Notebook?.Id == selectedNotebookId;
                var wasActive = notebookItem.IsActiveNotebook;
                notebookItem.IsActiveNotebook = isActive;
                if (!isActive)
                {
                    notebookItem.IsExpanded = false;
                }
                else if (!wasActive)
                {
                    notebookItem.IsExpanded = true;
                }
                notebookItem.Children.Clear();

                if (isActive)
                {
                    var sections = ViewModel.Sections.Where(
                        item => item.NotebookId == selectedNotebookId).ToList();
                    for (var index = 0; index < sections.Count; index++)
                    {
                        var sectionItem =
                            NotebookNavigationItem.ForSection(
                                sections[index],
                                canMoveUp: index > 0,
                                canMoveDown: index < sections.Count - 1);
                        sectionItem.IsSelectedSection =
                            sections[index].Id == ViewModel.SelectedSection?.Id;
                        notebookItem.Children.Add(sectionItem);
                    }
                }

                notebookItem.RefreshEmptySectionsVisibility();
            }

            SyncNavigationSelection();
        }

        private void SyncNavigationSelection()
        {
            var selectedSectionId = ViewModel.SelectedSection?.Id;

            foreach (var item in NotebookNavigationItems.SelectMany(
                item => item.Children))
            {
                item.IsSelectedSection = item.Section?.Id == selectedSectionId;
            }

        }
        private void UpdatePageState()
        {
            var hasSection =
                ViewModel.SelectedSection is not null &&
                ViewModel.SelectedSection.NotebookId == ViewModel.SelectedNotebook?.Id;
            var hasPage = IsSelectedPageCurrent();
            NewPageButton.IsEnabled = hasSection;
            PageEmptyState.Text = hasSection
                ? "No pages yet. Create one to get started."
                : "Select a section to see its pages.";
            PageEmptyState.Visibility = !hasSection || ViewModel.Pages.Count == 0
                ? Visibility.Visible
                : Visibility.Collapsed;
            EditorPageTitle.Visibility = hasPage ? Visibility.Visible : Visibility.Collapsed;
            FormattingToolbar.Visibility = hasPage ? Visibility.Visible : Visibility.Collapsed;
            PageContentRichEditBox.Visibility = hasPage ? Visibility.Visible : Visibility.Collapsed;
            EditorEmptyState.Visibility = hasPage ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SyncPageEditorContent(bool force = false)
        {
            PageContentRichEditBox.Document.GetText(
                TextGetOptions.None,
                out var editorContent);

            if (!force && editorContent == ViewModel.PageContent)
            {
                return;
            }

            _isUpdatingPageEditorContent = true;
            BeginInternalEditorChange();
            try
            {
                if (string.IsNullOrEmpty(ViewModel.PageFormattedContent))
                {
                    PageContentRichEditBox.Document.SetText(
                        TextSetOptions.None,
                        ViewModel.PageContent);
                }
                else
                {
                    PageContentRichEditBox.Document.SetText(
                        TextSetOptions.FormatRtf,
                        ViewModel.PageFormattedContent);
                }
            }
            finally
            {
                _isUpdatingPageEditorContent = false;
                EndInternalEditorChange();
            }

            QueueConceptHighlightRefresh();
            UpdateFormattingToolbarState();
        }

        private void GetPersistedPageEditorContent(
            out string content,
            out string formattedContent)
        {
            PageContentRichEditBox.Document.GetText(
                TextGetOptions.None,
                out content);

            _isApplyingConceptHighlighting = true;
            BeginInternalEditorChange();
            PageContentRichEditBox.Document.BatchDisplayUpdates();
            try
            {
                if (content.Length > 0)
                {
                    var documentRange = PageContentRichEditBox.Document.GetRange(
                        0,
                        content.Length);
                    var documentFormat = documentRange.CharacterFormat;
                    documentFormat.BackgroundColor = Colors.Transparent;
                    documentRange.CharacterFormat = documentFormat;
                }

                PageContentRichEditBox.Document.GetText(
                    TextGetOptions.FormatRtf,
                    out formattedContent);
            }
            finally
            {
                PageContentRichEditBox.Document.ApplyDisplayUpdates();
                _isApplyingConceptHighlighting = false;
                EndInternalEditorChange();
            }

            QueueConceptHighlightRefresh();
        }

        private void UpdatePageContentFromEditor()
        {
            HideConceptDefinition();
            GetPersistedPageEditorContent(
                out var content,
                out var formattedContent);
            ViewModel.PageContent = content;
            ViewModel.PageFormattedContent = formattedContent;
        }

        private void ParagraphStyleComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_isUpdatingFormattingToolbar ||
                !IsSelectedPageCurrent() ||
                ParagraphStyleComboBox.SelectedIndex < 0)
            {
                return;
            }

            var selectedStyle = ParagraphStyleComboBox.SelectedIndex;
            var selection = PageContentRichEditBox.Document.Selection;
            var paragraphRange = selection.GetClone();
            paragraphRange.Expand(TextRangeUnit.Paragraph);

            var paragraphFormat = paragraphRange.ParagraphFormat;
            paragraphFormat.Style = selectedStyle switch
            {
                1 => ParagraphStyle.Heading1,
                2 => ParagraphStyle.Heading2,
                _ => ParagraphStyle.Normal
            };
            paragraphRange.ParagraphFormat = paragraphFormat;
            selection.ParagraphFormat = paragraphFormat;

            var fontSize = selectedStyle switch
            {
                1 => Heading1FontSizeInPoints,
                2 => Heading2FontSizeInPoints,
                _ => NormalFontSizeInPoints
            };
            var bold = selectedStyle == 0
                ? FormatEffect.Off
                : FormatEffect.On;
            var characterFormat = paragraphRange.CharacterFormat;
            characterFormat.Size = fontSize;
            characterFormat.Bold = bold;
            paragraphRange.CharacterFormat = characterFormat;

            var insertionFormat = selection.CharacterFormat;
            insertionFormat.Size = fontSize;
            insertionFormat.Bold = bold;
            selection.CharacterFormat = insertionFormat;

            UpdatePageContentFromEditor();
            UpdateFormattingToolbarState();
            PageContentRichEditBox.Focus(FocusState.Programmatic);
        }

        private void BoldButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingFormattingToolbar)
            {
                return;
            }

            var selection = PageContentRichEditBox.Document.Selection;
            var characterFormat = selection.CharacterFormat;
            characterFormat.Bold = BoldButton.IsChecked == true
                ? FormatEffect.On
                : FormatEffect.Off;
            selection.CharacterFormat = characterFormat;

            UpdatePageContentFromEditor();
            PageContentRichEditBox.Focus(FocusState.Programmatic);
        }

        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingFormattingToolbar)
            {
                return;
            }

            var selection = PageContentRichEditBox.Document.Selection;
            var characterFormat = selection.CharacterFormat;
            characterFormat.Italic = ItalicButton.IsChecked == true
                ? FormatEffect.On
                : FormatEffect.Off;
            selection.CharacterFormat = characterFormat;

            UpdatePageContentFromEditor();
            PageContentRichEditBox.Focus(FocusState.Programmatic);
        }

        private void UnderlineButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingFormattingToolbar)
            {
                return;
            }

            var selection = PageContentRichEditBox.Document.Selection;
            var characterFormat = selection.CharacterFormat;
            characterFormat.Underline = UnderlineButton.IsChecked == true
                ? UnderlineType.Single
                : UnderlineType.None;
            selection.CharacterFormat = characterFormat;

            UpdatePageContentFromEditor();
            PageContentRichEditBox.Focus(FocusState.Programmatic);
        }

        private void BulletedListButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingFormattingToolbar)
            {
                return;
            }

            ApplyListFormatting(
                MarkerType.Bullet,
                BulletedListButton.IsChecked == true);
        }

        private void NumberedListButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingFormattingToolbar)
            {
                return;
            }

            ApplyListFormatting(
                MarkerType.Arabic,
                NumberedListButton.IsChecked == true);
        }

        private void ApplyListFormatting(MarkerType listType, bool isEnabled)
        {
            var selection = PageContentRichEditBox.Document.Selection;
            var paragraphRange = selection.GetClone();
            paragraphRange.Expand(TextRangeUnit.Paragraph);

            var paragraphFormat = paragraphRange.ParagraphFormat;
            if (isEnabled)
            {
                paragraphFormat.ListType = listType;
                paragraphFormat.ListLevelIndex = 1;
                if (listType == MarkerType.Arabic)
                {
                    paragraphFormat.ListStart = 1;
                }
            }
            else
            {
                paragraphFormat.ListType = MarkerType.None;
                paragraphFormat.ListLevelIndex = 0;
                paragraphFormat.SetIndents(0, 0, 0);
                paragraphFormat.ClearAllTabs();
            }

            paragraphRange.ParagraphFormat = paragraphFormat;
            selection.ParagraphFormat = paragraphFormat;

            UpdatePageContentFromEditor();
            UpdateFormattingToolbarState();
            PageContentRichEditBox.Focus(FocusState.Programmatic);
        }

        private void UpdateFormattingToolbarState()
        {
            if (!IsSelectedPageCurrent())
            {
                return;
            }

            var characterFormat =
                PageContentRichEditBox.Document.Selection.CharacterFormat;
            var paragraphStyle =
                PageContentRichEditBox.Document.Selection.ParagraphFormat.Style;
            var listType =
                PageContentRichEditBox.Document.Selection.ParagraphFormat.ListType;

            _isUpdatingFormattingToolbar = true;
            try
            {
                ParagraphStyleComboBox.SelectedIndex = paragraphStyle switch
                {
                    ParagraphStyle.Heading1 => 1,
                    ParagraphStyle.Heading2 => 2,
                    ParagraphStyle.Normal or ParagraphStyle.None => 0,
                    _ => -1
                };
                BoldButton.IsChecked = characterFormat.Bold == FormatEffect.On;
                ItalicButton.IsChecked = characterFormat.Italic == FormatEffect.On;
                UnderlineButton.IsChecked =
                    characterFormat.Underline != UnderlineType.None &&
                    characterFormat.Underline != UnderlineType.Undefined;
                BulletedListButton.IsChecked = listType == MarkerType.Bullet;
                NumberedListButton.IsChecked = listType == MarkerType.Arabic;
            }
            finally
            {
                _isUpdatingFormattingToolbar = false;
            }
        }

        private void QueueConceptHighlightRefresh()
        {
            if (_isConceptHighlightUpdateQueued)
            {
                return;
            }

            _isConceptHighlightUpdateQueued = true;
            if (!DispatcherQueue.TryEnqueue(() =>
            {
                _isConceptHighlightUpdateQueued = false;
                ApplyConceptHighlights();
            }))
            {
                _isConceptHighlightUpdateQueued = false;
            }
        }

        private void ApplyConceptHighlights()
        {
            PageContentRichEditBox.Document.GetText(
                TextGetOptions.None,
                out var editorContent);

            var selection = PageContentRichEditBox.Document.Selection;
            var selectionStart = selection.StartPosition;
            var selectionEnd = selection.EndPosition;

            _isApplyingConceptHighlighting = true;
            BeginInternalEditorChange();
            PageContentRichEditBox.Document.BatchDisplayUpdates();
            try
            {
                if (editorContent.Length > 0)
                {
                    var documentRange = PageContentRichEditBox.Document.GetRange(
                        0,
                        editorContent.Length);
                    var documentFormat = documentRange.CharacterFormat;
                    documentFormat.BackgroundColor = Colors.Transparent;
                    documentRange.CharacterFormat = documentFormat;
                }

                foreach (var match in ViewModel.ConceptMatches)
                {
                    if (!IsCurrentConceptMatch(match, editorContent))
                    {
                        continue;
                    }

                    var endIndex = match.StartIndex + match.Length;
                    var conceptRange = PageContentRichEditBox.Document.GetRange(
                        match.StartIndex,
                        endIndex);
                    var conceptFormat = conceptRange.CharacterFormat;
                    conceptFormat.BackgroundColor = ColorHelper.FromArgb(
                        96,
                        0,
                        120,
                        212);
                    conceptRange.CharacterFormat = conceptFormat;
                }
            }
            finally
            {
                PageContentRichEditBox.Document.ApplyDisplayUpdates();
                _isApplyingConceptHighlighting = false;
                EndInternalEditorChange();
            }

            if (selection.StartPosition != selectionStart ||
                selection.EndPosition != selectionEnd)
            {
                selection.SetRange(selectionStart, selectionEnd);
            }
        }

        private static bool IsCurrentConceptMatch(
            ConceptMatch match,
            string editorContent)
        {
            return match.StartIndex >= 0 &&
                match.Length > 0 &&
                match.StartIndex <= editorContent.Length &&
                match.Length <= editorContent.Length - match.StartIndex &&
                string.Equals(
                    editorContent.Substring(match.StartIndex, match.Length),
                    match.ConceptName,
                    StringComparison.OrdinalIgnoreCase);
        }

        private void PageContentRichEditBox_Tapped(
            object sender,
            TappedRoutedEventArgs e)
        {
            var selection = PageContentRichEditBox.Document.Selection;
            if (ViewModel.SelectedPage is null ||
                selection.StartPosition != selection.EndPosition)
            {
                HideConceptDefinition();
                return;
            }

            PageContentRichEditBox.Document.GetText(
                TextGetOptions.None,
                out var editorContent);
            var caretPosition = selection.StartPosition;
            var match = ViewModel.ConceptMatches.FirstOrDefault(candidate =>
                IsCurrentConceptMatch(candidate, editorContent) &&
                caretPosition >= candidate.StartIndex &&
                caretPosition < candidate.StartIndex + candidate.Length);
            var concept = match is null
                ? null
                : ViewModel.Concepts.FirstOrDefault(candidate =>
                    candidate.Id == match.ConceptId &&
                    candidate.NotebookId == ViewModel.SelectedNotebook?.Id);

            if (concept is null)
            {
                HideConceptDefinition();
                return;
            }

            HideConceptDefinition();
            ConceptDefinitionNameTextBlock.Text = concept.Name;
            ConceptDefinitionTextBlock.Text = concept.Definition;
            ConceptDefinitionFlyout.ShowAt(
                PageContentRichEditBox,
                new FlyoutShowOptions
                {
                    Placement = FlyoutPlacementMode.Bottom,
                    Position = e.GetPosition(PageContentRichEditBox),
                    ShowMode = FlyoutShowMode.Transient
                });
        }

        private void HideConceptDefinition()
        {
            if (ConceptDefinitionFlyout.IsOpen)
            {
                ConceptDefinitionFlyout.Hide();
            }
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
                _notebookActionTarget is not Notebook notebook ||
                !await EnsureNotebookSelectedAsync(notebook) ||
                ViewModel.SelectedNotebook is not Notebook selectedNotebook)
            {
                return;
            }

            notebook = selectedNotebook;

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

        private async void ConceptsListView_SelectionChanged(
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
            await ViewModel.LoadConceptReferencesAsync(
                ConceptsListView.SelectedItem as Concept);
            UpdateConceptEditorState();
        }

        private async void ConceptReferencesListView_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (_isConceptOperationInProgress ||
                ConceptReferencesListView.SelectedItem
                    is not ConceptReference reference)
            {
                return;
            }

            SetConceptOperationInProgress(true);
            _selectionNavigationVersion++;
            await _selectionNavigationLock.WaitAsync();
            try
            {
                if (await ViewModel.NavigateToConceptReferenceAsync(reference))
                {
                    ShowPageEditor();
                    RefreshSectionNavigation();
                    PagesListView.SelectedItem = ViewModel.SelectedPage;
                    UpdatePageState();
                }
                else
                {
                    ConceptMessage.Text =
                        "This page is no longer available.";
                }

                ConceptReferencesListView.SelectedItem = null;
            }
            finally
            {
                _selectionNavigationLock.Release();
                SetConceptOperationInProgress(false);
            }
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
                RefreshSectionNavigation();
                PagesListView.SelectedItem = ViewModel.SelectedPage;
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

            var notebook = (sender as FrameworkElement)?.DataContext
                is NotebookNavigationItem navigationItem
                    ? navigationItem.Notebook
                    : ViewModel.SelectedNotebook;
            if (notebook is null || !await EnsureNotebookSelectedAsync(notebook))
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
        private async void NotebookNavigationButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection || _isSearchNavigationInProgress ||
                (sender as FrameworkElement)?.DataContext
                    is not NotebookNavigationItem navigationItem ||
                navigationItem.Notebook is not Notebook notebook)
            {
                return;
            }

            var navigationVersion = ++_selectionNavigationVersion;
            navigationItem.IsExpanded = true;
            ViewModel.SelectedNotebook = notebook;
            ShowPageEditor();
            await _selectionNavigationLock.WaitAsync();
            try
            {
                if (navigationVersion != _selectionNavigationVersion ||
                    _isSearchNavigationInProgress)
                {
                    return;
                }

                await ViewModel.LoadSectionsCommand.ExecuteAsync(null);
                if (navigationVersion == _selectionNavigationVersion &&
                    ViewModel.SelectedNotebook?.Id == notebook.Id)
                {
                    RefreshSectionNavigation();
                }
            }
            finally
            {
                _selectionNavigationLock.Release();
            }
        }

        private async void SectionNavigationButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection || _isSearchNavigationInProgress ||
                (sender as FrameworkElement)?.DataContext
                    is not NotebookNavigationItem navigationItem ||
                navigationItem.Section is not Section section)
            {
                return;
            }

            var currentSection = ViewModel.Sections.FirstOrDefault(
                item => item.Id == section.Id);
            if (currentSection is null ||
                currentSection.NotebookId != ViewModel.SelectedNotebook?.Id)
            {
                return;
            }

            var navigationVersion = ++_selectionNavigationVersion;
            ViewModel.SelectedSection = currentSection;
            ShowPageEditor();
            await _selectionNavigationLock.WaitAsync();
            try
            {
                if (navigationVersion != _selectionNavigationVersion ||
                    _isSearchNavigationInProgress)
                {
                    return;
                }

                await ViewModel.LoadPagesCommand.ExecuteAsync(null);
                if (navigationVersion == _selectionNavigationVersion &&
                    ViewModel.SelectedSection?.Id == currentSection.Id)
                {
                    SyncNavigationSelection();
                }
            }
            finally
            {
                _selectionNavigationLock.Release();
            }
        }

        private async void NotebookExpandCollapseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection || _isSearchNavigationInProgress ||
                (sender as FrameworkElement)?.DataContext
                    is not NotebookNavigationItem navigationItem ||
                navigationItem.Notebook is not Notebook notebook)
            {
                return;
            }

            var shouldExpand = !navigationItem.IsActiveNotebook ||
                !navigationItem.IsExpanded;
            if (!await EnsureNotebookSelectedAsync(notebook))
            {
                return;
            }

            var currentItem = NotebookNavigationItems.FirstOrDefault(
                item => item.Notebook?.Id == notebook.Id);
            if (currentItem is not null)
            {
                currentItem.IsExpanded = shouldExpand;
                currentItem.RefreshEmptySectionsVisibility();
            }
        }

        private void NotebookContextMenu_Opening(
            object sender,
            object e)
        {
            _notebookActionTarget = (sender as FlyoutBase)?.Target?.DataContext
                is NotebookNavigationItem navigationItem
                    ? navigationItem.Notebook
                    : null;
        }

        private void SectionContextMenu_Opening(
            object sender,
            object e)
        {
            _sectionActionTarget = (sender as FlyoutBase)?.Target?.DataContext
                is NotebookNavigationItem navigationItem
                    ? navigationItem.Section
                    : null;
        }

        private void PageContextMenu_Opening(
            object sender,
            object e)
        {
            var flyout = sender as MenuFlyout;
            _pageActionTarget = flyout?.Target?.DataContext
                as Syllanote.Domain.Entities.Page;

            var index = _pageActionTarget is null
                ? -1
                : ViewModel.Pages.IndexOf(_pageActionTarget);
            if (flyout?.Items.Count >= 2)
            {
                flyout.Items[0].IsEnabled = index > 0;
                flyout.Items[1].IsEnabled =
                    index >= 0 && index < ViewModel.Pages.Count - 1;
            }
        }

        private async Task<bool> EnsureNotebookSelectedAsync(Notebook notebook)
        {
            await _selectionNavigationLock.WaitAsync();
            try
            {
                var currentNotebook = ViewModel.Notebooks.FirstOrDefault(
                    item => item.Id == notebook.Id);
                if (currentNotebook is null)
                {
                    return false;
                }

                if (ViewModel.SelectedNotebook?.Id != currentNotebook.Id)
                {
                    ViewModel.SelectedNotebook = currentNotebook;
                    await ViewModel.LoadSectionsCommand.ExecuteAsync(null);
                }

                ShowPageEditor();
                RefreshSectionNavigation();
                return ViewModel.SelectedNotebook?.Id == currentNotebook.Id;
            }
            finally
            {
                _selectionNavigationLock.Release();
            }
        }

        private async Task<Section?> EnsureSectionSelectedAsync(Section section)
        {
            var notebook = ViewModel.Notebooks.FirstOrDefault(
                item => item.Id == section.NotebookId);
            if (notebook is null || !await EnsureNotebookSelectedAsync(notebook))
            {
                return null;
            }

            await _selectionNavigationLock.WaitAsync();
            try
            {
                var currentSection = ViewModel.Sections.FirstOrDefault(
                    item => item.Id == section.Id);
                if (currentSection is null)
                {
                    return null;
                }

                if (ViewModel.SelectedSection?.Id != currentSection.Id)
                {
                    ViewModel.SelectedSection = currentSection;
                    await ViewModel.LoadPagesCommand.ExecuteAsync(null);
                }

                SyncNavigationSelection();
                return currentSection;
            }
            finally
            {
                _selectionNavigationLock.Release();
            }
        }

        private async Task<Syllanote.Domain.Entities.Page?>
            EnsurePageSelectedAsync(Syllanote.Domain.Entities.Page page)
        {
            await _selectionNavigationLock.WaitAsync();
            try
            {
                var currentPage = ViewModel.Pages.FirstOrDefault(
                    item => item.Id == page.Id);
                if (currentPage is null ||
                    currentPage.SectionId != ViewModel.SelectedSection?.Id ||
                    ViewModel.SelectedSection?.NotebookId !=
                        ViewModel.SelectedNotebook?.Id)
                {
                    return null;
                }

                if (ViewModel.SelectedPage != currentPage)
                {
                    await ViewModel.SelectPageAsync(currentPage);
                }

                PagesListView.SelectedItem = currentPage;
                ShowPageEditor();
                UpdatePageState();
                return currentPage;
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

            var notebook = _notebookActionTarget;
            if (notebook is null || !await EnsureNotebookSelectedAsync(notebook))
            {
                return;
            }

            notebook = ViewModel.SelectedNotebook;
            if (notebook is null) return;

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
                    ViewModel.SelectedNotebook = notebook;
                    RebuildNotebookNavigation();
                }
                finally
                {
                    SetNavigationEnabled(true);
                    _isRenamingSelection = false;
                }
            }
        }

        private async void MoveNotebookUpMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            await MoveSelectedNotebookAsync(moveUp: true);
        }

        private async void MoveNotebookDownMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            await MoveSelectedNotebookAsync(moveUp: false);
        }

        private async Task MoveSelectedNotebookAsync(
            bool moveUp)
        {
            var notebook = _notebookActionTarget;
            if (notebook is null || !await EnsureNotebookSelectedAsync(notebook))
            {
                return;
            }

            SetNavigationEnabled(false);
            try
            {
                if (moveUp)
                {
                    await ViewModel.MoveSelectedNotebookUpCommand.ExecuteAsync(null);
                }
                else
                {
                    await ViewModel.MoveSelectedNotebookDownCommand.ExecuteAsync(null);
                }
                RebuildNotebookNavigation();
            }
            finally
            {
                SetNavigationEnabled(true);
            }
        }

        private async void RenameSectionMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection) return;

            var section = _sectionActionTarget;
            if (section is null ||
                await EnsureSectionSelectedAsync(section) is not Section currentSection)
            {
                return;
            }

            section = currentSection;

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
                    ViewModel.SelectedSection = section;
                    RefreshSectionNavigation();
                }
                finally
                {
                    SetNavigationEnabled(true);
                    _isRenamingSelection = false;
                }
            }
        }

        private async void MoveSectionUpMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            await MoveSelectedSectionAsync(moveUp: true);
        }

        private async void MoveSectionDownMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            await MoveSelectedSectionAsync(moveUp: false);
        }

        private async Task MoveSelectedSectionAsync(bool moveUp)
        {
            var section = _sectionActionTarget;
            if (section is null ||
                await EnsureSectionSelectedAsync(section) is not Section currentSection)
            {
                return;
            }

            SetNavigationEnabled(false);
            try
            {
                ViewModel.SelectedSection = currentSection;
                if (moveUp)
                {
                    await ViewModel.MoveSelectedSectionUpCommand.ExecuteAsync(null);
                }
                else
                {
                    await ViewModel.MoveSelectedSectionDownCommand.ExecuteAsync(null);
                }
                RefreshSectionNavigation();
            }
            finally
            {
                SetNavigationEnabled(true);
            }
        }
        private void PageContentRichEditBox_TextChanged(
            object sender,
            RoutedEventArgs e)
        {
            if (_isUpdatingPageEditorContent ||
                _isApplyingConceptHighlighting ||
                _isSuppressingEditorChanges ||
                sender is not RichEditBox richEditBox)
            {
                return;
            }

            UpdatePageContentFromEditor();
            UpdateFormattingToolbarState();
        }

        private void PageContentRichEditBox_SelectionChanged(
            object sender,
            RoutedEventArgs e)
        {
            UpdateFormattingToolbarState();
        }
        private async void PageListView_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_isRenamingSelection || _isSearchNavigationInProgress ||
                _isConceptOperationInProgress)
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

        private async void MovePageUpMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            await MoveSelectedPageAsync(moveUp: true);
        }

        private async void MovePageDownMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            await MoveSelectedPageAsync(moveUp: false);
        }

        private async Task MoveSelectedPageAsync(bool moveUp)
        {
            if (_isRenamingSelection)
            {
                return;
            }

            var page = _pageActionTarget;
            if (page is null ||
                await EnsurePageSelectedAsync(page) is not
                    Syllanote.Domain.Entities.Page currentPage)
            {
                return;
            }

            SetNavigationEnabled(false);
            try
            {
                await ViewModel.SaveCurrentPageAsync();
                if (moveUp)
                {
                    await ViewModel.MoveSelectedPageUpCommand.ExecuteAsync(null);
                }
                else
                {
                    await ViewModel.MoveSelectedPageDownCommand.ExecuteAsync(null);
                }
                PagesListView.SelectedItem = currentPage;
                UpdatePageState();
            }
            finally
            {
                SetNavigationEnabled(true);
            }
        }

        private async void RenamePageMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection) return;

            var page = _pageActionTarget;
            if (page is null ||
                await EnsurePageSelectedAsync(page) is not
                    Syllanote.Domain.Entities.Page currentPage)
            {
                return;
            }

            page = currentPage;

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

        private async void DeletePageMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection) return;

            var page = _pageActionTarget;
            if (page is null ||
                await EnsurePageSelectedAsync(page) is not
                    Syllanote.Domain.Entities.Page currentPage)
            {
                return;
            }

            page = currentPage;

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

            var section = _sectionActionTarget;
            if (section is null ||
                await EnsureSectionSelectedAsync(section) is not Section currentSection)
            {
                return;
            }

            section = currentSection;

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
                ViewModel.SelectedSection?.Id == section.Id &&
                ViewModel.SelectedNotebook?.Id == section.NotebookId)
            {
                await ViewModel.DeleteSectionCommand.ExecuteAsync(null);
                RefreshSectionNavigation();
            }
        }

        private async void DeleteNotebookButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection) return;

            var notebook = _notebookActionTarget;
            if (notebook is null || !await EnsureNotebookSelectedAsync(notebook))
            {
                return;
            }

            notebook = ViewModel.SelectedNotebook;
            if (notebook is null) return;

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
                ViewModel.SelectedNotebook?.Id == notebook.Id)
            {
                await ViewModel.DeleteNotebookCommand.ExecuteAsync(null);
                RebuildNotebookNavigation();
            }
        }
    }
}
