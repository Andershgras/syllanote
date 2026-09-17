using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Sections.Pages.CreatePage;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Notebooks;

[TestClass]
public class CreatePageServiceTests
{
    private class FakePageRepository : IPageRepository
    {
        public Page? AddedPage { get; private set; }

        public int AddCallCount { get; private set; }

        public Task AddAsync(Page page)
        {
            AddCallCount++;
            AddedPage = page;

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<Page>> GetBySectionIdAsync(
            Guid sectionId)
        {
            IReadOnlyList<Page> pages = [];

            return Task.FromResult(pages);
        }
    }

    [TestMethod]
    public async Task CreateAsync_WithValidData_CreatesAndAddsPage()
    {
        // Arrange
        var repository = new FakePageRepository();
        var service = new CreatePageService(repository);
        var sectionId = Guid.NewGuid();

        // Act
        var page = await service.CreateAsync(
            sectionId,
            "Confusion Matrix");

        // Assert
        Assert.AreEqual(sectionId, page.SectionId);
        Assert.AreEqual("Confusion Matrix", page.Title);
        Assert.AreSame(page, repository.AddedPage);
        Assert.AreEqual(1, repository.AddCallCount);
    }
    [DataTestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    public async Task CreateAsync_WithInvalidTitle_DoesNotAddPage(
    string title)
    {
        // Arrange
        var repository = new FakePageRepository();
        var service = new CreatePageService(repository);
        var sectionId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => service.CreateAsync(sectionId, title));

        Assert.AreEqual(0, repository.AddCallCount);
    }
    [TestMethod]
    public async Task CreateAsync_WithEmptySectionId_DoesNotAddPage()
    {
        // Arrange
        var repository = new FakePageRepository();
        var service = new CreatePageService(repository);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => service.CreateAsync(
                Guid.Empty,
                "Confusion Matrix"));

        Assert.AreEqual(0, repository.AddCallCount);
    }
}