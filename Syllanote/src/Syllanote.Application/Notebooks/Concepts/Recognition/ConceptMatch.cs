namespace Syllanote.Application.Notebooks.Concepts.Recognition;

public sealed record ConceptMatch(
    Guid ConceptId,
    string ConceptName,
    int StartIndex,
    int Length);
