using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Sections.GetSections;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Notebooks;

[TestClass]
public class GetSectionsServiceTests
{
    private class FakeSectionRepository : ISectionRepository
    {
        public Guid? RequestedNotebookId { get; private set; }

        public IReadOnlyList<Section> SectionsToReturn { get; set; }
            = [];

        public Task AddAsync(Section section)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Section section)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Section>> GetByNotebookIdAsync(
            Guid notebookId)
        {
            RequestedNotebookId = notebookId;

            return Task.FromResult(SectionsToReturn);
        }
    }
    [TestMethod]
    public async Task GetByNotebookIdAsync_ReturnsSectionsForRequestedNotebook()
    {
        // Arrange
        var notebookId = Guid.NewGuid();

        var repository = new FakeSectionRepository
        {
            SectionsToReturn =
            [
                new Section(notebookId, "Classification"),
            new Section(notebookId, "Regression")
            ]
        };

        var service = new GetSectionsService(repository);

        // Act
        var sections =
            await service.GetByNotebookIdAsync(notebookId);

        // Assert
        Assert.AreEqual(notebookId, repository.RequestedNotebookId);

        Assert.AreEqual(2, sections.Count);
        Assert.AreEqual("Classification", sections[0].Name);
        Assert.AreEqual("Regression", sections[1].Name);
    }
}
