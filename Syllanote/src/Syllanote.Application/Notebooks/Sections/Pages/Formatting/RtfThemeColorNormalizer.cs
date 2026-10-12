using System.Text.RegularExpressions;

namespace Syllanote.Application.Notebooks.Sections.Pages.Formatting;

public static partial class RtfThemeColorNormalizer
{
    private const string ColorAwareStoragePrefix =
        "syllanote:rtf-colors-v1\n";

    public static string PrepareForDisplay(string storedRtf)
    {
        if (string.IsNullOrEmpty(storedRtf))
        {
            return storedRtf;
        }

        return storedRtf.StartsWith(
            ColorAwareStoragePrefix,
            StringComparison.Ordinal)
                ? storedRtf[ColorAwareStoragePrefix.Length..]
                : Normalize(storedRtf);
    }

    public static string PrepareForDisplay(
        string storedRtf,
        byte themeRed,
        byte themeGreen,
        byte themeBlue)
    {
        var rtf = PrepareForDisplay(storedRtf);
        if (string.IsNullOrEmpty(rtf))
        {
            return rtf;
        }

        var colorTableMatch = ColorTableRegex().Match(rtf);
        if (!colorTableMatch.Success)
        {
            return rtf;
        }

        var colorTableEntries = colorTableMatch.Groups["entries"].Value;
        var themeColorIndex = FindColorIndex(
            colorTableEntries,
            themeRed,
            themeGreen,
            themeBlue);
        if (themeColorIndex < 0)
        {
            var prefix = colorTableEntries.Length == 0 ? ";" : string.Empty;
            var themeColorEntry =
                $@"{prefix}\red{themeRed}\green{themeGreen}\blue{themeBlue};";
            themeColorIndex = colorTableEntries.Count(character => character == ';');
            if (colorTableEntries.Length == 0)
            {
                themeColorIndex = 1;
            }

            var colorTable = colorTableMatch.Value;
            var expandedColorTable =
                colorTable[..^1] + themeColorEntry + "}";
            rtf = rtf.Remove(
                    colorTableMatch.Index,
                    colorTableMatch.Length)
                .Insert(colorTableMatch.Index, expandedColorTable);
        }

        return AutomaticForegroundColorRegex().Replace(
            rtf,
            $@"\cf{themeColorIndex}");
    }

    public static string PrepareForStorage(string rtf)
    {
        if (string.IsNullOrEmpty(rtf) ||
            rtf.StartsWith(ColorAwareStoragePrefix, StringComparison.Ordinal))
        {
            return rtf;
        }

        return ColorAwareStoragePrefix + rtf;
    }

    public static string PrepareForStorage(
        string rtf,
        byte themeRed,
        byte themeGreen,
        byte themeBlue)
    {
        if (string.IsNullOrEmpty(rtf))
        {
            return rtf;
        }

        var colorTableMatch = ColorTableRegex().Match(rtf);
        if (colorTableMatch.Success)
        {
            var themeColorIndex = FindColorIndex(
                colorTableMatch.Groups["entries"].Value,
                themeRed,
                themeGreen,
                themeBlue);
            if (themeColorIndex >= 0)
            {
                rtf = Regex.Replace(
                    rtf,
                    $@"(?<!\\)\\cf{themeColorIndex}(?!\d)",
                    @"\cf0");
            }
        }

        return PrepareForStorage(rtf);
    }

    private static int FindColorIndex(
        string colorTableEntries,
        byte red,
        byte green,
        byte blue)
    {
        foreach (Match match in ColorEntryRegex().Matches(colorTableEntries))
        {
            if (!byte.TryParse(match.Groups["red"].Value, out var entryRed) ||
                !byte.TryParse(match.Groups["green"].Value, out var entryGreen) ||
                !byte.TryParse(match.Groups["blue"].Value, out var entryBlue) ||
                entryRed != red ||
                entryGreen != green ||
                entryBlue != blue)
            {
                continue;
            }

            return colorTableEntries[..match.Index]
                .Count(character => character == ';');
        }

        return -1;
    }

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

    [GeneratedRegex(@"\{\\colortbl(?<entries>[^{}]*)\}")]
    private static partial Regex ColorTableRegex();

    [GeneratedRegex(
        @"\\red(?<red>\d+)\\green(?<green>\d+)\\blue(?<blue>\d+);")]
    private static partial Regex ColorEntryRegex();

    [GeneratedRegex(@"(?<!\\)\\cf0(?!\d)")]
    private static partial Regex AutomaticForegroundColorRegex();
}
