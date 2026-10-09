using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
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
        : $"Press Enter or Space to select this section. Press Shift+F10 for more actions.{SectionColorAccessibilityText}";

    public Visibility SectionColorMarkerVisibility =>
        SectionColorBrush is not null
            ? Visibility.Visible
            : Visibility.Collapsed;

    public SolidColorBrush? SectionColorBrush => Section?.ColorKey switch
    {
        SectionColorKeys.Red => new(ColorHelper.FromArgb(255, 232, 72, 86)),
        SectionColorKeys.Orange => new(ColorHelper.FromArgb(255, 247, 99, 12)),
        SectionColorKeys.Yellow => new(ColorHelper.FromArgb(255, 255, 185, 0)),
        SectionColorKeys.Green => new(ColorHelper.FromArgb(255, 16, 124, 16)),
        SectionColorKeys.Teal => new(ColorHelper.FromArgb(255, 0, 183, 195)),
        SectionColorKeys.Blue => new(ColorHelper.FromArgb(255, 0, 120, 212)),
        SectionColorKeys.Purple => new(ColorHelper.FromArgb(255, 136, 108, 228)),
        SectionColorKeys.Pink => new(ColorHelper.FromArgb(255, 227, 0, 140)),
        _ => null
    };

    private string SectionColorAccessibilityText => Section?.ColorKey switch
    {
        SectionColorKeys.Red => " Color: Red.",
        SectionColorKeys.Orange => " Color: Orange.",
        SectionColorKeys.Yellow => " Color: Yellow.",
        SectionColorKeys.Green => " Color: Green.",
        SectionColorKeys.Teal => " Color: Teal.",
        SectionColorKeys.Blue => " Color: Blue.",
        SectionColorKeys.Purple => " Color: Purple.",
        SectionColorKeys.Pink => " Color: Pink.",
        _ => string.Empty
    };

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
