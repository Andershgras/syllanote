using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using DomainPage = Syllanote.Domain.Entities.Page;

namespace Syllanote.Desktop.Controls
{
    public delegate void PageContextMenuOpeningEventHandler(
        object sender,
        object e);

    public sealed partial class PageSidebar : UserControl
    {
        public event RoutedEventHandler? NewPageRequested;
        public event SelectionChangedEventHandler? PageSelectionChanged;
        public event PageContextMenuOpeningEventHandler? PageContextMenuOpening;
        public event RoutedEventHandler? MovePageUpRequested;
        public event RoutedEventHandler? MovePageDownRequested;
        public event RoutedEventHandler? RenamePageRequested;
        public event RoutedEventHandler? DeletePageRequested;

        public DomainPage? SelectedPage => PagesListView.SelectedItem as DomainPage;

        public PageSidebar()
        {
            InitializeComponent();
        }

        public void UpdateState(bool hasSection, bool hasPages)
        {
            NewPageButton.IsEnabled = hasSection;
            PageEmptyStateIcon.Glyph = hasSection ? "\uE8A5" : "\uE8B7";
            PageEmptyStateTitle.Text = hasSection
                ? "No pages yet"
                : "No section selected";
            PageEmptyStateDescription.Text = hasSection
                ? "Create a page to get started."
                : "Select a section to see its pages.";
            PageEmptyState.Visibility = !hasSection || !hasPages
                ? Visibility.Visible
                : Visibility.Collapsed;

            var newPageHelpText = hasSection
                ? "New page"
                : "Select a section before creating a page.";
            ToolTipService.SetToolTip(NewPageButton, newPageHelpText);
            AutomationProperties.SetHelpText(NewPageButton, newPageHelpText);
        }

        public void SetPageListEnabled(bool isEnabled)
        {
            PagesListView.IsEnabled = isEnabled;
        }

        public void SelectPage(DomainPage? page)
        {
            PagesListView.SelectedItem = page;
        }

        private void NewPageButton_Click(object sender, RoutedEventArgs e)
        {
            NewPageRequested?.Invoke(this, e);
        }

        private void PagesListView_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            PageSelectionChanged?.Invoke(this, e);
        }

        private void PageContextMenu_Opening(object sender, object e)
        {
            PageContextMenuOpening?.Invoke(sender, e);
        }

        private void MovePageUpMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MovePageUpRequested?.Invoke(this, e);
        }

        private void MovePageDownMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MovePageDownRequested?.Invoke(this, e);
        }

        private void RenamePageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            RenamePageRequested?.Invoke(this, e);
        }

        private void DeletePageMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DeletePageRequested?.Invoke(this, e);
        }
    }
}
