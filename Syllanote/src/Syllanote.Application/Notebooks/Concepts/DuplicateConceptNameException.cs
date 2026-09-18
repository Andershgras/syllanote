namespace Syllanote.Application.Notebooks.Concepts;

public class DuplicateConceptNameException : InvalidOperationException
{
    public DuplicateConceptNameException()
        : base("A concept with this name already exists in this notebook.")
    {
    }
}
