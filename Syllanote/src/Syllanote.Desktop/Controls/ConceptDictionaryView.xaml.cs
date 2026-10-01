using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Syllanote.Application.Notebooks.Concepts.FindConceptReferences;
using Syllanote.Domain.Entities;

namespace Syllanote.Desktop.Controls
{
    public sealed partial class ConceptDictionaryView : UserControl
    {
        public event RoutedEventHandler? BackRequested;
        public event RoutedEventHandler? NewConceptRequested;
        public event SelectionChangedEventHandler? ConceptSelectionChanged;
        public event SelectionChangedEventHandler? ReferenceSelectionChanged;
        public event TextChangedEventHandler? InputChanged;
        public event RoutedEventHandler? SaveRequested;
        public event RoutedEventHandler? DeleteRequested;

        public Concept? SelectedConcept => ConceptsListView.SelectedItem as Concept;
        public ConceptReference? SelectedReference =>
            ConceptReferencesListView.SelectedItem as ConceptReference;
        public string ConceptName => ConceptNameTextBox.Text;
        public string ConceptDefinition => ConceptDefinitionTextBox.Text;

        public ConceptDictionaryView()
        {
            InitializeComponent();
        }

        public void UpdateState(
            bool isOperationInProgress,
            bool hasNotebook,
            bool hasCurrentConcept,
            bool hasConcepts,
            bool hasReferences)
        {
            SaveConceptButton.Content = hasCurrentConcept ? "Save" : "Create";
            SaveConceptButton.IsEnabled = !isOperationInProgress &&
                hasNotebook &&
                !string.IsNullOrWhiteSpace(ConceptNameTextBox.Text) &&
                !string.IsNullOrWhiteSpace(ConceptDefinitionTextBox.Text);
            DeleteConceptButton.IsEnabled = !isOperationInProgress &&
                hasCurrentConcept;
            ConceptEmptyState.Visibility = hasConcepts
                ? Visibility.Collapsed
                : Visibility.Visible;
            ConceptReferencesEmptyStateTitle.Text = hasCurrentConcept
                ? "No references yet"
                : "No concept selected";
            ConceptReferencesEmptyStateDescription.Text = hasCurrentConcept
                ? "This concept is not referenced on any pages."
                : "Select a concept to view references.";
            ConceptReferencesEmptyState.Visibility = hasReferences
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            NewConceptButton.IsEnabled = isEnabled;
            ConceptsListView.IsEnabled = isEnabled;
            ConceptReferencesListView.IsEnabled = isEnabled;
            ConceptNameTextBox.IsEnabled = isEnabled;
            ConceptDefinitionTextBox.IsEnabled = isEnabled;
        }

        public void ClearEditor()
        {
            ConceptsListView.SelectedItem = null;
            SetInput(string.Empty, string.Empty);
            Message = string.Empty;
        }

        public void SetInput(string name, string definition)
        {
            ConceptNameTextBox.Text = name;
            ConceptDefinitionTextBox.Text = definition;
        }

        public void SelectConcept(Concept? concept)
        {
            ConceptsListView.SelectedItem = concept;
        }

        public void ClearSelectedReference()
        {
            ConceptReferencesListView.SelectedItem = null;
        }

        public bool FocusConceptName() =>
            ConceptNameTextBox.Focus(FocusState.Keyboard);

        public bool FocusSelectedConceptOrNewButton()
        {
            if (SelectedConcept is not null)
            {
                ConceptsListView.ScrollIntoView(SelectedConcept);
                ConceptsListView.UpdateLayout();
                if (ConceptsListView.ContainerFromItem(SelectedConcept)
                    is ListViewItem item)
                {
                    return item.Focus(FocusState.Keyboard);
                }
            }

            return NewConceptButton.Focus(FocusState.Keyboard);
        }

        public string Message
        {
            get => ConceptMessage.Text;
            set => ConceptMessage.Text = value;
        }

        private void ConceptBackButton_Click(object sender, RoutedEventArgs e) =>
            BackRequested?.Invoke(sender, e);

        private void NewConceptButton_Click(object sender, RoutedEventArgs e) =>
            NewConceptRequested?.Invoke(sender, e);

        private void ConceptsListView_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e) =>
            ConceptSelectionChanged?.Invoke(sender, e);

        private void ConceptsListView_ContainerContentChanging(
            ListViewBase sender,
            ContainerContentChangingEventArgs args)
        {
            if (args.ItemContainer is null)
            {
                return;
            }

            AutomationProperties.SetName(
                args.ItemContainer,
                args.InRecycleQueue || args.Item is not Concept concept
                    ? string.Empty
                    : concept.Name);
        }

        private void ConceptReferencesListView_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e) =>
            ReferenceSelectionChanged?.Invoke(sender, e);

        private void ConceptReferencesListView_ContainerContentChanging(
            ListViewBase sender,
            ContainerContentChangingEventArgs args)
        {
            if (args.ItemContainer is null)
            {
                return;
            }

            if (args.InRecycleQueue || args.Item is not ConceptReference reference)
            {
                AutomationProperties.SetName(args.ItemContainer, string.Empty);
                AutomationProperties.SetHelpText(args.ItemContainer, string.Empty);
                return;
            }

            AutomationProperties.SetName(args.ItemContainer, reference.PageTitle);
            AutomationProperties.SetHelpText(args.ItemContainer, reference.SectionName);
        }

        private void ConceptInput_TextChanged(
            object sender,
            TextChangedEventArgs e) =>
            InputChanged?.Invoke(sender, e);

        private void SaveConceptButton_Click(object sender, RoutedEventArgs e) =>
            SaveRequested?.Invoke(sender, e);

        private void DeleteConceptButton_Click(object sender, RoutedEventArgs e) =>
            DeleteRequested?.Invoke(sender, e);
    }
}
