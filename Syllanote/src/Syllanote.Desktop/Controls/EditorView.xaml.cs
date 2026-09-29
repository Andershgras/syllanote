using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace Syllanote.Desktop.Controls
{
    public sealed partial class EditorView : UserControl
    {
        public event SelectionChangedEventHandler? ParagraphStyleSelectionChanged;
        public event RoutedEventHandler? BoldRequested;
        public event RoutedEventHandler? ItalicRequested;
        public event RoutedEventHandler? UnderlineRequested;
        public event RoutedEventHandler? BulletedListRequested;
        public event RoutedEventHandler? NumberedListRequested;
        public event RoutedEventHandler? ContentSelectionChanged;
        public event RoutedEventHandler? ContentTextChanged;

        public RichEditBox ContentEditor => PageContentRichEditBox;
        public ComboBox ParagraphStyleSelector => ParagraphStyleComboBox;
        public ToggleButton BoldToggle => BoldButton;
        public ToggleButton ItalicToggle => ItalicButton;
        public ToggleButton UnderlineToggle => UnderlineButton;
        public ToggleButton BulletedListToggle => BulletedListButton;
        public ToggleButton NumberedListToggle => NumberedListButton;

        public EditorView()
        {
            InitializeComponent();
            ConceptDefinitionFlyout.OverlayInputPassThroughElement =
                PageContentRichEditBox;
        }

        public void UpdatePageState(bool hasPage)
        {
            EditorPageTitle.Visibility = hasPage
                ? Visibility.Visible
                : Visibility.Collapsed;
            FormattingToolbar.Visibility = hasPage
                ? Visibility.Visible
                : Visibility.Collapsed;
            PageContentRichEditBox.Visibility = hasPage
                ? Visibility.Visible
                : Visibility.Collapsed;
            EditorEmptyState.Visibility = hasPage
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public void ShowConceptDefinition(
            string name,
            string definition,
            Point position)
        {
            ConceptDefinitionNameTextBlock.Text = name;
            ConceptDefinitionTextBlock.Text = definition;
            ConceptDefinitionFlyout.ShowAt(
                PageContentRichEditBox,
                new FlyoutShowOptions
                {
                    Placement = FlyoutPlacementMode.Bottom,
                    Position = position,
                    ShowMode = FlyoutShowMode.Transient
                });
        }

        public void HideConceptDefinition()
        {
            if (ConceptDefinitionFlyout.IsOpen)
            {
                ConceptDefinitionFlyout.Hide();
            }
        }

        private void ParagraphStyleComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e) =>
            ParagraphStyleSelectionChanged?.Invoke(sender, e);

        private void BoldButton_Click(object sender, RoutedEventArgs e) =>
            BoldRequested?.Invoke(sender, e);

        private void ItalicButton_Click(object sender, RoutedEventArgs e) =>
            ItalicRequested?.Invoke(sender, e);

        private void UnderlineButton_Click(object sender, RoutedEventArgs e) =>
            UnderlineRequested?.Invoke(sender, e);

        private void BulletedListButton_Click(object sender, RoutedEventArgs e) =>
            BulletedListRequested?.Invoke(sender, e);

        private void NumberedListButton_Click(object sender, RoutedEventArgs e) =>
            NumberedListRequested?.Invoke(sender, e);

        private void PageContentRichEditBox_SelectionChanged(
            object sender,
            RoutedEventArgs e) =>
            ContentSelectionChanged?.Invoke(sender, e);

        private void PageContentRichEditBox_TextChanged(
            object sender,
            RoutedEventArgs e) =>
            ContentTextChanged?.Invoke(sender, e);
    }
}
