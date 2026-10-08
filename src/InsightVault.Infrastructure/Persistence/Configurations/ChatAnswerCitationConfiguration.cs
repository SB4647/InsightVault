using InsightVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightVault.Infrastructure.Persistence.Configurations;

public sealed class ChatAnswerCitationConfiguration : IEntityTypeConfiguration<ChatAnswerCitation>
{
    public void Configure(EntityTypeBuilder<ChatAnswerCitation> builder)
    {
        builder.ToTable("ChatAnswerCitations");
        builder.HasKey(citation => citation.Id);
        builder.Property(citation => citation.DocumentVersion).IsRequired();
        builder.Property(citation => citation.SourcePageNumber).IsRequired();
        builder.Property(citation => citation.SectionTitle).HasMaxLength(500);
        builder.Property(citation => citation.Score).HasPrecision(5, 4).IsRequired();
        builder.Property(citation => citation.Rank).IsRequired();
        builder.HasIndex(citation => new { citation.ChatAnswerId, citation.Rank }).IsUnique();
        builder.HasIndex(citation => citation.DocumentId);
        builder.HasIndex(citation => citation.DocumentChunkId);
        builder.HasOne<ChatAnswer>()
            .WithMany(answer => answer.Citations)
            .HasForeignKey(citation => citation.ChatAnswerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
