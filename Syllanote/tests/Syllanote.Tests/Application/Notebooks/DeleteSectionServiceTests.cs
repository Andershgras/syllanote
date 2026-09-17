using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Sections.DeleteSection;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Notebooks;

[TestClass]
public class DeleteSectionServiceTests
{
    [TestMethod]
    public async Task DeleteAsync_DeletesRequestedSection()
    {
        var repository = new FakeSectionRepository();
        var service = new DeleteSectionService(repository);
        var section = new Section(Guid.NewGuid(), "Old section");

        await service.DeleteAsync(section);

        Assert.AreSame(section, repository.DeletedSection);
        Assert.AreEqual(1, repository.DeleteCallCount);
    }

    private class FakeSectionRepository : ISectionRepository
    {
        public Section? DeletedSection { get; private set; }
        public int DeleteCallCount { get; private set; }

        public Task AddAsync(Section section) => Task.CompletedTask;

        public Task<IReadOnlyList<Section>> GetByNotebookIdAsync(Guid notebookId)
        {
            IReadOnlyList<Section> sections = [];
            return Task.FromResult(sections);
        }

        public Task UpdateAsync(Section section) => Task.CompletedTask;

        public Task DeleteAsync(Section section)
        {
            DeletedSection = section;
            DeleteCallCount++;
            return Task.CompletedTask;
        }
    }
}
