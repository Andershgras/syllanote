using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Syllanote.Infrastructure.Persistence;
using Syllanote.Application.Abstractions;
using Syllanote.Infrastructure.Repositories;

namespace Syllanote.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SyllanoteDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<INotebookRepository, NotebookRepository>();
        services.AddScoped<ISectionRepository, SectionRepository>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<IConceptRepository, ConceptRepository>();

        return services;
    }
}
