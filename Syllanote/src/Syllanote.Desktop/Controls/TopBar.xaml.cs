using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Syllanote.Application.Notebooks.Sections.Pages.SearchPages;
using Windows.System;

namespace Syllanote.Desktop.Controls
{
    public sealed partial class TopBar : UserControl
    {
        public event RoutedEventHandler? BackupRequested;
        public event RoutedEventHandler? SearchRequested;
        public event SelectionChangedEventHandler? SearchResultSelectionChanged;

        public string SearchText => SearchTextBox.Text;

        public SearchPageResult? SelectedSearchResult =>
            SearchResultsListView.SelectedItem as SearchPageResult;

        public TopBar()
        {
            InitializeComponent();
        }

        public void SetSearchEnabled(bool isEnabled)
        {
            SearchButton.IsEnabled = isEnabled;
            SearchTextBox.IsEnabled = isEnabled;
            SearchResultsListView.IsEnabled = isEnabled;
        }

        public void SetSearchInputEnabled(bool isEnabled)
        {
            SearchButton.IsEnabled = isEnabled;
            SearchTextBox.IsEnabled = isEnabled;
        }

        public void SetSearchSubmissionEnabled(bool isEnabled)
        {
            SearchButton.IsEnabled = isEnabled;
            SearchProgressRing.IsActive = !isEnabled;
            SearchProgressRing.Visibility = isEnabled
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public void SetBackupEnabled(bool isEnabled)
        {
            BackupButton.IsEnabled = isEnabled;
        }

        public void SetBackupInProgress(bool isInProgress)
        {
            BackupButton.IsEnabled = !isInProgress;
            BackupProgressRing.IsActive = isInProgress;
            BackupProgressRing.Visibility = isInProgress
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public void ClearSelectedSearchResult()
        {
            SearchResultsListView.SelectedItem = null;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchRequested?.Invoke(this, e);
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            BackupRequested?.Invoke(this, e);
        }

        private void SearchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter || !SearchButton.IsEnabled)
            {
                return;
            }

            e.Handled = true;
            SearchRequested?.Invoke(this, e);
        }

        private void FocusSearchKeyboardAccelerator_Invoked(
            KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs e)
        {
            if (!SearchTextBox.IsEnabled)
            {
                return;
            }

            SearchTextBox.Focus(FocusState.Keyboard);
            SearchTextBox.SelectAll();
            e.Handled = true;
        }

        private void SearchResultsListView_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            SearchResultSelectionChanged?.Invoke(this, e);
        }
    }
}
