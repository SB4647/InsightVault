using InsightVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsightVault.Infrastructure.Persistence.Configurations;

public sealed class DocumentPermissionConfiguration : IEntityTypeConfiguration<DocumentPermission>
{
    public void Configure(EntityTypeBuilder<DocumentPermission> builder)
    {
        builder.ToTable("DocumentPermissions");

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.UserId)
            .HasMaxLength(450)
            .IsRequired();

        builder.Property(permission => permission.Level)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(permission => permission.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(permission => new { permission.DocumentId, permission.UserId })
            .IsUnique();

        builder.HasIndex(permission => permission.UserId);

        builder.HasOne<Document>()
            .WithMany(document => document.Permissions)
            .HasForeignKey(permission => permission.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
