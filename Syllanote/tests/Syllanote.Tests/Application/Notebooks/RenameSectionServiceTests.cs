using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Sections.RenameSection;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Notebooks;

[TestClass]
public class RenameSectionServiceTests
{
    [TestMethod]
    public async Task RenameAsync_WithValidName_UpdatesSectionAndRepository()
    {
        var repository = new FakeSectionRepository();
        var service = new RenameSectionService(repository);
        var section = new Section(Guid.NewGuid(), "Old name");

        await service.RenameAsync(section, "New name");

        Assert.AreEqual("New name", section.Name);
        Assert.AreSame(section, repository.UpdatedSection);
        Assert.AreEqual(1, repository.UpdateCallCount);
    }

    [TestMethod]
    public async Task RenameAsync_WithInvalidName_DoesNotUpdateRepository()
    {
        var repository = new FakeSectionRepository();
        var service = new RenameSectionService(repository);
        var section = new Section(Guid.NewGuid(), "Original name");

        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => service.RenameAsync(section, " "));

        Assert.AreEqual("Original name", section.Name);
        Assert.AreEqual(0, repository.UpdateCallCount);
    }

    private class FakeSectionRepository : ISectionRepository
    {
        public Section? UpdatedSection { get; private set; }
        public int UpdateCallCount { get; private set; }

        public Task AddAsync(Section section) => Task.CompletedTask;

        public Task<IReadOnlyList<Section>> GetByNotebookIdAsync(
            Guid notebookId)
        {
            IReadOnlyList<Section> sections = [];
            return Task.FromResult(sections);
        }

        public Task UpdateAsync(Section section)
        {
            UpdatedSection = section;
            UpdateCallCount++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Section section) => Task.CompletedTask;
    }
}
