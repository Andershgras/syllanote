using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Syllanote.Infrastructure.Persistence;

namespace Syllanote.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SyllanoteDbContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }
}