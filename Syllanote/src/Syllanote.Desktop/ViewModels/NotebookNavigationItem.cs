using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Syllanote.Domain.Entities;
using System.Collections.ObjectModel;

namespace Syllanote.Desktop.ViewModels;

public partial class NotebookNavigationItem : ObservableObject
{
    private NotebookNavigationItem(Notebook? notebook, Section? section)
    {
        Notebook = notebook;
        Section = section;
    }

    public Notebook? Notebook { get; }

    public Section? Section { get; }

    public string Name => Notebook?.Name ?? Section?.Name ?? string.Empty;

    public string AccessibilityName => Notebook is not null
        ? $"{Name}, notebook"
        : $"{Name}, section";

    public string AccessibilityHelpText => Notebook is not null
        ? "Press Enter or Space to select this notebook and expand or collapse its sections. Press Shift+F10 for more actions."
        : "Press Enter or Space to select this section. Press Shift+F10 for more actions.";

    public string AccessibilityItemStatus => Notebook is not null
        ? $"{(IsActiveNotebook ? "Selected" : "Not selected")}, {(IsExpanded ? "expanded" : "collapsed")}"
        : IsSelectedSection
            ? "Selected"
            : "Not selected";

    public ObservableCollection<NotebookNavigationItem> Children { get; } = [];

    public Visibility SectionControlsVisibility =>
        Notebook is not null && IsActiveNotebook && IsExpanded
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility EmptySectionsVisibility =>
        Notebook is not null && IsActiveNotebook && IsExpanded &&
        Children.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility SelectedSectionIndicatorVisibility =>
        Section is not null && IsSelectedSection
            ? Visibility.Visible
            : Visibility.Collapsed;

    public Visibility SelectedNotebookIndicatorVisibility =>
        Notebook is not null && IsActiveNotebook
            ? Visibility.Visible
            : Visibility.Collapsed;

    public string ExpandCollapseGlyph => IsExpanded ? "\uE70D" : "\uE76C";

    public bool CanMoveUp { get; private set; }

    public bool CanMoveDown { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SectionControlsVisibility))]
    [NotifyPropertyChangedFor(nameof(EmptySectionsVisibility))]
    [NotifyPropertyChangedFor(nameof(SelectedNotebookIndicatorVisibility))]
    [NotifyPropertyChangedFor(nameof(AccessibilityItemStatus))]
    public partial bool IsActiveNotebook { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SectionControlsVisibility))]
    [NotifyPropertyChangedFor(nameof(EmptySectionsVisibility))]
    [NotifyPropertyChangedFor(nameof(ExpandCollapseGlyph))]
    [NotifyPropertyChangedFor(nameof(AccessibilityItemStatus))]
    public partial bool IsExpanded { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSectionIndicatorVisibility))]
    [NotifyPropertyChangedFor(nameof(AccessibilityItemStatus))]
    public partial bool IsSelectedSection { get; set; }

    public static NotebookNavigationItem ForNotebook(
        Notebook notebook,
        bool canMoveUp,
        bool canMoveDown)
    {
        return new NotebookNavigationItem(notebook, null)
        {
            CanMoveUp = canMoveUp,
            CanMoveDown = canMoveDown
        };
    }

    public static NotebookNavigationItem ForSection(
        Section section,
        bool canMoveUp,
        bool canMoveDown)
    {
        return new NotebookNavigationItem(null, section)
        {
            CanMoveUp = canMoveUp,
            CanMoveDown = canMoveDown
        };
    }

    public void RefreshEmptySectionsVisibility()
    {
        OnPropertyChanged(nameof(EmptySectionsVisibility));
    }
}
