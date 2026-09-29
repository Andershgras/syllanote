using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Syllanote.Desktop.ViewModels;
using System.Collections.ObjectModel;

namespace Syllanote.Desktop.Controls
{
    public delegate void NavigationContextMenuOpeningEventHandler(
        object sender,
        object e);

    public sealed partial class NotebookSidebar : UserControl
    {
        public event RoutedEventHandler? NewNotebookRequested;
        public event RoutedEventHandler? NotebookExpandCollapseRequested;
        public event NavigationContextMenuOpeningEventHandler? NotebookContextMenuOpening;
        public event RoutedEventHandler? MoveNotebookUpRequested;
        public event RoutedEventHandler? MoveNotebookDownRequested;
        public event RoutedEventHandler? ConceptDictionaryRequested;
        public event RoutedEventHandler? RenameNotebookRequested;
        public event RoutedEventHandler? DeleteNotebookRequested;
        public event RoutedEventHandler? SectionNavigationRequested;
        public event NavigationContextMenuOpeningEventHandler? SectionContextMenuOpening;
        public event RoutedEventHandler? MoveSectionUpRequested;
        public event RoutedEventHandler? MoveSectionDownRequested;
        public event RoutedEventHandler? RenameSectionRequested;
        public event RoutedEventHandler? DeleteSectionRequested;
        public event RoutedEventHandler? NewSectionRequested;

        public NotebookSidebar()
        {
            InitializeComponent();
        }

        public void SetItemsSource(
            ObservableCollection<NotebookNavigationItem> navigationItems)
        {
            NotebookItemsControl.ItemsSource = navigationItems;
        }

        public void SetNavigationEnabled(bool isEnabled)
        {
            NotebookItemsControl.IsEnabled = isEnabled;
        }

        public void UpdateEmptyState(bool hasNotebooks)
        {
            NotebookEmptyState.Visibility = hasNotebooks
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void NewNotebookButton_Click(object sender, RoutedEventArgs e) =>
            NewNotebookRequested?.Invoke(sender, e);

        private void NotebookExpandCollapseButton_Click(object sender, RoutedEventArgs e) =>
            NotebookExpandCollapseRequested?.Invoke(sender, e);

        private void NotebookContextMenu_Opening(object sender, object e) =>
            NotebookContextMenuOpening?.Invoke(sender, e);

        private void MoveNotebookUpMenuItem_Click(object sender, RoutedEventArgs e) =>
            MoveNotebookUpRequested?.Invoke(sender, e);

        private void MoveNotebookDownMenuItem_Click(object sender, RoutedEventArgs e) =>
            MoveNotebookDownRequested?.Invoke(sender, e);

        private void ConceptDictionaryMenuItem_Click(object sender, RoutedEventArgs e) =>
            ConceptDictionaryRequested?.Invoke(sender, e);

        private void RenameNotebookMenuItem_Click(object sender, RoutedEventArgs e) =>
            RenameNotebookRequested?.Invoke(sender, e);

        private void DeleteNotebookButton_Click(object sender, RoutedEventArgs e) =>
            DeleteNotebookRequested?.Invoke(sender, e);

        private void SectionNavigationButton_Click(object sender, RoutedEventArgs e) =>
            SectionNavigationRequested?.Invoke(sender, e);

        private void SectionContextMenu_Opening(object sender, object e) =>
            SectionContextMenuOpening?.Invoke(sender, e);

        private void MoveSectionUpMenuItem_Click(object sender, RoutedEventArgs e) =>
            MoveSectionUpRequested?.Invoke(sender, e);

        private void MoveSectionDownMenuItem_Click(object sender, RoutedEventArgs e) =>
            MoveSectionDownRequested?.Invoke(sender, e);

        private void RenameSectionMenuItem_Click(object sender, RoutedEventArgs e) =>
            RenameSectionRequested?.Invoke(sender, e);

        private void DeleteSectionButton_Click(object sender, RoutedEventArgs e) =>
            DeleteSectionRequested?.Invoke(sender, e);

        private void NewSectionButton_Click(object sender, RoutedEventArgs e) =>
            NewSectionRequested?.Invoke(sender, e);
    }
}
