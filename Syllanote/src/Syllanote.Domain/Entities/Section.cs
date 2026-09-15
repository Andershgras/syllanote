namespace Syllanote.Domain.Entities;

public class Section
{
    public Guid Id { get; private set; }

    public Guid NotebookId { get; private set; }

    public string Name { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public Section(Guid notebookId, string name)
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

        Id = Guid.NewGuid();
        NotebookId = notebookId;
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }
}