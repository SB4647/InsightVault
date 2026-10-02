using InsightVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace InsightVault.Infrastructure.Persistence;

public sealed class PostgresApplicationDbContext(
    DbContextOptions<PostgresApplicationDbContext> options) : ApplicationDbContext(options)
{
    public const int VectorDimensions = 1536;
    public const string VectorPropertyName = "Vector";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.Entity<Embedding>()
            .Property<Vector>(VectorPropertyName)
            .HasColumnType($"vector({VectorDimensions})");
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SynchronizeVectors();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        SynchronizeVectors();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void SynchronizeVectors()
    {
        foreach (var entry in ChangeTracker.Entries<Embedding>()
                     .Where(entry => entry.State is EntityState.Added or EntityState.Modified))
        {
            var values = entry.Entity.GetVector();

            if (values.Count != VectorDimensions)
            {
                throw new InvalidOperationException(
                    $"Embedding vectors must contain {VectorDimensions} dimensions for PostgreSQL pgvector storage.");
            }

            entry.Property<Vector>(VectorPropertyName).CurrentValue = new Vector(values.ToArray());
        }
    }
}
