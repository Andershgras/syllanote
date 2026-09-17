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
            "A confusion matrix contains TP, TN, FP and FN.");

        // Assert
        Assert.AreEqual(
            "A confusion matrix contains TP, TN, FP and FN.",
            page.Content);
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
            "A confusion matrix contains TP, TN, FP and FN.");

        // Assert
        Assert.IsTrue(
            page.UpdatedAt >= originalUpdatedAt);
    }
}