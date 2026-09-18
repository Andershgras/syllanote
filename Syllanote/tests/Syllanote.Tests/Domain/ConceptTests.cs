using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Domain;

[TestClass]
public class ConceptTests
{
    [TestMethod]
    public void Constructor_WithValidData_CreatesNotebookScopedConcept()
    {
        var notebookId = Guid.NewGuid();

        var concept = new Concept(
            notebookId,
            "  Dependency Injection  ",
            "  A dependency is supplied from outside.  ");

        Assert.AreNotEqual(Guid.Empty, concept.Id);
        Assert.AreEqual(notebookId, concept.NotebookId);
        Assert.AreEqual("Dependency Injection", concept.Name);
        Assert.AreEqual("DEPENDENCY INJECTION", concept.NormalizedName);
        Assert.AreEqual("A dependency is supplied from outside.", concept.Definition);
        Assert.AreEqual(concept.CreatedAt, concept.UpdatedAt);
        Assert.AreEqual(DateTimeKind.Utc, concept.CreatedAt.Kind);
    }

    [TestMethod]
    public void Constructor_WithEmptyNotebookId_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new Concept(Guid.Empty, "Singleton", "One instance."));
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    public void Constructor_WithBlankName_ThrowsArgumentException(string name)
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new Concept(Guid.NewGuid(), name, "Definition"));
    }

    [DataTestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    public void Constructor_WithBlankDefinition_ThrowsArgumentException(string definition)
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new Concept(Guid.NewGuid(), "Singleton", definition));
    }

    [TestMethod]
    public void Rename_NormalizesNameAndUpdatesTimestamp()
    {
        var concept = new Concept(Guid.NewGuid(), "Old name", "Definition");
        var originalUpdatedAt = concept.UpdatedAt;

        concept.Rename("  Årsag  ");

        Assert.AreEqual("Årsag", concept.Name);
        Assert.AreEqual("ÅRSAG", concept.NormalizedName);
        Assert.AreEqual("Definition", concept.Definition);
        Assert.IsTrue(concept.UpdatedAt >= originalUpdatedAt);
    }

    [TestMethod]
    public void NamesWithDifferentCaseAndUnicodeForm_HaveSameNormalizedName()
    {
        var notebookId = Guid.NewGuid();
        var first = new Concept(notebookId, "Årsag", "First definition");
        var second = new Concept(notebookId, "a\u030arsag", "Second definition");

        Assert.AreEqual(first.NormalizedName, second.NormalizedName);
    }

    [TestMethod]
    public void Rename_WithBlankName_LeavesConceptUnchanged()
    {
        var concept = new Concept(Guid.NewGuid(), "Singleton", "Definition");
        var originalUpdatedAt = concept.UpdatedAt;

        Assert.ThrowsException<ArgumentException>(() => concept.Rename("  "));

        Assert.AreEqual("Singleton", concept.Name);
        Assert.AreEqual("SINGLETON", concept.NormalizedName);
        Assert.AreEqual(originalUpdatedAt, concept.UpdatedAt);
    }

    [TestMethod]
    public void UpdateDefinition_UpdatesTextAndTimestamp()
    {
        var concept = new Concept(Guid.NewGuid(), "Singleton", "Old definition");
        var originalUpdatedAt = concept.UpdatedAt;

        concept.UpdateDefinition("  One instance.\nShared use.  ");

        Assert.AreEqual("One instance.\nShared use.", concept.Definition);
        Assert.IsTrue(concept.UpdatedAt >= originalUpdatedAt);
    }

    [TestMethod]
    public void UpdateDefinition_WithBlankText_LeavesConceptUnchanged()
    {
        var concept = new Concept(Guid.NewGuid(), "Singleton", "Definition");
        var originalUpdatedAt = concept.UpdatedAt;

        Assert.ThrowsException<ArgumentException>(() => concept.UpdateDefinition("  "));

        Assert.AreEqual("Definition", concept.Definition);
        Assert.AreEqual(originalUpdatedAt, concept.UpdatedAt);
    }

    [TestMethod]
    public void Update_WithBlankDefinition_LeavesNameAndDefinitionUnchanged()
    {
        var concept = new Concept(Guid.NewGuid(), "Singleton", "Definition");
        var originalUpdatedAt = concept.UpdatedAt;

        Assert.ThrowsException<ArgumentException>(() =>
            concept.Update("New name", "  "));

        Assert.AreEqual("Singleton", concept.Name);
        Assert.AreEqual("SINGLETON", concept.NormalizedName);
        Assert.AreEqual("Definition", concept.Definition);
        Assert.AreEqual(originalUpdatedAt, concept.UpdatedAt);
    }
}
