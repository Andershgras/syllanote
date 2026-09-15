using Microsoft.Extensions.DependencyInjection;
using Syllanote.Application.Notebooks.CreateNotebook;
using Syllanote.Application.Notebooks.GetNotebooks;

namespace Syllanote.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        services.AddScoped<CreateNotebookService>();
        services.AddScoped<GetNotebooksService>();

        return services;
    }
}