using Microsoft.Extensions.DependencyInjection;
using Syllanote.Application.Notebooks.CreateNotebook;
using Syllanote.Application.Notebooks.Concepts.CreateConcept;
using Syllanote.Application.Notebooks.Concepts.DeleteConcept;
using Syllanote.Application.Notebooks.Concepts.GetConcepts;
using Syllanote.Application.Notebooks.Concepts.Recognition;
using Syllanote.Application.Notebooks.Concepts.UpdateConcept;
using Syllanote.Application.Notebooks.DeleteNotebook;
using Syllanote.Application.Notebooks.GetNotebooks;
using Syllanote.Application.Notebooks.MoveNotebook;
using Syllanote.Application.Notebooks.RenameNotebook;
using Syllanote.Application.Notebooks.Sections.CreateSection;
using Syllanote.Application.Notebooks.Sections.DeleteSection;
using Syllanote.Application.Notebooks.Sections.GetSections;
using Syllanote.Application.Notebooks.Sections.MoveSection;
using Syllanote.Application.Notebooks.Sections.RenameSection;
using Syllanote.Application.Notebooks.Sections.Pages.CreatePage;
using Syllanote.Application.Notebooks.Sections.Pages.DeletePage;
using Syllanote.Application.Notebooks.Sections.Pages.GetPages;
using Syllanote.Application.Notebooks.Sections.Pages.RenamePage;
using Syllanote.Application.Notebooks.Sections.Pages.UpdatePageContent;
using Syllanote.Application.Notebooks.Sections.Pages.SearchPages;

namespace Syllanote.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<CreateNotebookService>();
        services.AddScoped<DeleteNotebookService>();
        services.AddScoped<GetNotebooksService>();
        services.AddScoped<MoveNotebookService>();
        services.AddScoped<RenameNotebookService>();
        services.AddScoped<CreateSectionService>();
        services.AddScoped<DeleteSectionService>();
        services.AddScoped<GetSectionsService>();
        services.AddScoped<MoveSectionService>();
        services.AddScoped<RenameSectionService>();
        services.AddScoped<CreatePageService>();
        services.AddScoped<DeletePageService>();
        services.AddScoped<GetPagesService>();
        services.AddScoped<RenamePageService>();
        services.AddScoped<UpdatePageContentService>();
        services.AddScoped<SearchPagesService>();
        services.AddScoped<CreateConceptService>();
        services.AddScoped<GetConceptsService>();
        services.AddScoped<UpdateConceptService>();
        services.AddScoped<DeleteConceptService>();
        services.AddScoped<RecognizeConceptsService>();

        return services;
    }
}
