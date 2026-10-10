using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Syllanote.Domain.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Syllanote.Desktop.Controls
{
    public delegate void ConceptDictionaryRequestedEventHandler(
        object sender,
        Notebook notebook);

    public sealed partial class WorkspaceMenuBar : UserControl
    {
        private bool _isInteractionEnabled = true;

        public event ConceptDictionaryRequestedEventHandler?
            ConceptDictionaryRequested;

        public WorkspaceMenuBar()
        {
            InitializeComponent();
        }

        public void SetInteractionEnabled(bool isEnabled)
        {
            _isInteractionEnabled = isEnabled;
            ConceptsButton.IsEnabled = isEnabled;
        }

        public void UpdateState(
            IEnumerable<Notebook> notebooks,
            Notebook? notebook,
            bool isConceptDictionaryOpen)
        {
            ConceptsButton.IsEnabled = _isInteractionEnabled;
            ConceptsSelectionIndicator.Visibility = isConceptDictionaryOpen
                ? Visibility.Visible
                : Visibility.Collapsed;

            AutomationProperties.SetItemStatus(
                ConceptsButton,
                isConceptDictionaryOpen && notebook is not null
                    ? $"Concept Dictionary open for {notebook.Name}"
                    : string.Empty);

            RebuildNotebookChoices(notebooks, notebook);
        }

        private void RebuildNotebookChoices(
            IEnumerable<Notebook> notebooks,
            Notebook? selectedNotebook)
        {
            ConceptsFlyout.Items.Clear();
            var availableNotebooks = notebooks.ToList();
            if (availableNotebooks.Count == 0)
            {
                ConceptsFlyout.Items.Add(new MenuFlyoutItem
                {
                    Text = "Create a notebook first",
                    IsEnabled = false
                });
                return;
            }

            ConceptsFlyout.Items.Add(new MenuFlyoutItem
            {
                Text = "Choose a notebook",
                IsEnabled = false
            });
            ConceptsFlyout.Items.Add(new MenuFlyoutSeparator());

            foreach (var notebook in availableNotebooks)
            {
                var item = new ToggleMenuFlyoutItem
                {
                    Text = notebook.Name,
                    Tag = notebook,
                    IsChecked = notebook.Id == selectedNotebook?.Id
                };
                AutomationProperties.SetHelpText(
                    item,
                    $"Open the Concept Dictionary for {notebook.Name}");
                item.Click += NotebookConceptsMenuItem_Click;
                ConceptsFlyout.Items.Add(item);
            }
        }

        private void NotebookConceptsMenuItem_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (sender is ToggleMenuFlyoutItem { Tag: Notebook notebook })
            {
                ConceptDictionaryRequested?.Invoke(sender, notebook);
            }
        }

        private void ConceptsKeyboardAccelerator_Invoked(
            KeyboardAccelerator sender,
            KeyboardAcceleratorInvokedEventArgs e)
        {
            if (!ConceptsButton.IsEnabled)
            {
                return;
            }

            e.Handled = true;
            ConceptsFlyout.ShowAt(ConceptsButton);
        }
    }
}
