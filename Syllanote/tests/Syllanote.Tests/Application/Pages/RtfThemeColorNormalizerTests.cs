using Syllanote.Application.Notebooks.Sections.Pages.Formatting;

namespace Syllanote.Tests.Application.Pages;

[TestClass]
public sealed class RtfThemeColorNormalizerTests
{
    [TestMethod]
    public void PrepareForDisplay_NormalizesLegacyRtfColors()
    {
        const string rtf =
            @"{\rtf1{\colortbl ;\red255\green0\blue0;}\cf1\highlight1 Text}";

        var prepared = RtfThemeColorNormalizer.PrepareForDisplay(rtf);

        Assert.AreEqual(
            @"{\rtf1{\colortbl ;\red255\green0\blue0;}\cf0\highlight0 Text}",
            prepared);
    }

    [TestMethod]
    public void PrepareForStorageAndDisplay_PreservesColorAwareRtf()
    {
        const string rtf =
            @"{\rtf1{\colortbl ;\red255\green0\blue0;}\cf1\highlight1 Text}";

        var stored = RtfThemeColorNormalizer.PrepareForStorage(rtf);
        var prepared = RtfThemeColorNormalizer.PrepareForDisplay(stored);

        Assert.AreEqual(rtf, prepared);
        Assert.AreNotEqual(rtf, stored);
    }

    [TestMethod]
    public void PrepareForStorage_DoesNotAddThePrefixTwice()
    {
        const string rtf = @"{\rtf1 Text}";
        var stored = RtfThemeColorNormalizer.PrepareForStorage(rtf);

        var storedAgain = RtfThemeColorNormalizer.PrepareForStorage(stored);

        Assert.AreEqual(stored, storedAgain);
    }

    [TestMethod]
    public void PrepareForDisplay_AppliesThemeColorOnlyToAutomaticText()
    {
        const string rtf =
            @"{\rtf1{\colortbl ;\red231\green72\blue86;}\cf0 Default \cf1 Red}";
        var stored = RtfThemeColorNormalizer.PrepareForStorage(rtf);

        var prepared = RtfThemeColorNormalizer.PrepareForDisplay(
            stored,
            255,
            255,
            255);

        Assert.AreEqual(
            @"{\rtf1{\colortbl ;\red231\green72\blue86;\red255\green255\blue255;}\cf2 Default \cf1 Red}",
            prepared);
    }

    [TestMethod]
    public void PrepareForStorage_RestoresThemeTextToAutomaticColor()
    {
        const string displayedRtf =
            @"{\rtf1{\colortbl ;\red231\green72\blue86;\red255\green255\blue255;}\cf2 Default \cf1 Red}";

        var stored = RtfThemeColorNormalizer.PrepareForStorage(
            displayedRtf,
            255,
            255,
            255);
        var prepared = RtfThemeColorNormalizer.PrepareForDisplay(stored);

        Assert.AreEqual(
            @"{\rtf1{\colortbl ;\red231\green72\blue86;\red255\green255\blue255;}\cf0 Default \cf1 Red}",
            prepared);
    }

    [TestMethod]
    public void PrepareForDisplay_ReusesExistingThemeColorEntry()
    {
        const string rtf =
            @"{\rtf1{\colortbl ;\red255\green255\blue255;}\cf0 Text}";
        var stored = RtfThemeColorNormalizer.PrepareForStorage(rtf);

        var prepared = RtfThemeColorNormalizer.PrepareForDisplay(
            stored,
            255,
            255,
            255);

        Assert.AreEqual(
            @"{\rtf1{\colortbl ;\red255\green255\blue255;}\cf1 Text}",
            prepared);
    }

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
