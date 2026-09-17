using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syllanote.Desktop.ViewModels;
using System;

namespace Syllanote.Desktop
{
    public sealed partial class MainWindow : Window
    {
        public NotebookViewModel ViewModel { get; }
        public MainWindow(NotebookViewModel viewModel)
        {
            InitializeComponent();

            ViewModel = viewModel;
        }
        private async void RootGrid_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            await ViewModel.LoadNotebooksCommand.ExecuteAsync(null);
        }
        private async void NotebookListView_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            await ViewModel.LoadSectionsCommand.ExecuteAsync(null);
        }
        private async void SectionListView_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            await ViewModel.LoadPagesCommand.ExecuteAsync(null);
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
            if (sender is not ListView listView)
            {
                return;
            }

            var page =
                listView.SelectedItem as Syllanote.Domain.Entities.Page;

            await ViewModel.SelectPageAsync(page);
        }

        private async void DeletePageButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            var page = ViewModel.SelectedPage;
            if (page is null)
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
                ViewModel.SelectedPage == page)
            {
                await ViewModel.DeletePageCommand.ExecuteAsync(null);
            }
        }
    }
}
