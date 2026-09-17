namespace Syllanote.Application.Notebooks.Sections.Pages.SearchPages;

public record SearchPageResult(
    Guid PageId,
    string PageTitle,
    Guid SectionId,
    string SectionName,
    Guid NotebookId,
    string NotebookName)
{
    public string Location => $"{NotebookName} / {SectionName}";
}
