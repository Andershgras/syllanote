using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Concepts.Recognition;

public class RecognizeConceptsService
{
    public IReadOnlyList<ConceptMatch> Recognize(
        string content,
        IEnumerable<Concept> concepts)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(concepts);

        if (content.Length == 0)
        {
            return [];
        }

        var candidates = new List<MatchCandidate>();

        foreach (var concept in concepts)
        {
            var searchIndex = 0;

            while (searchIndex < content.Length)
            {
                var startIndex = content.IndexOf(
                    concept.Name,
                    searchIndex,
                    StringComparison.OrdinalIgnoreCase);

                if (startIndex < 0)
                {
                    break;
                }

                var endIndex = startIndex + concept.Name.Length;
                if (HasValidBoundaries(content, startIndex, endIndex))
                {
                    candidates.Add(new MatchCandidate(
                        concept.Id,
                        concept.Name,
                        startIndex,
                        concept.Name.Length));
                }

                searchIndex = startIndex + 1;
            }
        }

        var orderedCandidates = candidates
            .OrderBy(candidate => candidate.StartIndex)
            .ThenByDescending(candidate => candidate.Length)
            .ThenBy(candidate => candidate.ConceptName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.ConceptId);

        var matches = new List<ConceptMatch>();
        var nextAvailableIndex = 0;

        foreach (var candidate in orderedCandidates)
        {
            if (candidate.StartIndex < nextAvailableIndex)
            {
                continue;
            }

            matches.Add(new ConceptMatch(
                candidate.ConceptId,
                candidate.ConceptName,
                candidate.StartIndex,
                candidate.Length));
            nextAvailableIndex = candidate.StartIndex + candidate.Length;
        }

        return matches;
    }

    private static bool HasValidBoundaries(
        string content,
        int startIndex,
        int endIndex)
    {
        var hasValidStart = startIndex == 0 ||
            !IsWordCharacter(content[startIndex - 1]);
        var hasValidEnd = endIndex == content.Length ||
            !IsWordCharacter(content[endIndex]);

        return hasValidStart && hasValidEnd;
    }

    private static bool IsWordCharacter(char character)
    {
        return char.IsLetterOrDigit(character) || character == '_';
    }

    private sealed record MatchCandidate(
        Guid ConceptId,
        string ConceptName,
        int StartIndex,
        int Length);
}
