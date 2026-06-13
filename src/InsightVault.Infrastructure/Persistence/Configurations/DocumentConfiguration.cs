using InsightVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightVault.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.OriginalFileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(document => document.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(document => document.SizeInBytes)
            .IsRequired();

        builder.Property(document => document.BlobName)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(document => document.UploadedAtUtc)
            .IsRequired();

        builder.Property(document => document.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Navigation(document => document.Chunks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
