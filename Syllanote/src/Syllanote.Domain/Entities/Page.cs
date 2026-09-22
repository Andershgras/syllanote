namespace Syllanote.Domain.Entities;

public class Page
{
    public Guid Id { get; private set; }

    public Guid SectionId { get; private set; }

    public string Title { get; private set; }

    public string Content { get; private set; }

    public string FormattedContent { get; private set; }

    public int SortOrder { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public Page(Guid sectionId, string title, int sortOrder = 0)
    {
        if (sectionId == Guid.Empty)
        {
            throw new ArgumentException(
                "Section id cannot be empty.",
                nameof(sectionId));
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Page title cannot be empty.",
                nameof(title));
        }

        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Sort order cannot be negative.");
        }

        Id = Guid.NewGuid();
        SectionId = sectionId;
        Title = title;
        Content = string.Empty;
        FormattedContent = string.Empty;
        SortOrder = sortOrder;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }
    public void UpdateContent(string content, string formattedContent)
    {
        Content = content;
        FormattedContent = formattedContent;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rename(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Page title cannot be empty.",
                nameof(title));
        }

        Title = title;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeSortOrder(int sortOrder)
    {
        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Sort order cannot be negative.");
        }

        SortOrder = sortOrder;
    }
}
