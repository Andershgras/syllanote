using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Syllanote.Infrastructure.Persistence;

public class SyllanoteDbContextFactory : IDesignTimeDbContextFactory<SyllanoteDbContext>
{
    public SyllanoteDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SyllanoteDbContext>();

        optionsBuilder.UseSqlite("Data Source=syllanote-design.db");

        return new SyllanoteDbContext(optionsBuilder.Options);
    }
}