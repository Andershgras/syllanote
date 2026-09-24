namespace Syllanote.Application.Notebooks.Concepts.FindConceptReferences;

public record ConceptReference(
    Guid PageId,
    string PageTitle,
    Guid SectionId,
    string SectionName);
