using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Concepts.FindConceptReferences;
using Syllanote.Application.Notebooks.Concepts.Recognition;
using Syllanote.Application.Notebooks.Sections.Pages.SearchPages;
using Syllanote.Domain.Entities;

namespace Syllanote.Tests.Application.Concepts;

[TestClass]
public class FindConceptReferencesServiceTests
{
    [TestMethod]
    public async Task FindAsync_ReturnsMatchingPagesFromConceptNotebook()
    {
        var notebookId = Guid.NewGuid();
        var section = new Section(notebookId, "Patterns");
        var matchingPage = CreatePage(
            section.Id,
            "Singleton notes",
            "A singleton has one instance.");
        var otherPage = CreatePage(
            section.Id,
            "Factory notes",
            "A factory creates objects.");
        var service = CreateService(
            [section],
            new Dictionary<Guid, IReadOnlyList<Page>>
            {
                [section.Id] = [matchingPage, otherPage]
            });

        var references = await service.FindAsync(
            new Concept(notebookId, "Singleton", "Definition"));

        Assert.AreEqual(1, references.Count);
        Assert.AreEqual(matchingPage.Id, references[0].PageId);
        Assert.AreEqual(matchingPage.Title, references[0].PageTitle);
        Assert.AreEqual(section.Id, references[0].SectionId);
        Assert.AreEqual(section.Name, references[0].SectionName);
    }

    [TestMethod]
    public async Task FindAsync_UsesConceptMatchingRules()
    {
        var notebookId = Guid.NewGuid();
        var section = new Section(notebookId, "Patterns");
        var caseInsensitiveMatch = CreatePage(
            section.Id,
            "Exact match",
            "A SINGLETON is useful.");
        var substringOnly = CreatePage(
            section.Id,
            "Substring",
            "SingletonPattern is another term.");
        var titleOnly = CreatePage(
            section.Id,
            "Singleton",
            "The content does not contain the term.");
        var emptyPage = CreatePage(
            section.Id,
            "Empty page",
            string.Empty);
        var service = CreateService(
            [section],
            new Dictionary<Guid, IReadOnlyList<Page>>
            {
                [section.Id] =
                [
                    caseInsensitiveMatch,
                    substringOnly,
                    titleOnly,
                    emptyPage
                ]
            });

        var references = await service.FindAsync(
            new Concept(notebookId, "Singleton", "Definition"));

        Assert.AreEqual(1, references.Count);
        Assert.AreEqual(caseInsensitiveMatch.Id, references[0].PageId);
    }

    [TestMethod]
    public async Task FindAsync_WithRepeatedOccurrences_ReturnsPageOnce()
    {
        var notebookId = Guid.NewGuid();
        var section = new Section(notebookId, "Patterns");
        var page = CreatePage(
            section.Id,
            "Singleton notes",
            "Singleton, singleton, SINGLETON.");
        var service = CreateService(
            [section],
            new Dictionary<Guid, IReadOnlyList<Page>>
            {
                [section.Id] = [page]
            });

        var references = await service.FindAsync(
            new Concept(notebookId, "Singleton", "Definition"));

        Assert.AreEqual(1, references.Count);
    }

    [TestMethod]
    public async Task FindAsync_OnlyLoadsSectionsFromConceptNotebook()
    {
        var notebookId = Guid.NewGuid();
        var includedSection = new Section(notebookId, "Included");
        var otherSection = new Section(Guid.NewGuid(), "Other notebook");
        var sectionRepository = new FakeSectionRepository
        {
            Sections = [includedSection]
        };
        var pageRepository = new FakePageRepository
        {
            PagesBySectionId = new Dictionary<Guid, IReadOnlyList<Page>>
            {
                [includedSection.Id] = [],
                [otherSection.Id] =
                [
                    CreatePage(otherSection.Id, "Other page", "Singleton")
                ]
            }
        };
        var service = new FindConceptReferencesService(
            sectionRepository,
            pageRepository,
            new RecognizeConceptsService());

        var references = await service.FindAsync(
            new Concept(notebookId, "Singleton", "Definition"));

        Assert.AreEqual(notebookId, sectionRepository.NotebookId);
        Assert.AreEqual(0, references.Count);
        CollectionAssert.AreEqual(
            new[] { includedSection.Id },
            pageRepository.RequestedSectionIds.ToArray());
    }

    [TestMethod]
    public async Task FindAsync_PreservesRepositorySectionAndPageOrder()
    {
        var notebookId = Guid.NewGuid();
        var secondSection = new Section(notebookId, "Second");
        var firstSection = new Section(notebookId, "First");
        var secondPage = CreatePage(
            firstSection.Id,
            "Second page",
            "Singleton");
        var firstPage = CreatePage(
            firstSection.Id,
            "First page",
            "Singleton");
        var otherSectionPage = CreatePage(
            secondSection.Id,
            "Other section page",
            "Singleton");
        var service = CreateService(
            [firstSection, secondSection],
            new Dictionary<Guid, IReadOnlyList<Page>>
            {
                [firstSection.Id] = [firstPage, secondPage],
                [secondSection.Id] = [otherSectionPage]
            });

        var references = await service.FindAsync(
            new Concept(notebookId, "Singleton", "Definition"));

        CollectionAssert.AreEqual(
            new[] { firstPage.Id, secondPage.Id, otherSectionPage.Id },
            references.Select(reference => reference.PageId).ToArray());
    }

    [TestMethod]
    public async Task FindAsync_WithNullConcept_ThrowsArgumentNullException()
    {
        var service = CreateService(
            [],
            new Dictionary<Guid, IReadOnlyList<Page>>());

        await Assert.ThrowsExceptionAsync<ArgumentNullException>(
            () => service.FindAsync(null!));
    }

    private static FindConceptReferencesService CreateService(
        IReadOnlyList<Section> sections,
        IReadOnlyDictionary<Guid, IReadOnlyList<Page>> pagesBySectionId)
    {
        return new FindConceptReferencesService(
            new FakeSectionRepository { Sections = sections },
            new FakePageRepository { PagesBySectionId = pagesBySectionId },
            new RecognizeConceptsService());
    }

    private static Page CreatePage(
        Guid sectionId,
        string title,
        string content)
    {
        var page = new Page(sectionId, title);
        page.UpdateContent(content, string.Empty);
        return page;
    }

    private class FakeSectionRepository : ISectionRepository
    {
        public Guid? NotebookId { get; private set; }
        public IReadOnlyList<Section> Sections { get; set; } = [];

        public Task<IReadOnlyList<Section>> GetByNotebookIdAsync(
            Guid notebookId)
        {
            NotebookId = notebookId;
            return Task.FromResult(Sections);
        }

        public Task AddAsync(Section section) => Task.CompletedTask;
        public Task UpdateAsync(Section section) => Task.CompletedTask;
        public Task DeleteAsync(Section section) => Task.CompletedTask;
    }

    private class FakePageRepository : IPageRepository
    {
        public IReadOnlyDictionary<Guid, IReadOnlyList<Page>> PagesBySectionId
        {
            get;
            set;
        } = new Dictionary<Guid, IReadOnlyList<Page>>();

        public List<Guid> RequestedSectionIds { get; } = [];

        public Task<IReadOnlyList<Page>> GetBySectionIdAsync(Guid sectionId)
        {
            RequestedSectionIds.Add(sectionId);
            return Task.FromResult(PagesBySectionId[sectionId]);
        }

        public Task AddAsync(Page page) => Task.CompletedTask;
        public Task<IReadOnlyList<SearchPageResult>> SearchAsync(string searchText)
            => Task.FromResult<IReadOnlyList<SearchPageResult>>([]);
        public Task UpdateAsync(Page page) => Task.CompletedTask;
        public Task DeleteAsync(Page page) => Task.CompletedTask;
    }
}
