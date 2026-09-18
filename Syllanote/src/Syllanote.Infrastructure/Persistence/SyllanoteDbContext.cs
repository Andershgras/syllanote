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
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Page> Pages => Set<Page>();
    public DbSet<Concept> Concepts => Set<Concept>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Section>()
            .HasOne<Notebook>()
            .WithMany()
            .HasForeignKey(section => section.NotebookId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Page>()
            .HasOne<Section>()
            .WithMany()
            .HasForeignKey(page => page.SectionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Concept>()
            .HasOne<Notebook>()
            .WithMany()
            .HasForeignKey(concept => concept.NotebookId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Concept>()
            .HasIndex(concept => new { concept.NotebookId, concept.NormalizedName })
            .IsUnique();
    }
}
