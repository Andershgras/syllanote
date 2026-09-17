using Microsoft.Extensions.DependencyInjection;
using Syllanote.Application.Notebooks.CreateNotebook;
using Syllanote.Application.Notebooks.GetNotebooks;
using Syllanote.Application.Notebooks.Sections.CreateSection;
using Syllanote.Application.Notebooks.Sections.GetSections;
using Syllanote.Application.Notebooks.Sections.Pages.CreatePage;
using Syllanote.Application.Notebooks.Sections.Pages.GetPages;

namespace Syllanote.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<CreateNotebookService>();
        services.AddScoped<GetNotebooksService>();
        services.AddScoped<CreateSectionService>();
        services.AddScoped<GetSectionsService>();
        services.AddScoped<CreatePageService>();
        services.AddScoped<GetPagesService>();

        return services;
    }
}