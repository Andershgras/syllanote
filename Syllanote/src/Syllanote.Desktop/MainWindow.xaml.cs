using Microsoft.UI.Xaml;
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
    }
}
