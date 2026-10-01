using System.Text.RegularExpressions;

namespace Syllanote.Application.Notebooks.Sections.Pages.Formatting;

public static partial class RtfThemeColorNormalizer
{
    public static string Normalize(string rtf)
    {
        if (string.IsNullOrEmpty(rtf))
        {
            return rtf;
        }

        var normalized = ForegroundColorRegex().Replace(rtf, @"\cf0");
        return HighlightColorRegex().Replace(normalized, @"\highlight0");
    }

    [GeneratedRegex(@"(?<!\\)\\cf\d+")]
    private static partial Regex ForegroundColorRegex();

    [GeneratedRegex(@"(?<!\\)\\highlight\d+")]
    private static partial Regex HighlightColorRegex();
}
