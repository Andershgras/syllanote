using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace Syllanote.Desktop.Controls
{
    public sealed partial class EditorView : UserControl
    {
        public event SelectionChangedEventHandler? ParagraphStyleSelectionChanged;
        public event SelectionChangedEventHandler? FontSizeSelectionChanged;
        public event RoutedEventHandler? BoldRequested;
        public event RoutedEventHandler? ItalicRequested;
        public event RoutedEventHandler? UnderlineRequested;
        public event RoutedEventHandler? StrikethroughRequested;
        public event RoutedEventHandler? BulletedListRequested;
        public event RoutedEventHandler? NumberedListRequested;
        public event RoutedEventHandler? AlignLeftRequested;
        public event RoutedEventHandler? AlignCenterRequested;
        public event RoutedEventHandler? AlignRightRequested;
        public event RoutedEventHandler? DecreaseIndentRequested;
        public event RoutedEventHandler? IncreaseIndentRequested;
        public event RoutedEventHandler? ClearFormattingRequested;
        public event RoutedEventHandler? TextColorRequested;
        public event RoutedEventHandler? HighlightColorRequested;
        public event RoutedEventHandler? ContentSelectionChanged;
        public event RoutedEventHandler? ContentTextChanged;

        public RichEditBox ContentEditor => PageContentRichEditBox;
        public ComboBox ParagraphStyleSelector => ParagraphStyleComboBox;
        public ComboBox FontSizeSelector => FontSizeComboBox;
        public ToggleButton BoldToggle => BoldButton;
        public ToggleButton ItalicToggle => ItalicButton;
        public ToggleButton UnderlineToggle => UnderlineButton;
        public ToggleButton StrikethroughToggle => StrikethroughButton;
        public ToggleButton BulletedListToggle => BulletedListButton;
        public ToggleButton NumberedListToggle => NumberedListButton;
        public ToggleButton AlignLeftToggle => AlignLeftButton;
        public ToggleButton AlignCenterToggle => AlignCenterButton;
        public ToggleButton AlignRightToggle => AlignRightButton;
        public AppBarButton DecreaseIndentAction => DecreaseIndentButton;

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
            PageStatisticsTextBlock.Visibility = hasPage
                ? Visibility.Visible
                : Visibility.Collapsed;
            EditorEmptyState.Visibility = hasPage
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public void UpdateStatistics(int wordCount, int characterCount)
        {
            PageStatisticsTextBlock.Text =
                $"{wordCount} {(wordCount == 1 ? "word" : "words")} · " +
                $"{characterCount} {(characterCount == 1 ? "character" : "characters")}";
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

        private void FontSizeComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e) =>
            FontSizeSelectionChanged?.Invoke(sender, e);

        private void BoldButton_Click(object sender, RoutedEventArgs e) =>
            BoldRequested?.Invoke(sender, e);

        private void ItalicButton_Click(object sender, RoutedEventArgs e) =>
            ItalicRequested?.Invoke(sender, e);

        private void UnderlineButton_Click(object sender, RoutedEventArgs e) =>
            UnderlineRequested?.Invoke(sender, e);

        private void StrikethroughButton_Click(object sender, RoutedEventArgs e) =>
            StrikethroughRequested?.Invoke(sender, e);

        private void BulletedListButton_Click(object sender, RoutedEventArgs e) =>
            BulletedListRequested?.Invoke(sender, e);

        private void NumberedListButton_Click(object sender, RoutedEventArgs e) =>
            NumberedListRequested?.Invoke(sender, e);

        private void AlignLeftButton_Click(object sender, RoutedEventArgs e) =>
            AlignLeftRequested?.Invoke(sender, e);

        private void AlignCenterButton_Click(object sender, RoutedEventArgs e) =>
            AlignCenterRequested?.Invoke(sender, e);

        private void AlignRightButton_Click(object sender, RoutedEventArgs e) =>
            AlignRightRequested?.Invoke(sender, e);

        private void DecreaseIndentButton_Click(object sender, RoutedEventArgs e) =>
            DecreaseIndentRequested?.Invoke(sender, e);

        private void IncreaseIndentButton_Click(object sender, RoutedEventArgs e) =>
            IncreaseIndentRequested?.Invoke(sender, e);

        private void ClearFormattingButton_Click(object sender, RoutedEventArgs e) =>
            ClearFormattingRequested?.Invoke(sender, e);

        private void TextColorMenuFlyoutItem_Click(object sender, RoutedEventArgs e) =>
            TextColorRequested?.Invoke(sender, e);

        private void HighlightColorMenuFlyoutItem_Click(object sender, RoutedEventArgs e) =>
            HighlightColorRequested?.Invoke(sender, e);

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
