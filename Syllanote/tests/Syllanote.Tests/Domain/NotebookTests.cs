using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Domain;

[TestClass]
public class NotebookTests
{
    [TestMethod]
    public void Constructor_WithValidName_CreatesNotebook()
    {
        // Arrange
        var name = "My Notebook";

        // Act
        var notebook = new Notebook(name);

        // Assert
        Assert.AreEqual(name, notebook.Name);
        Assert.AreNotEqual(Guid.Empty, notebook.Id);
    }

    [TestMethod]
    public void Constructor_WithValidName_SetsCreatedAt()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var notebook = new Notebook("My Notebook");

        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.IsTrue(notebook.CreatedAt >= beforeCreation);
        Assert.IsTrue(notebook.CreatedAt <= afterCreation);
    }

    [TestMethod]
    public void Constructor_WithSortOrder_SetsSortOrder()
    {
        var notebook = new Notebook("My Notebook", 2);

        Assert.AreEqual(2, notebook.SortOrder);
    }

    [TestMethod]
    public void ChangeSortOrder_WithValidValue_UpdatesSortOrder()
    {
        var notebook = new Notebook("My Notebook");

        notebook.ChangeSortOrder(3);

        Assert.AreEqual(3, notebook.SortOrder);
    }

    [TestMethod]
    public void ChangeSortOrder_WithNegativeValue_ThrowsArgumentOutOfRangeException()
    {
        var notebook = new Notebook("My Notebook");

        Assert.ThrowsException<ArgumentOutOfRangeException>(
            () => notebook.ChangeSortOrder(-1));
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string name)
    {
        Assert.ThrowsException<ArgumentException>(() => new Notebook(name));
    }

    [TestMethod]
    public void Rename_WithValidName_UpdatesName()
    {
        var notebook = new Notebook("Old name");

        notebook.Rename("New name");

        Assert.AreEqual("New name", notebook.Name);
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    public void Rename_WithInvalidName_ThrowsArgumentException(string name)
    {
        var notebook = new Notebook("Original name");

        Assert.ThrowsException<ArgumentException>(
            () => notebook.Rename(name));

        Assert.AreEqual("Original name", notebook.Name);
    }
}
