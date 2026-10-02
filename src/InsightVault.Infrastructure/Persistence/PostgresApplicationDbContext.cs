using InsightVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace InsightVault.Infrastructure.Persistence;

public sealed class PostgresApplicationDbContext(
    DbContextOptions<PostgresApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.Entity<Embedding>()
            .Property<Vector>("Vector")
            .HasColumnType("vector(1536)");
    }
}
