using InsightVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightVault.Infrastructure.Persistence.Configurations;

public sealed class EmbeddingConfiguration : IEntityTypeConfiguration<Embedding>
{
    public void Configure(EntityTypeBuilder<Embedding> builder)
    {
        builder.ToTable("Embeddings");

        builder.HasKey(embedding => embedding.Id);

        builder.Property(embedding => embedding.VectorJson)
            .IsRequired();

        builder.Property(embedding => embedding.CreatedAtUtc)
            .IsRequired();

        builder.HasOne<DocumentChunk>()
            .WithOne(chunk => chunk.Embedding)
            .HasForeignKey<Embedding>(embedding => embedding.DocumentChunkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
