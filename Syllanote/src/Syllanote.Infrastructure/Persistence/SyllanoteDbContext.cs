using Microsoft.EntityFrameworkCore;

namespace Syllanote.Infrastructure.Persistence;

public class SyllanoteDbContext : DbContext
{
    public SyllanoteDbContext(DbContextOptions<SyllanoteDbContext> options)
        : base(options)
    {
    }
}