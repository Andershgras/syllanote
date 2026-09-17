using Microsoft.Extensions.DependencyInjection;
using Syllanote.Application.Notebooks.CreateNotebook;
using Syllanote.Application.Notebooks.GetNotebooks;
using Syllanote.Application.Notebooks.Sections.CreateSection;
using Syllanote.Application.Notebooks.Sections.GetSections;

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

        return services;
    }
}