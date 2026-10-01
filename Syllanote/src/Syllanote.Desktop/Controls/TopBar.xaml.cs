using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Syllanote.Application.Notebooks.Sections.Pages.SearchPages;
using Windows.System;

namespace Syllanote.Desktop.Controls
{
    public sealed partial class TopBar : UserControl
    {
        public event RoutedEventHandler? BackupRequested;
        public event RoutedEventHandler? RestoreRequested;
        public event RoutedEventHandler? SearchRequested;
        public event ItemClickEventHandler? SearchResultInvoked;

        public string SearchText => SearchTextBox.Text;

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

        public void SetLibraryActionsEnabled(bool isEnabled)
        {
            BackupButton.IsEnabled = isEnabled;
            RestoreButton.IsEnabled = isEnabled;
        }

        public void SetLibraryOperationInProgress(bool isInProgress)
        {
            SetLibraryActionsEnabled(!isInProgress);
            LibraryOperationProgressRing.IsActive = isInProgress;
            LibraryOperationProgressRing.Visibility = isInProgress
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

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            RestoreRequested?.Invoke(this, e);
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

        private void SearchResultsListView_ItemClick(
            object sender,
            ItemClickEventArgs e)
        {
            SearchResultInvoked?.Invoke(this, e);
        }

        private void SearchResultsListView_ContainerContentChanging(
            ListViewBase sender,
            ContainerContentChangingEventArgs args)
        {
            if (args.ItemContainer is null)
            {
                return;
            }

            if (args.InRecycleQueue || args.Item is not SearchPageResult result)
            {
                AutomationProperties.SetName(args.ItemContainer, string.Empty);
                AutomationProperties.SetHelpText(args.ItemContainer, string.Empty);
                return;
            }

            AutomationProperties.SetName(args.ItemContainer, result.PageTitle);
            AutomationProperties.SetHelpText(args.ItemContainer, result.Location);
        }
    }
}
