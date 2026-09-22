using System.Text;

namespace Syllanote.Domain.Entities;

public class Concept
{
    public Guid Id { get; private set; }

    public Guid NotebookId { get; private set; }

    public string Name { get; private set; }

    public string NormalizedName { get; private set; }

    public string Definition { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    public Concept(Guid notebookId, string name, string definition)
    {
        if (notebookId == Guid.Empty)
        {
            throw new ArgumentException(
                "Notebook id cannot be empty.",
                nameof(notebookId));
        }

        (Name, NormalizedName) = NormalizeNameParts(name);
        Definition = ValidateDefinition(definition);
        Id = Guid.NewGuid();
        NotebookId = notebookId;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public void Rename(string name)
    {
        (Name, NormalizedName) = NormalizeNameParts(name);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDefinition(string definition)
    {
        Definition = ValidateDefinition(definition);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string definition)
    {
        var (displayName, normalizedName) = NormalizeNameParts(name);
        var validDefinition = ValidateDefinition(definition);

        Name = displayName;
        NormalizedName = normalizedName;
        Definition = validDefinition;
        UpdatedAt = DateTime.UtcNow;
    }

    public static string NormalizeName(string name)
    {
        return NormalizeNameParts(name).NormalizedName;
    }

    private static (string Name, string NormalizedName) NormalizeNameParts(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Concept name cannot be empty.",
                nameof(name));
        }

        var displayName = name.Trim().Normalize(NormalizationForm.FormC);
        return (displayName, displayName.ToUpperInvariant());
    }

    private static string ValidateDefinition(string definition)
    {
        if (string.IsNullOrWhiteSpace(definition))
        {
            throw new ArgumentException(
                "Concept definition cannot be empty.",
                nameof(definition));
        }

        return definition.Trim();
    }
}
