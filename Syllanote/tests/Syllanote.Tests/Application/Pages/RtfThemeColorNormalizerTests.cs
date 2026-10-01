using Syllanote.Application.Notebooks.Sections.Pages.Formatting;

namespace Syllanote.Tests.Application.Pages;

[TestClass]
public sealed class RtfThemeColorNormalizerTests
{
    [TestMethod]
    public void Normalize_ReplacesEditorColorsWithAutomaticColors()
    {
        const string rtf =
            @"{\rtf1{\colortbl ;\red255\green255\blue255;}\cf1\highlight1\b Text\b0}";

        var normalized = RtfThemeColorNormalizer.Normalize(rtf);

        Assert.AreEqual(
            @"{\rtf1{\colortbl ;\red255\green255\blue255;}\cf0\highlight0\b Text\b0}",
            normalized);
    }

    [TestMethod]
    public void Normalize_PreservesEscapedTextAndOtherFormatting()
    {
        const string rtf = @"{\rtf1 Literal \\cf1 text\par}";

        var normalized = RtfThemeColorNormalizer.Normalize(rtf);

        Assert.AreEqual(rtf, normalized);
    }
}
