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

    /// <summary>
    /// Synchronizes domain embedding JSON into PostgreSQL's native pgvector column before a synchronous save.
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        SynchronizeVectors();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <summary>
    /// Synchronizes domain embedding JSON into PostgreSQL's native pgvector column before an asynchronous save.
    /// </summary>
    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        SynchronizeVectors();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Converts changed embedding values to pgvector and rejects vectors with an unexpected dimension.
    /// </summary>
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
