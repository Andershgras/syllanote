using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Concepts.UpdateConcept;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Concepts;

[TestClass]
public class UpdateConceptServiceTests
{
    [TestMethod]
    public async Task UpdateAsync_WhenRepositoryFails_RestoresOriginalValues()
    {
        var repository = new FailingConceptRepository();
        var service = new UpdateConceptService(repository);
        var concept = new Concept(
            Guid.NewGuid(),
            "Original name",
            "Original definition");

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(
            () => service.UpdateAsync(
                concept,
                "Unsaved name",
                "Unsaved definition"));

        Assert.AreEqual("Original name", concept.Name);
        Assert.AreEqual("Original definition", concept.Definition);
        Assert.AreEqual(1, repository.UpdateCallCount);
    }

    private sealed class FailingConceptRepository : IConceptRepository
    {
        public int UpdateCallCount { get; private set; }

        public Task AddAsync(Concept concept) => Task.CompletedTask;

        public Task<IReadOnlyList<Concept>> GetByNotebookIdAsync(Guid notebookId) =>
            Task.FromResult<IReadOnlyList<Concept>>([]);

        public Task<Concept?> GetByNormalizedNameAsync(
            Guid notebookId,
            string normalizedName) => Task.FromResult<Concept?>(null);

        public Task UpdateAsync(Concept concept)
        {
            UpdateCallCount++;
            return Task.FromException(
                new InvalidOperationException("Database unavailable"));
        }

        public Task DeleteAsync(Concept concept) => Task.CompletedTask;
    }
}
