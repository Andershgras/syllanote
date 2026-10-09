using System.Globalization;
using System.Text.RegularExpressions;

namespace Syllanote.Application.Notebooks.Sections.Pages.Formatting;

public readonly partial record struct PageTextStatistics(
    int WordCount,
    int CharacterCount)
{
    public static PageTextStatistics Calculate(string? content)
    {
        var normalized = content?.TrimEnd('\r') ?? string.Empty;
        if (normalized.Length == 0)
        {
            return new PageTextStatistics(0, 0);
        }

        return new PageTextStatistics(
            WordRegex().Matches(normalized).Count,
            StringInfo.ParseCombiningCharacters(normalized).Length);
    }

    [GeneratedRegex(
        @"[\p{L}\p{M}\p{N}]+(?:['’\-][\p{L}\p{M}\p{N}]+)*",
        RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();
}
