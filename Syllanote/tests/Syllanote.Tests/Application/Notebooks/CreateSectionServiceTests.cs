using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Sections.CreateSection;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Notebooks;

[TestClass]
public class CreateSectionServiceTests
{
    private class FakeSectionRepository : ISectionRepository
    {
        public IReadOnlyList<Section> Sections { get; set; } = [];

        public Section? AddedSection { get; private set; }

        public int AddCallCount { get; private set; }

        public Task AddAsync(Section section)
        {
            AddCallCount++;
            AddedSection = section;

            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<Section>> GetByNotebookIdAsync(
            Guid notebookId)
        {
            return Task.FromResult(Sections);
        }

        public Task UpdateAsync(Section section)
        {
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Section section) => Task.CompletedTask;
    }

    [TestMethod]
    public async Task CreateAsync_WithValidData_CreatesAndAddsSection()
    {
        // Arrange
        var repository = new FakeSectionRepository();
        var service = new CreateSectionService(repository);
        var notebookId = Guid.NewGuid();

        // Act
        var section = await service.CreateAsync(
            notebookId,
            "Classification");

        // Assert
        Assert.AreEqual("Classification", section.Name);
        Assert.AreEqual(notebookId, section.NotebookId);
        Assert.AreSame(section, repository.AddedSection);
        Assert.AreEqual(1, repository.AddCallCount);
    }

    [TestMethod]
    public async Task CreateAsync_WithExistingSections_AppendsSection()
    {
        var notebookId = Guid.NewGuid();
        var repository = new FakeSectionRepository
        {
            Sections =
            [
                new Section(notebookId, "First", 0),
                new Section(notebookId, "Second", 3)
            ]
        };
        var service = new CreateSectionService(repository);

        var section = await service.CreateAsync(notebookId, "Third");

        Assert.AreEqual(4, section.SortOrder);
    }
    [DataTestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    public async Task CreateAsync_WithInvalidName_DoesNotAddSection(
    string name)
    {
        // Arrange
        var repository = new FakeSectionRepository();
        var service = new CreateSectionService(repository);
        var notebookId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => service.CreateAsync(notebookId, name));

        Assert.AreEqual(0, repository.AddCallCount);
    }
    [TestMethod]
    public async Task CreateAsync_WithEmptyNotebookId_DoesNotAddSection()
    {
        // Arrange
        var repository = new FakeSectionRepository();
        var service = new CreateSectionService(repository);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => service.CreateAsync(
                Guid.Empty,
                "Classification"));

        Assert.AreEqual(0, repository.AddCallCount);
    }
}
