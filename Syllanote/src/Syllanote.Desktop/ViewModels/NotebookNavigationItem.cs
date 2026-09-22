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

    public string ExpandCollapseGlyph => IsExpanded ? "▾" : "▸";

    public bool CanMoveUp { get; private set; }

    public bool CanMoveDown { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SectionControlsVisibility))]
    [NotifyPropertyChangedFor(nameof(EmptySectionsVisibility))]
    [NotifyPropertyChangedFor(nameof(SelectedNotebookIndicatorVisibility))]
    private bool _isActiveNotebook;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SectionControlsVisibility))]
    [NotifyPropertyChangedFor(nameof(EmptySectionsVisibility))]
    [NotifyPropertyChangedFor(nameof(ExpandCollapseGlyph))]
    private bool _isExpanded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedSectionIndicatorVisibility))]
    private bool _isSelectedSection;

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

    public static NotebookNavigationItem ForSection(Section section)
    {
        return new NotebookNavigationItem(null, section);
    }

    public void RefreshEmptySectionsVisibility()
    {
        OnPropertyChanged(nameof(EmptySectionsVisibility));
    }
}
