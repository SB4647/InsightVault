using InsightVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightVault.Infrastructure.Persistence.Configurations;

public sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("DocumentChunks");

        builder.HasKey(chunk => chunk.Id);

        builder.Property(chunk => chunk.ChunkIndex)
            .IsRequired();

        builder.Property(chunk => chunk.Text)
            .IsRequired();

        builder.Property(chunk => chunk.SourcePageNumber)
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(chunk => chunk.SectionTitle)
            .HasMaxLength(500);

        builder.HasOne<Document>()
            .WithMany(document => document.Chunks)
            .HasForeignKey(chunk => chunk.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(chunk => new { chunk.DocumentId, chunk.ChunkIndex })
            .IsUnique();
    }
}
