namespace Syllanote.Domain.Entities;

public static class SectionColorKeys
{
    public const string Red = "red";
    public const string Orange = "orange";
    public const string Yellow = "yellow";
    public const string Green = "green";
    public const string Teal = "teal";
    public const string Blue = "blue";
    public const string Purple = "purple";
    public const string Pink = "pink";

    private static readonly HashSet<string> SupportedKeys =
        new(StringComparer.Ordinal)
        {
            Red,
            Orange,
            Yellow,
            Green,
            Teal,
            Blue,
            Purple,
            Pink
        };

    public static bool IsSupported(string? colorKey) =>
        colorKey is not null && SupportedKeys.Contains(colorKey);
}
