using Syllanote.Application.Notebooks.Sections.Pages.Formatting;

namespace Syllanote.Tests.Application.Pages;

[TestClass]
public sealed class PageTextStatisticsTests
{
    [DataTestMethod]
    [DataRow(null, 0, 0)]
    [DataRow("", 0, 0)]
    [DataRow("\r", 0, 0)]
    [DataRow("One word", 2, 8)]
    [DataRow("  two\r\nlines\r", 2, 11)]
    [DataRow("don't re-enter", 2, 14)]
    public void Calculate_ReturnsExpectedCounts(
        string? content,
        int expectedWords,
        int expectedCharacters)
    {
        var result = PageTextStatistics.Calculate(content);

        Assert.AreEqual(expectedWords, result.WordCount);
        Assert.AreEqual(expectedCharacters, result.CharacterCount);
    }

    [TestMethod]
    public void Calculate_CountsCombiningSequenceAsOneCharacter()
    {
        var result = PageTextStatistics.Calculate("e\u0301");

        Assert.AreEqual(1, result.WordCount);
        Assert.AreEqual(1, result.CharacterCount);
    }
}
