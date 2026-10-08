using InsightVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightVault.Infrastructure.Persistence.Configurations;

public sealed class ChatAnswerConfiguration : IEntityTypeConfiguration<ChatAnswer>
{
    public void Configure(EntityTypeBuilder<ChatAnswer> builder)
    {
        builder.ToTable("ChatAnswers");
        builder.HasKey(answer => answer.Id);
        builder.Property(answer => answer.OwnerUserId).HasMaxLength(450).IsRequired();
        builder.Property(answer => answer.Question).HasMaxLength(4000).IsRequired();
        builder.Property(answer => answer.Answer).IsRequired();
        builder.Property(answer => answer.CreatedAtUtc).IsRequired();
        builder.Property(answer => answer.TopK).IsRequired();
        builder.Property(answer => answer.MinimumSimilarity).HasPrecision(4, 3).IsRequired();
        builder.Property(answer => answer.RetrievalStrategyVersion).IsRequired();
        builder.HasIndex(answer => new { answer.OwnerUserId, answer.CreatedAtUtc });
        builder.Navigation(answer => answer.Citations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
