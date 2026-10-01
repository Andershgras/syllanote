using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.System;
using Microsoft.UI.Text;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Syllanote.Application.Backups;
using Syllanote.Desktop.ViewModels;
using Syllanote.Application.Notebooks.Concepts;
using Syllanote.Application.Notebooks.Concepts.FindConceptReferences;
using Syllanote.Application.Notebooks.Concepts.Recognition;
using Syllanote.Application.Notebooks.Sections.Pages.Formatting;
using Syllanote.Application.Notebooks.Sections.Pages.SearchPages;
using Syllanote.Domain.Entities;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Syllanote.Desktop
{
    public sealed partial class MainWindow : Window
    {
        private const float NormalFontSizeInPoints = 10.5f;
        private const float Heading1FontSizeInPoints = 18;
        private const float Heading2FontSizeInPoints = 15;
        private const string NotebookColumnWidthSettingKey =
            "Workspace.NotebookColumnWidth";
        private const string PageColumnWidthSettingKey =
            "Workspace.PageColumnWidth";
        private const string WindowWidthSettingKey = "Window.Width";
        private const string WindowHeightSettingKey = "Window.Height";
        private const string WindowXSettingKey = "Window.X";
        private const string WindowYSettingKey = "Window.Y";
        private const string WindowMaximizedSettingKey = "Window.Maximized";
        private const string DatabaseRetryMessage =
            "Check that the local database is available, then try again.";
        private const int MinimumWindowWidth = 900;
        private const int MinimumWindowHeight = 600;

        private bool _isRenamingSelection;
        private bool _isSearchNavigationInProgress;
        private bool _isConceptOperationInProgress;
        private bool _isUpdatingPageEditorContent;
        private bool _isApplyingConceptHighlighting;
        private bool _isSuppressingEditorChanges;
        private bool _isUpdatingFormattingToolbar;
        private bool _isConceptHighlightUpdateQueued;
        private bool _allowWindowClose;
        private bool _isWindowCloseInProgress;
        private bool _isLibraryOperationInProgress;
        private bool _shouldRestoreMaximizedState;
        private bool _hasLastRestoredWindowBounds;
        private RectInt32 _lastRestoredWindowBounds;
        private int _editorChangeSuppressionVersion;
        private readonly SemaphoreSlim _selectionNavigationLock = new(1, 1);
        private int _selectionNavigationVersion;
        private Notebook? _notebookActionTarget;
        private Section? _sectionActionTarget;
        private Syllanote.Domain.Entities.Page? _pageActionTarget;
        private readonly ILibraryBackupService _libraryBackupService;
        private readonly ILibraryBackupValidator _libraryBackupValidator;
        private readonly ILibraryRestoreService _libraryRestoreService;
        private readonly ThemeSettings _themeSettings;

        public NotebookViewModel ViewModel { get; }
        public ObservableCollection<NotebookNavigationItem> NotebookNavigationItems { get; } = [];

        private void NotebookColumnSplitter_DragDelta(
            object sender,
            DragDeltaEventArgs e)
        {
            NotebookColumn.Width = new GridLength(
                Math.Clamp(
                    NotebookColumn.ActualWidth + e.HorizontalChange,
                    NotebookColumn.MinWidth,
                    NotebookColumn.MaxWidth));
        }

        private void PageColumnSplitter_DragDelta(
            object sender,
            DragDeltaEventArgs e)
        {
            PageColumn.Width = new GridLength(
                Math.Clamp(
                    PageColumn.ActualWidth + e.HorizontalChange,
                    PageColumn.MinWidth,
                    PageColumn.MaxWidth));
        }

        private void NotebookColumnSplitter_DragCompleted(
            object sender,
            DragCompletedEventArgs e) =>
            SavePanelWidth(
                NotebookColumnWidthSettingKey,
                NotebookColumn.ActualWidth);

        private void PageColumnSplitter_DragCompleted(
            object sender,
            DragCompletedEventArgs e) =>
            SavePanelWidth(
                PageColumnWidthSettingKey,
                PageColumn.ActualWidth);

        private void RestorePanelWidths()
        {
            NotebookColumn.Width = new GridLength(
                GetStoredPanelWidth(
                    NotebookColumnWidthSettingKey,
                    NotebookColumn.Width.Value,
                    NotebookColumn.MinWidth,
                    NotebookColumn.MaxWidth));
            PageColumn.Width = new GridLength(
                GetStoredPanelWidth(
                    PageColumnWidthSettingKey,
                    PageColumn.Width.Value,
                    PageColumn.MinWidth,
                    PageColumn.MaxWidth));
        }

        private static double GetStoredPanelWidth(
            string settingKey,
            double defaultWidth,
            double minimumWidth,
            double maximumWidth)
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            return settings.TryGetValue(settingKey, out var storedValue) &&
                storedValue is double storedWidth
                    ? Math.Clamp(storedWidth, minimumWidth, maximumWidth)
                    : defaultWidth;
        }

        private static void SavePanelWidth(string settingKey, double width)
        {
            ApplicationData.Current.LocalSettings.Values[settingKey] = width;
        }

        private void RestoreWindowPlacement()
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            _shouldRestoreMaximizedState =
                settings.TryGetValue(
                    WindowMaximizedSettingKey,
                    out var storedMaximized) &&
                storedMaximized is true;

            if (!settings.TryGetValue(WindowWidthSettingKey, out var storedWidth) ||
                storedWidth is not int width ||
                !settings.TryGetValue(WindowHeightSettingKey, out var storedHeight) ||
                storedHeight is not int height)
            {
                CaptureCurrentWindowBounds();
                return;
            }

            var x = AppWindow.Position.X;
            var y = AppWindow.Position.Y;
            var hasStoredPosition = false;
            if (settings.TryGetValue(WindowXSettingKey, out var storedX) &&
                storedX is int storedPositionX &&
                settings.TryGetValue(WindowYSettingKey, out var storedY) &&
                storedY is int storedPositionY)
            {
                x = storedPositionX;
                y = storedPositionY;
                hasStoredPosition = true;
            }
            var requestedBounds = new RectInt32(
                x,
                y,
                width,
                height);
            var displayArea = hasStoredPosition
                ? DisplayArea.GetFromRect(
                    requestedBounds,
                    DisplayAreaFallback.Nearest)
                : DisplayArea.GetFromWindowId(
                    AppWindow.Id,
                    DisplayAreaFallback.Primary);
            var safeBounds = ClampWindowBounds(
                requestedBounds,
                displayArea.WorkArea);

            if (hasStoredPosition)
            {
                AppWindow.MoveAndResize(safeBounds);
            }
            else
            {
                AppWindow.Resize(new SizeInt32(
                    safeBounds.Width,
                    safeBounds.Height));
            }

            CaptureCurrentWindowBounds();
        }

        private static RectInt32 ClampWindowBounds(
            RectInt32 requestedBounds,
            RectInt32 workArea)
        {
            var minimumWidth = Math.Min(MinimumWindowWidth, workArea.Width);
            var minimumHeight = Math.Min(MinimumWindowHeight, workArea.Height);
            var width = Math.Clamp(
                requestedBounds.Width,
                minimumWidth,
                workArea.Width);
            var height = Math.Clamp(
                requestedBounds.Height,
                minimumHeight,
                workArea.Height);
            var x = Math.Clamp(
                requestedBounds.X,
                workArea.X,
                workArea.X + workArea.Width - width);
            var y = Math.Clamp(
                requestedBounds.Y,
                workArea.Y,
                workArea.Y + workArea.Height - height);

            return new RectInt32(x, y, width, height);
        }

        private void AppWindow_Changed(
            AppWindow sender,
            AppWindowChangedEventArgs args)
        {
            if ((args.DidPositionChange || args.DidSizeChange) &&
                sender.Presenter is OverlappedPresenter
                {
                    State: OverlappedPresenterState.Restored
                })
            {
                CaptureCurrentWindowBounds();
            }
        }

        private void CaptureCurrentWindowBounds()
        {
            _lastRestoredWindowBounds = new RectInt32(
                AppWindow.Position.X,
                AppWindow.Position.Y,
                AppWindow.Size.Width,
                AppWindow.Size.Height);
            _hasLastRestoredWindowBounds = true;
        }

        private async void MainWindow_Closing(
            AppWindow sender,
            AppWindowClosingEventArgs args)
        {
            var settings = ApplicationData.Current.LocalSettings.Values;
            settings[WindowMaximizedSettingKey] =
                sender.Presenter is OverlappedPresenter
                {
                    State: OverlappedPresenterState.Maximized
                };

            if (_hasLastRestoredWindowBounds)
            {
                settings[WindowXSettingKey] = _lastRestoredWindowBounds.X;
                settings[WindowYSettingKey] = _lastRestoredWindowBounds.Y;
                settings[WindowWidthSettingKey] =
                    _lastRestoredWindowBounds.Width;
                settings[WindowHeightSettingKey] =
                    _lastRestoredWindowBounds.Height;
            }

            if (_allowWindowClose)
            {
                return;
            }

            args.Cancel = true;
            if (_isLibraryOperationInProgress)
            {
                ShowOperationMessage(
                    "Library operation in progress",
                    "Wait for the backup or restore preparation to finish " +
                    "before closing Syllanote.",
                    InfoBarSeverity.Warning);
                return;
            }

            if (_isWindowCloseInProgress)
            {
                return;
            }

            _isWindowCloseInProgress = true;
            try
            {
                if (await ViewModel.SaveCurrentPageAsync())
                {
                    _allowWindowClose = true;
                    Close();
                    return;
                }

                var dialog = new ContentDialog
                {
                    XamlRoot = RootGrid.XamlRoot,
                    Title = "Changes couldn't be saved",
                    Content =
                        "Your latest changes are still in the editor. You can " +
                        "try saving again, keep editing, or close Syllanote " +
                        "without saving those changes.",
                    PrimaryButtonText = "Try again",
                    SecondaryButtonText = "Close without saving",
                    CloseButtonText = "Keep editing",
                    DefaultButton = ContentDialogButton.Close
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    if (await ViewModel.SaveCurrentPageAsync())
                    {
                        _allowWindowClose = true;
                        Close();
                    }
                }
                else if (result == ContentDialogResult.Secondary)
                {
                    _allowWindowClose = true;
                    Close();
                }
            }
            finally
            {
                _isWindowCloseInProgress = false;
            }
        }

        private void SetNavigationEnabled(bool isEnabled)
        {
            NotebookSidebar.SetNavigationEnabled(isEnabled);
            PageSidebar.SetPageListEnabled(isEnabled);
            TopBar.SetLibraryActionsEnabled(isEnabled);
        }

        private void ShowOperationError(string title, string message)
        {
            ShowOperationMessage(title, message, InfoBarSeverity.Error);
        }

        private void ShowOperationSuccess(string title, string message)
        {
            ShowOperationMessage(title, message, InfoBarSeverity.Success);
        }

        private void ShowOperationMessage(
            string title,
            string message,
            InfoBarSeverity severity)
        {
            OperationInfoBar.Title = title;
            OperationInfoBar.Message = message;
            OperationInfoBar.Severity = severity;
            AutomationProperties.SetLiveSetting(
                OperationInfoBar,
                severity is InfoBarSeverity.Error or InfoBarSeverity.Warning
                    ? AutomationLiveSetting.Assertive
                    : AutomationLiveSetting.Polite);
            OperationInfoBar.IsOpen = true;
        }

        private void QueueKeyboardFocus(Func<bool> focusAction)
        {
            DispatcherQueue.TryEnqueue(
                DispatcherQueuePriority.Low,
                () => focusAction());
        }

        private bool FocusPageWorkspace()
        {
            if (IsSelectedPageCurrent())
            {
                return EditorView.ContentEditor.Focus(FocusState.Keyboard);
            }

            return ViewModel.SelectedSection is not null
                ? PageSidebar.FocusNewPageButton()
                : NotebookSidebar.FocusNotebook(ViewModel.SelectedNotebook);
        }

        private static void FocusDialogTextBox(
            ContentDialog dialog,
            TextBox textBox,
            bool selectAll = false)
        {
            dialog.Opened += (_, _) =>
            {
                textBox.Focus(FocusState.Keyboard);
                if (selectAll)
                {
                    textBox.SelectAll();
                }
            };
        }

        private void ClearOperationMessage()
        {
            OperationInfoBar.IsOpen = false;
        }

        private async Task<bool> RunOperationAsync(
            Func<Task> operation,
            string errorTitle,
            string errorMessage = DatabaseRetryMessage)
        {
            ClearOperationMessage();
            try
            {
                await operation();
                return true;
            }
            catch (Exception)
            {
                ShowOperationError(errorTitle, errorMessage);
                return false;
            }
        }

        private async Task<(bool Succeeded, T Result)> RunOperationAsync<T>(
            Func<Task<T>> operation,
            string errorTitle,
            string errorMessage = DatabaseRetryMessage)
        {
            ClearOperationMessage();
            try
            {
                return (true, await operation());
            }
            catch (Exception)
            {
                ShowOperationError(errorTitle, errorMessage);
                return (false, default!);
            }
        }

        public MainWindow(
            NotebookViewModel viewModel,
            ILibraryBackupService libraryBackupService,
            ILibraryBackupValidator libraryBackupValidator,
            ILibraryRestoreService libraryRestoreService)
        {
            _libraryBackupService = libraryBackupService;
            _libraryBackupValidator = libraryBackupValidator;
            _libraryRestoreService = libraryRestoreService;
            InitializeComponent();
            _themeSettings = ThemeSettings.CreateForWindowId(AppWindow.Id);
            _themeSettings.Changed += ThemeSettings_Changed;
            RestorePanelWidths();
            RestoreWindowPlacement();
            AppWindow.Changed += AppWindow_Changed;
            AppWindow.Closing += MainWindow_Closing;
            Title = "Syllanote";
            SystemBackdrop = new MicaBackdrop();

            ViewModel = viewModel;
            NotebookSidebar.SetItemsSource(NotebookNavigationItems);
            EditorView.ContentEditor.AddHandler(
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
            ConceptDictionaryView.Visibility = Visibility.Collapsed;
            EditorView.Visibility = Visibility.Visible;
        }

        private void UpdateConceptEditorState()
        {
            var selected = ConceptDictionaryView.SelectedConcept;
            var hasCurrentConcept = selected is not null &&
                selected.NotebookId == ViewModel.SelectedNotebook?.Id;
            ConceptDictionaryView.UpdateState(
                _isConceptOperationInProgress,
                ViewModel.SelectedNotebook is not null,
                hasCurrentConcept,
                ViewModel.Concepts.Count > 0,
                ViewModel.ConceptReferences.Count > 0);
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
            TopBar.SetSearchInputEnabled(!isInProgress);
            ConceptDictionaryView.SetInteractionEnabled(!isInProgress);
            UpdateConceptEditorState();
        }
        private void UpdateNotebookEmptyState()
        {
            NotebookSidebar.UpdateEmptyState(ViewModel.Notebooks.Count > 0);
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
            PageSidebar.UpdateState(hasSection, ViewModel.Pages.Count > 0);
            EditorView.UpdatePageState(hasPage);
        }

        private void SyncPageEditorContent(bool force = false)
        {
            EditorView.ContentEditor.Document.GetText(
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
                    EditorView.ContentEditor.Document.SetText(
                        TextSetOptions.None,
                        ViewModel.PageContent);
                }
                else
                {
                    EditorView.ContentEditor.Document.SetText(
                        TextSetOptions.FormatRtf,
                        RtfThemeColorNormalizer.Normalize(
                            ViewModel.PageFormattedContent));
                }

                ApplyEditorThemeForeground();
            }
            finally
            {
                _isUpdatingPageEditorContent = false;
                EndInternalEditorChange();
            }

            QueueConceptHighlightRefresh();
            UpdateFormattingToolbarState();
        }

        private void ApplyEditorThemeForeground()
        {
            if (EditorView.ContentEditor.Foreground is not SolidColorBrush foregroundBrush)
            {
                return;
            }

            var document = EditorView.ContentEditor.Document;
            var defaultFormat = document.GetDefaultCharacterFormat();
            defaultFormat.ForegroundColor = foregroundBrush.Color;
            document.SetDefaultCharacterFormat(defaultFormat);

            var selection = document.Selection;
            var selectionStart = selection.StartPosition;
            var selectionEnd = selection.EndPosition;
            var selectionFormat = selection.CharacterFormat;
            selectionFormat.ForegroundColor = foregroundBrush.Color;
            selection.CharacterFormat = selectionFormat;

            document.GetText(TextGetOptions.None, out var content);
            if (content.Length == 0)
            {
                return;
            }

            var documentRange = document.GetRange(0, content.Length);
            var documentFormat = documentRange.CharacterFormat;
            documentFormat.ForegroundColor = foregroundBrush.Color;
            documentRange.CharacterFormat = documentFormat;
            selection.SetRange(selectionStart, selectionEnd);
        }

        private void RefreshEditorThemeForeground()
        {
            if (!IsSelectedPageCurrent())
            {
                return;
            }

            BeginInternalEditorChange();
            EditorView.ContentEditor.Document.BatchDisplayUpdates();
            try
            {
                ApplyEditorThemeForeground();
            }
            finally
            {
                EditorView.ContentEditor.Document.ApplyDisplayUpdates();
                EndInternalEditorChange();
            }
        }

        private void GetPersistedPageEditorContent(
            out string content,
            out string formattedContent)
        {
            EditorView.ContentEditor.Document.GetText(
                TextGetOptions.None,
                out content);

            _isApplyingConceptHighlighting = true;
            BeginInternalEditorChange();
            EditorView.ContentEditor.Document.BatchDisplayUpdates();
            try
            {
                if (!_themeSettings.HighContrast && content.Length > 0)
                {
                    var documentRange = EditorView.ContentEditor.Document.GetRange(
                        0,
                        content.Length);
                    var documentFormat = documentRange.CharacterFormat;
                    documentFormat.BackgroundColor = Colors.Transparent;
                    documentRange.CharacterFormat = documentFormat;
                }

                EditorView.ContentEditor.Document.GetText(
                    TextGetOptions.FormatRtf,
                    out formattedContent);
                formattedContent = RtfThemeColorNormalizer.Normalize(
                    formattedContent);
            }
            finally
            {
                EditorView.ContentEditor.Document.ApplyDisplayUpdates();
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
                EditorView.ParagraphStyleSelector.SelectedIndex < 0)
            {
                return;
            }

            var selectedStyle = EditorView.ParagraphStyleSelector.SelectedIndex;
            var selection = EditorView.ContentEditor.Document.Selection;
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
            EditorView.ContentEditor.Focus(FocusState.Programmatic);
        }

        private void BoldButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingFormattingToolbar)
            {
                return;
            }

            var selection = EditorView.ContentEditor.Document.Selection;
            var characterFormat = selection.CharacterFormat;
            characterFormat.Bold = EditorView.BoldToggle.IsChecked == true
                ? FormatEffect.On
                : FormatEffect.Off;
            selection.CharacterFormat = characterFormat;

            UpdatePageContentFromEditor();
            EditorView.ContentEditor.Focus(FocusState.Programmatic);
        }

        private void ItalicButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingFormattingToolbar)
            {
                return;
            }

            var selection = EditorView.ContentEditor.Document.Selection;
            var characterFormat = selection.CharacterFormat;
            characterFormat.Italic = EditorView.ItalicToggle.IsChecked == true
                ? FormatEffect.On
                : FormatEffect.Off;
            selection.CharacterFormat = characterFormat;

            UpdatePageContentFromEditor();
            EditorView.ContentEditor.Focus(FocusState.Programmatic);
        }

        private void UnderlineButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingFormattingToolbar)
            {
                return;
            }

            var selection = EditorView.ContentEditor.Document.Selection;
            var characterFormat = selection.CharacterFormat;
            characterFormat.Underline = EditorView.UnderlineToggle.IsChecked == true
                ? UnderlineType.Single
                : UnderlineType.None;
            selection.CharacterFormat = characterFormat;

            UpdatePageContentFromEditor();
            EditorView.ContentEditor.Focus(FocusState.Programmatic);
        }

        private void BulletedListButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingFormattingToolbar)
            {
                return;
            }

            ApplyListFormatting(
                MarkerType.Bullet,
                EditorView.BulletedListToggle.IsChecked == true);
        }

        private void NumberedListButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingFormattingToolbar)
            {
                return;
            }

            ApplyListFormatting(
                MarkerType.Arabic,
                EditorView.NumberedListToggle.IsChecked == true);
        }

        private void ApplyListFormatting(MarkerType listType, bool isEnabled)
        {
            var selection = EditorView.ContentEditor.Document.Selection;
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
            EditorView.ContentEditor.Focus(FocusState.Programmatic);
        }

        private void UpdateFormattingToolbarState()
        {
            if (!IsSelectedPageCurrent())
            {
                return;
            }

            var characterFormat =
                EditorView.ContentEditor.Document.Selection.CharacterFormat;
            var paragraphStyle =
                EditorView.ContentEditor.Document.Selection.ParagraphFormat.Style;
            var listType =
                EditorView.ContentEditor.Document.Selection.ParagraphFormat.ListType;

            _isUpdatingFormattingToolbar = true;
            try
            {
                EditorView.ParagraphStyleSelector.SelectedIndex = paragraphStyle switch
                {
                    ParagraphStyle.Heading1 => 1,
                    ParagraphStyle.Heading2 => 2,
                    ParagraphStyle.Normal or ParagraphStyle.None => 0,
                    _ => -1
                };
                EditorView.BoldToggle.IsChecked = characterFormat.Bold == FormatEffect.On;
                EditorView.ItalicToggle.IsChecked = characterFormat.Italic == FormatEffect.On;
                EditorView.UnderlineToggle.IsChecked =
                    characterFormat.Underline != UnderlineType.None &&
                    characterFormat.Underline != UnderlineType.Undefined;
                EditorView.BulletedListToggle.IsChecked = listType == MarkerType.Bullet;
                EditorView.NumberedListToggle.IsChecked = listType == MarkerType.Arabic;
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
            EditorView.ContentEditor.Document.GetText(
                TextGetOptions.None,
                out var editorContent);

            var selection = EditorView.ContentEditor.Document.Selection;
            var selectionStart = selection.StartPosition;
            var selectionEnd = selection.EndPosition;

            _isApplyingConceptHighlighting = true;
            BeginInternalEditorChange();
            EditorView.ContentEditor.Document.BatchDisplayUpdates();
            try
            {
                if (!_themeSettings.HighContrast)
                {
                    if (editorContent.Length > 0)
                    {
                        var documentRange = EditorView.ContentEditor.Document.GetRange(
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
                        var conceptRange = EditorView.ContentEditor.Document.GetRange(
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
            }
            finally
            {
                EditorView.ContentEditor.Document.ApplyDisplayUpdates();
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
            var selection = EditorView.ContentEditor.Document.Selection;
            if (ViewModel.SelectedPage is null ||
                selection.StartPosition != selection.EndPosition)
            {
                HideConceptDefinition();
                return;
            }

            EditorView.ContentEditor.Document.GetText(
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
            EditorView.ShowConceptDefinition(
                concept.Name,
                concept.Definition,
                e.GetPosition(EditorView.ContentEditor));
        }

        private void HideConceptDefinition()
        {
            EditorView.HideConceptDefinition();
        }
        private bool IsSelectedPageCurrent()
        {
            var page = ViewModel.SelectedPage;
            return page is not null &&
                page.SectionId == ViewModel.SelectedSection?.Id &&
                ViewModel.SelectedSection?.NotebookId == ViewModel.SelectedNotebook?.Id &&
                ReferenceEquals(PageSidebar.SelectedPage, page);
        }
        private async void RootGrid_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            UpdateTitleBarTheme();
            if (_shouldRestoreMaximizedState &&
                AppWindow.Presenter is OverlappedPresenter presenter)
            {
                _shouldRestoreMaximizedState = false;
                presenter.Maximize();
            }
            await RunOperationAsync(
                () => ViewModel.LoadNotebooksCommand.ExecuteAsync(null),
                "Notebooks couldn't be loaded");
        }

        private void RootGrid_ActualThemeChanged(
            FrameworkElement sender,
            object args)
        {
            UpdateTitleBarTheme();
            RefreshEditorThemeForeground();
            QueueConceptHighlightRefresh();
        }

        private void ThemeSettings_Changed(
            ThemeSettings sender,
            object args)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                UpdateTitleBarTheme();
                if (_themeSettings.HighContrast && IsSelectedPageCurrent())
                {
                    SyncPageEditorContent(force: true);
                }
                else
                {
                    QueueConceptHighlightRefresh();
                }
            });
        }

        private void UpdateTitleBarTheme()
        {
            if (!AppWindowTitleBar.IsCustomizationSupported())
            {
                return;
            }

            var isDarkTheme = RootGrid.ActualTheme == ElementTheme.Dark;
            var titleBar = AppWindow.TitleBar;

            titleBar.BackgroundColor = isDarkTheme
                ? ColorHelper.FromArgb(255, 32, 32, 32)
                : ColorHelper.FromArgb(255, 243, 243, 243);
            titleBar.ForegroundColor = isDarkTheme
                ? Colors.White
                : Colors.Black;
            titleBar.InactiveBackgroundColor = titleBar.BackgroundColor;
            titleBar.InactiveForegroundColor = isDarkTheme
                ? ColorHelper.FromArgb(255, 160, 160, 160)
                : ColorHelper.FromArgb(255, 96, 96, 96);
            titleBar.ButtonBackgroundColor = titleBar.BackgroundColor;
            titleBar.ButtonForegroundColor = titleBar.ForegroundColor;
            titleBar.ButtonInactiveBackgroundColor = titleBar.InactiveBackgroundColor;
            titleBar.ButtonInactiveForegroundColor = titleBar.InactiveForegroundColor;
            titleBar.ButtonHoverBackgroundColor = isDarkTheme
                ? ColorHelper.FromArgb(255, 51, 51, 51)
                : ColorHelper.FromArgb(255, 229, 229, 229);
            titleBar.ButtonHoverForegroundColor = titleBar.ForegroundColor;
            titleBar.ButtonPressedBackgroundColor = isDarkTheme
                ? ColorHelper.FromArgb(255, 64, 64, 64)
                : ColorHelper.FromArgb(255, 218, 218, 218);
            titleBar.ButtonPressedForegroundColor = titleBar.ForegroundColor;
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

                if (!await ViewModel.SaveCurrentPageAsync())
                {
                    return;
                }

                if (!await RunOperationAsync(
                        () => ViewModel.LoadConceptsAsync(notebook.Id),
                        "Concepts couldn't be loaded"))
                {
                    return;
                }

                if (ViewModel.SelectedNotebook != notebook)
                {
                    return;
                }

                ConceptDictionaryView.ClearEditor();
                EditorView.Visibility = Visibility.Collapsed;
                ConceptDictionaryView.Visibility = Visibility.Visible;
                QueueKeyboardFocus(
                    ConceptDictionaryView.FocusSelectedConceptOrNewButton);
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
                QueueKeyboardFocus(FocusPageWorkspace);
            }
        }

        private void NewConceptButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConceptOperationInProgress)
            {
                return;
            }

            ConceptDictionaryView.ClearEditor();
            UpdateConceptEditorState();
            QueueKeyboardFocus(ConceptDictionaryView.FocusConceptName);
        }

        private async void ConceptsListView_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (ViewModel is null)
            {
                return;
            }

            if (ConceptDictionaryView.SelectedConcept is Concept concept)
            {
                ConceptDictionaryView.SetInput(concept.Name, concept.Definition);
            }
            else
            {
                ConceptDictionaryView.SetInput(string.Empty, string.Empty);
            }

            ConceptDictionaryView.Message = string.Empty;
            UpdateConceptEditorState();
            await RunOperationAsync(
                () => ViewModel.LoadConceptReferencesAsync(
                    ConceptDictionaryView.SelectedConcept),
                "Concept references couldn't be loaded");
            UpdateConceptEditorState();
        }

        private async void ConceptReferencesListView_SelectionChanged(
            object sender, SelectionChangedEventArgs e)
        {
            if (_isConceptOperationInProgress ||
                ConceptDictionaryView.SelectedReference
                    is not ConceptReference reference)
            {
                return;
            }

            SetConceptOperationInProgress(true);
            _selectionNavigationVersion++;
            await _selectionNavigationLock.WaitAsync();
            try
            {
                var navigation = await RunOperationAsync(
                    () => ViewModel.NavigateToConceptReferenceAsync(reference),
                    "Referenced page couldn't be opened");
                if (!navigation.Succeeded)
                {
                    ConceptDictionaryView.ClearSelectedReference();
                    return;
                }

                if (navigation.Result)
                {
                    ShowPageEditor();
                    RefreshSectionNavigation();
                    PageSidebar.SelectPage(ViewModel.SelectedPage);
                    UpdatePageState();
                    QueueKeyboardFocus(() =>
                        EditorView.ContentEditor.Focus(FocusState.Keyboard));
                }
                else if (!ViewModel.HasPageSaveError)
                {
                    ConceptDictionaryView.Message =
                        "This page is no longer available.";
                }

                ConceptDictionaryView.ClearSelectedReference();
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
                ConceptDictionaryView.Message = string.Empty;
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

            var selected = ConceptDictionaryView.SelectedConcept;
            if (selected is not null && selected.NotebookId != notebook.Id)
            {
                return;
            }

            var name = ConceptDictionaryView.ConceptName;
            var definition = ConceptDictionaryView.ConceptDefinition;
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
                ConceptDictionaryView.SelectConcept(ViewModel.Concepts
                    .FirstOrDefault(concept => concept.Id == saved.Id));
                ConceptDictionaryView.Message = "Concept saved.";
                QueueKeyboardFocus(
                    ConceptDictionaryView.FocusSelectedConceptOrNewButton);
            }
            catch (DuplicateConceptNameException ex)
            {
                ConceptDictionaryView.Message = ex.Message;
            }
            catch (ArgumentException ex)
            {
                ConceptDictionaryView.Message = ex.Message;
            }
            catch (Exception)
            {
                ShowOperationError(
                    "Concept couldn't be saved",
                    DatabaseRetryMessage);
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
                ConceptDictionaryView.SelectedConcept is not Concept concept ||
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
                ConceptDictionaryView.SelectedConcept != concept)
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

                if (!await RunOperationAsync(
                        () => ViewModel.DeleteConceptAsync(concept),
                        "Concept couldn't be deleted"))
                {
                    return;
                }

                if (!await RunOperationAsync(
                        () => ViewModel.LoadConceptsAsync(notebook.Id),
                        "Concepts couldn't be refreshed"))
                {
                    return;
                }

                ConceptDictionaryView.Message = "Concept deleted.";
                QueueKeyboardFocus(
                    ConceptDictionaryView.FocusSelectedConceptOrNewButton);
            }
            finally
            {
                _selectionNavigationLock.Release();
                SetConceptOperationInProgress(false);
            }
        }
        private async void TopBar_SearchRequested(object sender, RoutedEventArgs e)
        {
            if (_isRenamingSelection || _isSearchNavigationInProgress ||
                _isLibraryOperationInProgress)
            {
                return;
            }

            TopBar.SetSearchSubmissionEnabled(false);
            TopBar.SetLibraryActionsEnabled(false);
            try
            {
                ViewModel.SearchText = TopBar.SearchText;
                await RunOperationAsync(
                    () => ViewModel.SearchPagesCommand.ExecuteAsync(null),
                    "Search couldn't be completed");
            }
            finally
            {
                TopBar.SetSearchSubmissionEnabled(true);
                TopBar.SetLibraryActionsEnabled(true);
            }
        }

        private async void TopBar_BackupRequested(
            object sender,
            RoutedEventArgs e)
        {
            if (_isLibraryOperationInProgress || _isRenamingSelection ||
                _isSearchNavigationInProgress ||
                _isConceptOperationInProgress)
            {
                return;
            }

            _isLibraryOperationInProgress = true;
            ClearOperationMessage();
            SetNavigationEnabled(false);
            TopBar.SetSearchInputEnabled(false);
            TopBar.SetLibraryOperationInProgress(true);

            StorageFile? selectedFile = null;
            var deleteSelectedFileOnFailure = false;
            try
            {
                if (!await ViewModel.SaveCurrentPageAsync())
                {
                    return;
                }

                var picker = CreateBackupFilePicker();
                selectedFile = await picker.PickSaveFileAsync();
                if (selectedFile is null)
                {
                    return;
                }

                var selectedFileProperties =
                    await selectedFile.GetBasicPropertiesAsync();
                deleteSelectedFileOnFailure =
                    selectedFileProperties.Size == 0;

                var result = await _libraryBackupService.CreateAsync(
                    selectedFile.Path);
                ShowOperationSuccess(
                    "Backup created",
                    $"Saved {FormatFileSize(result.SizeInBytes)} to " +
                    $"{result.FilePath}");
            }
            catch (Exception)
            {
                if (deleteSelectedFileOnFailure && selectedFile is not null)
                {
                    await TryDeleteEmptyPickedFileAsync(selectedFile);
                }

                ShowOperationError(
                    "Backup couldn't be created",
                    "Check that the selected folder is available and has " +
                    "enough free space, then try again.");
            }
            finally
            {
                _isLibraryOperationInProgress = false;
                TopBar.SetLibraryOperationInProgress(false);
                TopBar.SetSearchInputEnabled(true);
                SetNavigationEnabled(true);
            }
        }

        private async void TopBar_RestoreRequested(
            object sender,
            RoutedEventArgs e)
        {
            if (_isLibraryOperationInProgress || _isRenamingSelection ||
                _isSearchNavigationInProgress ||
                _isConceptOperationInProgress)
            {
                return;
            }

            _isLibraryOperationInProgress = true;
            ClearOperationMessage();
            SetNavigationEnabled(false);
            TopBar.SetSearchInputEnabled(false);
            TopBar.SetLibraryOperationInProgress(true);
            var isClosingForRestore = false;

            try
            {
                var picker = CreateRestoreFilePicker();
                var selectedFile = await picker.PickSingleFileAsync();
                if (selectedFile is null)
                {
                    return;
                }

                var validation = await _libraryBackupValidator.ValidateAsync(
                    selectedFile.Path);
                if (!await ConfirmRestoreAsync(
                        selectedFile.Name,
                        validation.Manifest.CreatedAtUtc))
                {
                    return;
                }

                if (!await ViewModel.SaveCurrentPageAsync())
                {
                    return;
                }

                await _libraryRestoreService.PrepareAsync(selectedFile.Path);

                _allowWindowClose = true;
                try
                {
                    Close();
                    isClosingForRestore = true;
                }
                catch
                {
                    _allowWindowClose = false;
                    throw;
                }
            }
            catch (InvalidLibraryBackupException exception)
            {
                ShowOperationError(
                    "Backup can't be restored",
                    exception.Message);
            }
            catch (Exception)
            {
                ShowOperationError(
                    "Restore couldn't be prepared",
                    "Your current library was not replaced. Check that the " +
                    "backup and local data folders are available and have " +
                    "enough free space, then try again.");
            }
            finally
            {
                _isLibraryOperationInProgress = false;
                if (!isClosingForRestore)
                {
                    TopBar.SetLibraryOperationInProgress(false);
                    TopBar.SetSearchInputEnabled(true);
                    SetNavigationEnabled(true);
                }
            }
        }

        private async Task<bool> ConfirmRestoreAsync(
            string backupFileName,
            DateTimeOffset backupCreatedAtUtc)
        {
            var dialog = new ContentDialog
            {
                XamlRoot = Content.XamlRoot,
                Title = "Restore this backup?",
                Content =
                    $"Restore \"{backupFileName}\" from " +
                    $"{backupCreatedAtUtc.ToLocalTime():g}?\n\n" +
                    "This will replace every notebook, section, page, " +
                    "formatted note, and concept in your current library. " +
                    "Syllanote will create a safety backup first and then " +
                    "close. Reopen it to finish the restore.",
                PrimaryButtonText = "Restore and close",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close
            };

            return await dialog.ShowAsync() == ContentDialogResult.Primary;
        }

        private FileOpenPicker CreateRestoreFilePicker()
        {
            var picker = new FileOpenPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add(LibraryBackupFormat.FileExtension);

            InitializeWithWindow.Initialize(
                picker,
                WindowNative.GetWindowHandle(this));
            return picker;
        }

        private FileSavePicker CreateBackupFilePicker()
        {
            var picker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SuggestedFileName =
                    $"Syllanote-backup-{DateTime.Now:yyyy-MM-dd-HHmm}"
            };
            picker.FileTypeChoices.Add(
                "Syllanote backup",
                [LibraryBackupFormat.FileExtension]);

            InitializeWithWindow.Initialize(
                picker,
                WindowNative.GetWindowHandle(this));
            return picker;
        }

        private static string FormatFileSize(long sizeInBytes)
        {
            const double bytesPerKilobyte = 1024;
            const double bytesPerMegabyte = bytesPerKilobyte * 1024;

            return sizeInBytes >= bytesPerMegabyte
                ? $"{sizeInBytes / bytesPerMegabyte:0.##} MB"
                : $"{Math.Max(sizeInBytes / bytesPerKilobyte, 0.01):0.##} KB";
        }

        private static async Task TryDeleteEmptyPickedFileAsync(
            StorageFile file)
        {
            try
            {
                var properties = await file.GetBasicPropertiesAsync();
                if (properties.Size == 0)
                {
                    await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                }
            }
            catch
            {
                // The original backup error remains the useful user-facing result.
            }
        }

        private async void TopBar_SearchResultInvoked(
            object sender, ItemClickEventArgs e)
        {
            if (_isRenamingSelection || _isSearchNavigationInProgress ||
                e.ClickedItem is not SearchPageResult result)
            {
                return;
            }

            _isSearchNavigationInProgress = true;
            _selectionNavigationVersion++;
            SetNavigationEnabled(false);
            TopBar.SetSearchEnabled(false);
            await _selectionNavigationLock.WaitAsync();
            try
            {
                var navigation = await RunOperationAsync(
                    () => ViewModel.NavigateToSearchResultAsync(result),
                    "Search result couldn't be opened");
                if (!navigation.Succeeded)
                {
                    TopBar.ClearSelectedSearchResult();
                    return;
                }

                if (navigation.Result)
                {
                    ShowPageEditor();
                    QueueKeyboardFocus(() =>
                        EditorView.ContentEditor.Focus(FocusState.Keyboard));
                }
                RefreshSectionNavigation();
                PageSidebar.SelectPage(ViewModel.SelectedPage);
                UpdatePageState();
                TopBar.ClearSelectedSearchResult();
            }
            finally
            {
                _selectionNavigationLock.Release();
                TopBar.SetSearchEnabled(true);
                SetNavigationEnabled(true);
                _isSearchNavigationInProgress = false;
            }
        }
        private async void NewNotebookButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isRenamingSelection) return;

            var existingNotebookIds = ViewModel.Notebooks
                .Select(notebook => notebook.Id)
                .ToHashSet();

            var nameTextBox = new TextBox
            {
                Header = "Notebook name (required)",
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

            FocusDialogTextBox(dialog, nameTextBox);

            nameTextBox.TextChanged += (_, _) =>
                dialog.IsPrimaryButtonEnabled =
                    !string.IsNullOrWhiteSpace(nameTextBox.Text);

            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                ViewModel.NewNotebookName = nameTextBox.Text;
                if (await RunOperationAsync(
                        () => ViewModel.CreateNotebookCommand.ExecuteAsync(null),
                        "Notebook couldn't be created"))
                {
                    var createdNotebook = ViewModel.Notebooks.FirstOrDefault(
                        notebook => !existingNotebookIds.Contains(notebook.Id));
                    QueueKeyboardFocus(() =>
                        NotebookSidebar.FocusNotebook(createdNotebook));
                }
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

            var existingSectionIds = ViewModel.Sections
                .Select(section => section.Id)
                .ToHashSet();

            var nameTextBox = new TextBox
            {
                Header = "Section name (required)",
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

            FocusDialogTextBox(dialog, nameTextBox);

            nameTextBox.TextChanged += (_, _) =>
                dialog.IsPrimaryButtonEnabled =
                    !string.IsNullOrWhiteSpace(nameTextBox.Text);

            if (await dialog.ShowAsync() == ContentDialogResult.Primary &&
                ViewModel.SelectedNotebook == notebook)
            {
                ViewModel.NewSectionName = nameTextBox.Text;
                if (await RunOperationAsync(
                        () => ViewModel.CreateSectionCommand.ExecuteAsync(null),
                        "Section couldn't be created"))
                {
                    var createdSection = ViewModel.Sections.FirstOrDefault(
                        section => !existingSectionIds.Contains(section.Id));
                    QueueKeyboardFocus(() =>
                        NotebookSidebar.FocusSection(createdSection));
                }
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

            var existingPageIds = ViewModel.Pages
                .Select(page => page.Id)
                .ToHashSet();

            var titleTextBox = new TextBox
            {
                Header = "Page title (required)",
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

            FocusDialogTextBox(dialog, titleTextBox);

            titleTextBox.TextChanged += (_, _) =>
                dialog.IsPrimaryButtonEnabled =
                    !string.IsNullOrWhiteSpace(titleTextBox.Text);

            if (await dialog.ShowAsync() == ContentDialogResult.Primary &&
                ViewModel.SelectedSection == section &&
                ViewModel.SelectedNotebook?.Id == section.NotebookId)
            {
                ViewModel.NewPageTitle = titleTextBox.Text;
                if (await RunOperationAsync(
                        () => ViewModel.CreatePageCommand.ExecuteAsync(null),
                        "Page couldn't be created"))
                {
                    var createdPage = ViewModel.Pages.FirstOrDefault(
                        page => !existingPageIds.Contains(page.Id));
                    QueueKeyboardFocus(() =>
                        PageSidebar.FocusPage(createdPage));
                }
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
            await _selectionNavigationLock.WaitAsync();
            try
            {
                if (navigationVersion != _selectionNavigationVersion ||
                    _isSearchNavigationInProgress)
                {
                    return;
                }

                if (!await ViewModel.SaveCurrentPageAsync())
                {
                    SyncNavigationSelection();
                    return;
                }

                var previousSection = ViewModel.SelectedSection;
                ViewModel.SelectedSection = currentSection;
                ShowPageEditor();
                if (!await RunOperationAsync(
                        () => ViewModel.LoadPagesCommand.ExecuteAsync(null),
                        "Pages couldn't be loaded"))
                {
                    ViewModel.SelectedSection = previousSection;
                    SyncNavigationSelection();
                    return;
                }

                if (navigationVersion == _selectionNavigationVersion &&
                    ViewModel.SelectedSection?.Id == currentSection.Id)
                {
                    SyncNavigationSelection();
                    QueueKeyboardFocus(() =>
                        NotebookSidebar.FocusSection(currentSection));
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
                QueueKeyboardFocus(() =>
                    NotebookSidebar.FocusNotebook(notebook));
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
                    if (!await ViewModel.SaveCurrentPageAsync())
                    {
                        RefreshSectionNavigation();
                        return false;
                    }

                    var previousNotebook = ViewModel.SelectedNotebook;
                    ViewModel.SelectedNotebook = currentNotebook;
                    if (!await RunOperationAsync(
                            () => ViewModel.LoadSectionsCommand.ExecuteAsync(null),
                            "Notebook couldn't be opened"))
                    {
                        ViewModel.SelectedNotebook = previousNotebook;
                        RefreshSectionNavigation();
                        return false;
                    }
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
                    if (!await ViewModel.SaveCurrentPageAsync())
                    {
                        SyncNavigationSelection();
                        return null;
                    }

                    var previousSection = ViewModel.SelectedSection;
                    ViewModel.SelectedSection = currentSection;
                    if (!await RunOperationAsync(
                            () => ViewModel.LoadPagesCommand.ExecuteAsync(null),
                            "Section couldn't be opened"))
                    {
                        ViewModel.SelectedSection = previousSection;
                        SyncNavigationSelection();
                        return null;
                    }
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
                    if (!await ViewModel.SelectPageAsync(currentPage))
                    {
                        PageSidebar.SelectPage(ViewModel.SelectedPage);
                        return null;
                    }
                }

                PageSidebar.SelectPage(currentPage);
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
                Header = "Notebook name (required)",
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

            FocusDialogTextBox(dialog, nameTextBox, selectAll: true);

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
                    var renamed = await RunOperationAsync(
                        () => ViewModel.RenameNotebookCommand.ExecuteAsync(null),
                        "Notebook couldn't be renamed");
                    if (!renamed || ViewModel.HasPageSaveError)
                    {
                        return;
                    }

                    ViewModel.SelectedNotebook = notebook;
                    RebuildNotebookNavigation();
                    QueueKeyboardFocus(() =>
                        NotebookSidebar.FocusNotebook(notebook));
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
                    if (!await RunOperationAsync(
                            () => ViewModel.MoveSelectedNotebookUpCommand.ExecuteAsync(null),
                            "Notebook couldn't be moved"))
                    {
                        return;
                    }
                }
                else
                {
                    if (!await RunOperationAsync(
                            () => ViewModel.MoveSelectedNotebookDownCommand.ExecuteAsync(null),
                            "Notebook couldn't be moved"))
                    {
                        return;
                    }
                }
                RebuildNotebookNavigation();
                QueueKeyboardFocus(() =>
                    NotebookSidebar.FocusNotebook(notebook));
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
                Header = "Section name (required)",
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

            FocusDialogTextBox(dialog, nameTextBox, selectAll: true);

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
                    var renamed = await RunOperationAsync(
                        () => ViewModel.RenameSectionCommand.ExecuteAsync(null),
                        "Section couldn't be renamed");
                    if (!renamed || ViewModel.HasPageSaveError)
                    {
                        return;
                    }

                    ViewModel.SelectedSection = section;
                    RefreshSectionNavigation();
                    QueueKeyboardFocus(() =>
                        NotebookSidebar.FocusSection(section));
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
                    if (!await RunOperationAsync(
                            () => ViewModel.MoveSelectedSectionUpCommand.ExecuteAsync(null),
                            "Section couldn't be moved"))
                    {
                        return;
                    }
                }
                else
                {
                    if (!await RunOperationAsync(
                            () => ViewModel.MoveSelectedSectionDownCommand.ExecuteAsync(null),
                            "Section couldn't be moved"))
                    {
                        return;
                    }
                }
                RefreshSectionNavigation();
                QueueKeyboardFocus(() =>
                    NotebookSidebar.FocusSection(currentSection));
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

            var page = PageSidebar.SelectedPage;

            var navigationVersion = _selectionNavigationVersion;
            await _selectionNavigationLock.WaitAsync();
            try
            {
                if (navigationVersion != _selectionNavigationVersion ||
                    _isSearchNavigationInProgress)
                {
                    return;
                }

                if (!await ViewModel.SelectPageAsync(page))
                {
                    PageSidebar.SelectPage(ViewModel.SelectedPage);
                    UpdatePageState();
                    return;
                }

                if (page is not null)
                {
                    ShowPageEditor();
                }

                UpdatePageState();
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
                if (!await ViewModel.SaveCurrentPageAsync())
                {
                    return;
                }

                if (moveUp)
                {
                    if (!await RunOperationAsync(
                            () => ViewModel.MoveSelectedPageUpCommand.ExecuteAsync(null),
                            "Page couldn't be moved"))
                    {
                        return;
                    }
                }
                else
                {
                    if (!await RunOperationAsync(
                            () => ViewModel.MoveSelectedPageDownCommand.ExecuteAsync(null),
                            "Page couldn't be moved"))
                    {
                        return;
                    }
                }
                PageSidebar.SelectPage(currentPage);
                UpdatePageState();
                QueueKeyboardFocus(() =>
                    PageSidebar.FocusPage(currentPage));
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
                Header = "Page title (required)",
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

            FocusDialogTextBox(dialog, titleTextBox, selectAll: true);

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
                    var renamed = await RunOperationAsync(
                        () => ViewModel.RenamePageCommand.ExecuteAsync(null),
                        "Page couldn't be renamed");
                    if (!renamed || ViewModel.HasPageSaveError)
                    {
                        return;
                    }

                    PageSidebar.SelectPage(page);
                    UpdatePageState();
                    QueueKeyboardFocus(() =>
                        PageSidebar.FocusPage(page));
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
            var deletedPageIndex = ViewModel.Pages.IndexOf(page);

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
                if (await RunOperationAsync(
                        () => ViewModel.DeletePageCommand.ExecuteAsync(null),
                        "Page couldn't be deleted"))
                {
                    var nextPage = ViewModel.Pages.Count == 0
                        ? null
                        : ViewModel.Pages[Math.Min(
                            deletedPageIndex,
                            ViewModel.Pages.Count - 1)];
                    QueueKeyboardFocus(() => nextPage is not null
                        ? PageSidebar.FocusPage(nextPage)
                        : PageSidebar.FocusNewPageButton());
                }
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
            var deletedSectionIndex = ViewModel.Sections.IndexOf(section);

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
                if (await RunOperationAsync(
                        () => ViewModel.DeleteSectionCommand.ExecuteAsync(null),
                        "Section couldn't be deleted"))
                {
                    RefreshSectionNavigation();
                    var nextSection = ViewModel.Sections.Count == 0
                        ? null
                        : ViewModel.Sections[Math.Min(
                            deletedSectionIndex,
                            ViewModel.Sections.Count - 1)];
                    QueueKeyboardFocus(() => nextSection is not null
                        ? NotebookSidebar.FocusSection(nextSection)
                        : NotebookSidebar.FocusNewSectionButton(notebook: ViewModel.SelectedNotebook));
                }
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
            var deletedNotebookIndex = ViewModel.Notebooks.IndexOf(notebook);

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
                if (await RunOperationAsync(
                        () => ViewModel.DeleteNotebookCommand.ExecuteAsync(null),
                        "Notebook couldn't be deleted"))
                {
                    RebuildNotebookNavigation();
                    var nextNotebook = ViewModel.Notebooks.Count == 0
                        ? null
                        : ViewModel.Notebooks[Math.Min(
                            deletedNotebookIndex,
                            ViewModel.Notebooks.Count - 1)];
                    QueueKeyboardFocus(() => nextNotebook is not null
                        ? NotebookSidebar.FocusNotebook(nextNotebook)
                        : NotebookSidebar.FocusNewNotebookButton());
                }
            }
        }
    }
}
