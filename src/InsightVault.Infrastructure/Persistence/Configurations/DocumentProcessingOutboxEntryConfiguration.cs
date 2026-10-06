using InsightVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightVault.Infrastructure.Persistence.Configurations;

public sealed class DocumentProcessingOutboxEntryConfiguration : IEntityTypeConfiguration<DocumentProcessingOutboxEntry>
{
    public void Configure(EntityTypeBuilder<DocumentProcessingOutboxEntry> builder)
    {
        builder.ToTable("DocumentProcessingOutboxEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.OwnerUserId).HasMaxLength(450).IsRequired();
        builder.Property(entry => entry.CreatedAtUtc).IsRequired();
        builder.Property(entry => entry.DispatchAttemptCount).IsRequired();
        builder.HasIndex(entry => new { entry.DispatchedAtUtc, entry.CreatedAtUtc });
        builder.HasIndex(entry => new { entry.OwnerUserId, entry.DocumentId });
        builder.HasOne(entry => entry.Document)
            .WithMany()
            .HasForeignKey(entry => entry.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
