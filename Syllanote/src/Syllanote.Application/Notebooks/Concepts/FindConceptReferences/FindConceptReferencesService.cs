using Syllanote.Application.Abstractions;
using Syllanote.Application.Notebooks.Concepts.Recognition;
using Syllanote.Domain.Entities;

namespace Syllanote.Application.Notebooks.Concepts.FindConceptReferences;

public class FindConceptReferencesService
{
    private readonly ISectionRepository _sectionRepository;
    private readonly IPageRepository _pageRepository;
    private readonly RecognizeConceptsService _recognizeConceptsService;

    public FindConceptReferencesService(
        ISectionRepository sectionRepository,
        IPageRepository pageRepository,
        RecognizeConceptsService recognizeConceptsService)
    {
        _sectionRepository = sectionRepository;
        _pageRepository = pageRepository;
        _recognizeConceptsService = recognizeConceptsService;
    }

    public async Task<IReadOnlyList<ConceptReference>> FindAsync(
        Concept concept)
    {
        ArgumentNullException.ThrowIfNull(concept);

        var references = new List<ConceptReference>();
        var sections = await _sectionRepository
            .GetByNotebookIdAsync(concept.NotebookId);

        foreach (var section in sections)
        {
            var pages = await _pageRepository
                .GetBySectionIdAsync(section.Id);

            foreach (var page in pages)
            {
                var matches = _recognizeConceptsService.Recognize(
                    page.Content,
                    [concept]);

                if (matches.Count == 0)
                {
                    continue;
                }

                references.Add(new ConceptReference(
                    page.Id,
                    page.Title,
                    section.Id,
                    section.Name));
            }
        }

        return references;
    }
}
