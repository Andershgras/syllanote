using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Domain;

[TestClass]
public class PageTests
{
    [TestMethod]
    public void Constructor_WithValidData_CreatesPage()
    {
        // Arrange
        var sectionId = Guid.NewGuid();

        // Act
        var page = new Page(
            sectionId,
            "Confusion Matrix");

        // Assert
        Assert.AreNotEqual(Guid.Empty, page.Id);
        Assert.AreEqual(sectionId, page.SectionId);
        Assert.AreEqual("Confusion Matrix", page.Title);
        Assert.AreEqual(string.Empty, page.Content);
        Assert.AreEqual(string.Empty, page.FormattedContent);
        Assert.AreEqual(page.CreatedAt, page.UpdatedAt);
    }
    [DataTestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    public void Constructor_WithInvalidTitle_ThrowsArgumentException(
    string title)
    {
        var sectionId = Guid.NewGuid();

        Assert.ThrowsException<ArgumentException>(
            () => new Page(sectionId, title));
    }
    [TestMethod]
    public void Constructor_WithEmptySectionId_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(
            () => new Page(
                Guid.Empty,
                "Confusion Matrix"));
    }
    [TestMethod]
    public void UpdateContent_ShouldUpdateContent()
    {
        // Arrange
        var page = new Page(
            Guid.NewGuid(),
            "Confusion Matrix");

        // Act
        page.UpdateContent(
            "A confusion matrix contains TP, TN, FP and FN.",
            @"{\rtf1 A confusion matrix contains \b TP\b0, TN, FP and FN.}");

        // Assert
        Assert.AreEqual(
            "A confusion matrix contains TP, TN, FP and FN.",
            page.Content);
        Assert.AreEqual(
            @"{\rtf1 A confusion matrix contains \b TP\b0, TN, FP and FN.}",
            page.FormattedContent);
    }
    [TestMethod]
    public void UpdateContent_ShouldUpdateUpdatedAt()
    {
        // Arrange
        var page = new Page(
            Guid.NewGuid(),
            "Confusion Matrix");

        var originalUpdatedAt = page.UpdatedAt;

        // Act
        page.UpdateContent(
            "A confusion matrix contains TP, TN, FP and FN.",
            @"{\rtf1 A confusion matrix contains TP, TN, FP and FN.}");

        // Assert
        Assert.IsTrue(
            page.UpdatedAt >= originalUpdatedAt);
    }

    [TestMethod]
    public void Rename_WithValidTitle_UpdatesTitleAndUpdatedAt()
    {
        var page = new Page(Guid.NewGuid(), "Old title");
        page.UpdateContent("Existing note", @"{\rtf1 Existing note}");
        var originalUpdatedAt = page.UpdatedAt;

        page.Rename("New title");

        Assert.AreEqual("New title", page.Title);
        Assert.AreEqual("Existing note", page.Content);
        Assert.AreEqual(@"{\rtf1 Existing note}", page.FormattedContent);
        Assert.IsTrue(page.UpdatedAt >= originalUpdatedAt);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    public void Rename_WithInvalidTitle_ThrowsArgumentException(string title)
    {
        var page = new Page(Guid.NewGuid(), "Original title");

        Assert.ThrowsException<ArgumentException>(() => page.Rename(title));

        Assert.AreEqual("Original title", page.Title);
    }
}
