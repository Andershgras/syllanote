using Microsoft.EntityFrameworkCore;

namespace Syllanote.Infrastructure.Persistence;

public sealed class DatabaseMigrationService
{
    private readonly SyllanoteDbContext _dbContext;

    public DatabaseMigrationService(SyllanoteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.Database.MigrateAsync(cancellationToken);
    }
}
