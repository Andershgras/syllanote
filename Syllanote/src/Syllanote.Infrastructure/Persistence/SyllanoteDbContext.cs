using Microsoft.EntityFrameworkCore;
using Syllanote.Domain.Entities;

namespace Syllanote.Infrastructure.Persistence;

public class SyllanoteDbContext : DbContext
{
    public SyllanoteDbContext(DbContextOptions<SyllanoteDbContext> options)
        : base(options)
    {
    }

    public DbSet<Notebook> Notebooks => Set<Notebook>();
}