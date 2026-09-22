namespace Syllanote.Domain.Entities;

public class Section
{
    public Guid Id { get; private set; }

    public Guid NotebookId { get; private set; }

    public string Name { get; private set; }

    public int SortOrder { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Section(Guid notebookId, string name, int sortOrder = 0)
    {
        if (notebookId == Guid.Empty)
        {
            throw new ArgumentException(
                "Notebook id cannot be empty.",
                nameof(notebookId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Section name cannot be empty.",
                nameof(name));
        }

        if (sortOrder < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sortOrder),
                "Sort order cannot be negative.");
        }

        Id = Guid.NewGuid();
        NotebookId = notebookId;
        Name = name;
        SortOrder = sortOrder;
        CreatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Section name cannot be empty.",
                nameof(name));
        }

        Name = name;
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
