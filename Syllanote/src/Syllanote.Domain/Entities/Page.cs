namespace Syllanote.Domain.Entities;

public class Page
{
    public Guid Id { get; private set; }

    public Guid SectionId { get; private set; }

    public string Title { get; private set; }

    public string Content { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public Page(Guid sectionId, string title)
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

        Id = Guid.NewGuid();
        SectionId = sectionId;
        Title = title;
        Content = string.Empty;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }
}