using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Domain
{
    [TestClass]
    public class SectionTests
    {
        [TestMethod]
        public void Constructor_WithValidData_CreatesSection()
        {
            // Arrange
            var notebookId = Guid.NewGuid();

            // Act
            var section = new Section(notebookId, "Classification");

            // Assert
            Assert.AreNotEqual(Guid.Empty, section.Id);
            Assert.AreEqual(notebookId, section.NotebookId);
            Assert.AreEqual("Classification", section.Name);
        }
        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        public void Constructor_WithInvalidName_ThrowsArgumentException(
            string name)
        {
            var notebookId = Guid.NewGuid();

            Assert.ThrowsException<ArgumentException>(
                () => new Section(notebookId, name));
        }
        [TestMethod]
        public void Constructor_WithEmptyNotebookId_ThrowsArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(
                () => new Section(Guid.Empty, "Classification"));
        }

        [TestMethod]
        public void Rename_WithValidName_UpdatesName()
        {
            var section = new Section(Guid.NewGuid(), "Old name");

            section.Rename("New name");

            Assert.AreEqual("New name", section.Name);
        }

        [DataTestMethod]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("\t")]
        public void Rename_WithInvalidName_ThrowsArgumentException(string name)
        {
            var section = new Section(Guid.NewGuid(), "Original name");

            Assert.ThrowsException<ArgumentException>(
                () => section.Rename(name));

            Assert.AreEqual("Original name", section.Name);
        }
    }
}
