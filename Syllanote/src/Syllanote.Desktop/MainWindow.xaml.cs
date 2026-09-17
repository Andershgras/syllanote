using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syllanote.Desktop.ViewModels;

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
    }
}
