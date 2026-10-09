using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Sections.ChangeSectionColor;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Notebooks;

[TestClass]
public class ChangeSectionColorServiceTests
{
    [TestMethod]
    public async Task ChangeAsync_WithSupportedColor_UpdatesSectionAndRepository()
    {
        var repository = new FakeSectionRepository();
        var service = new ChangeSectionColorService(repository);
        var section = new Section(Guid.NewGuid(), "Section");

        await service.ChangeAsync(section, SectionColorKeys.Blue);

        Assert.AreEqual(SectionColorKeys.Blue, section.ColorKey);
        Assert.AreSame(section, repository.UpdatedSection);
        Assert.AreEqual(1, repository.UpdateCallCount);
    }

    [TestMethod]
    public async Task ChangeAsync_WithNull_RemovesColor()
    {
        var repository = new FakeSectionRepository();
        var service = new ChangeSectionColorService(repository);
        var section = new Section(Guid.NewGuid(), "Section");
        section.ChangeColor(SectionColorKeys.Green);

        await service.ChangeAsync(section, null);

        Assert.IsNull(section.ColorKey);
        Assert.AreEqual(1, repository.UpdateCallCount);
    }

    [TestMethod]
    public async Task ChangeAsync_WithUnsupportedColor_DoesNotUpdateRepository()
    {
        var repository = new FakeSectionRepository();
        var service = new ChangeSectionColorService(repository);
        var section = new Section(Guid.NewGuid(), "Section");

        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => service.ChangeAsync(section, "unknown"));

        Assert.IsNull(section.ColorKey);
        Assert.AreEqual(0, repository.UpdateCallCount);
    }

    [TestMethod]
    public async Task ChangeAsync_WithCurrentColor_DoesNotUpdateRepository()
    {
        var repository = new FakeSectionRepository();
        var service = new ChangeSectionColorService(repository);
        var section = new Section(Guid.NewGuid(), "Section");
        section.ChangeColor(SectionColorKeys.Purple);

        await service.ChangeAsync(section, SectionColorKeys.Purple);

        Assert.AreEqual(0, repository.UpdateCallCount);
    }

    [TestMethod]
    public async Task ChangeAsync_WhenRepositoryFails_RestoresOriginalColor()
    {
        var repository = new FakeSectionRepository
        {
            UpdateException = new InvalidOperationException("Database unavailable")
        };
        var service = new ChangeSectionColorService(repository);
        var section = new Section(Guid.NewGuid(), "Section");
        section.ChangeColor(SectionColorKeys.Orange);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => service.ChangeAsync(section, SectionColorKeys.Teal));

        Assert.AreEqual(SectionColorKeys.Orange, section.ColorKey);
        Assert.AreEqual(1, repository.UpdateCallCount);
    }

    private sealed class FakeSectionRepository : ISectionRepository
    {
        public Section? UpdatedSection { get; private set; }
        public int UpdateCallCount { get; private set; }
        public Exception? UpdateException { get; init; }

        public Task AddAsync(Section section) => Task.CompletedTask;

        public Task<IReadOnlyList<Section>> GetByNotebookIdAsync(Guid notebookId) =>
            Task.FromResult<IReadOnlyList<Section>>([]);

        public Task UpdateAsync(Section section)
        {
            UpdatedSection = section;
            UpdateCallCount++;

            return UpdateException is null
                ? Task.CompletedTask
                : Task.FromException(UpdateException);
        }

        public Task DeleteAsync(Section section) => Task.CompletedTask;
    }
}
