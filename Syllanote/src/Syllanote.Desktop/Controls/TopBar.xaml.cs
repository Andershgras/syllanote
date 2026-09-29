using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Syllanote.Application.Notebooks.Sections.Pages.SearchPages;
using Windows.System;

namespace Syllanote.Desktop.Controls
{
    public sealed partial class TopBar : UserControl
    {
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
        }

        public void ClearSelectedSearchResult()
        {
            SearchResultsListView.SelectedItem = null;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchRequested?.Invoke(this, e);
        }

        private void SearchTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter)
            {
                return;
            }

            e.Handled = true;
            SearchRequested?.Invoke(this, e);
        }

        private void SearchResultsListView_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            SearchResultSelectionChanged?.Invoke(this, e);
        }
    }
}
