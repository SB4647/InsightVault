using InsightVault.Domain.Entities;
using InsightVault.Domain.Enums;
using InsightVault.Infrastructure.Identity;
using InsightVault.Infrastructure.Persistence;
using InsightVault.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InsightVault.Tests.Infrastructure;

public sealed class DocumentRepositoryTests
{
    [Fact]
    public async Task AddPermission_InsertsNewPermissionForTrackedDocument()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setupContext = new ApplicationDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();

            setupContext.Users.AddRange(
                new ApplicationUser
                {
                    Id = "owner-1",
                    UserName = "owner@example.com",
                    NormalizedUserName = "OWNER@EXAMPLE.COM",
                    Email = "owner@example.com",
                    NormalizedEmail = "OWNER@EXAMPLE.COM",
                    EmailConfirmed = true
                },
                new ApplicationUser
                {
                    Id = "viewer-1",
                    UserName = "viewer@example.com",
                    NormalizedUserName = "VIEWER@EXAMPLE.COM",
                    Email = "viewer@example.com",
                    NormalizedEmail = "VIEWER@EXAMPLE.COM",
                    EmailConfirmed = true
                });

            setupContext.Documents.Add(Document.Create(
                "sample.pdf",
                "application/pdf",
                256,
                "documents/sample.pdf",
                new DateTime(2026, 6, 21, 2, 0, 0, DateTimeKind.Utc),
                "owner-1"));

            await setupContext.SaveChangesAsync();
        }

        await using (var sharingContext = new ApplicationDbContext(options))
        {
            var repository = new DocumentRepository(sharingContext);
            var documentId = sharingContext.Documents.Select(document => document.Id).Single();
            var document = await repository.GetByIdAsync(documentId, "owner-1");

            Assert.NotNull(document);

            var shareResult = document.ShareWithViewer("viewer-1");
            repository.AddPermission(shareResult.Permission);
            await repository.SaveChangesAsync();
        }

        await using (var assertionContext = new ApplicationDbContext(options))
        {
            var permission = await assertionContext.DocumentPermissions.SingleAsync();

            Assert.Equal("viewer-1", permission.UserId);
            Assert.Equal(DocumentPermissionLevel.Viewer, permission.Level);
        }
    }

    [Fact]
    public async Task ReplaceChunksAsync_ReprocessesExistingDocumentWithoutStaleEmbeddingUpdates()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var setupContext = new ApplicationDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();

            setupContext.Users.Add(new ApplicationUser
            {
                Id = "user-1",
                UserName = "user@example.com",
                NormalizedUserName = "USER@EXAMPLE.COM",
                Email = "user@example.com",
                NormalizedEmail = "USER@EXAMPLE.COM",
                EmailConfirmed = true
            });

            var document = Document.Create(
                "sample.pdf",
                "application/pdf",
                256,
                "documents/sample.pdf",
                new DateTime(2026, 6, 21, 1, 0, 0, DateTimeKind.Utc),
                "user-1");

            var existingChunk = DocumentChunk.Create(document.Id, 0, "old content");
            existingChunk.SetEmbedding([0.1f, 0.2f, 0.3f]);
            document.CompleteProcessing([existingChunk]);

            setupContext.Documents.Add(document);
            await setupContext.SaveChangesAsync();
        }

        await using (var processingContext = new ApplicationDbContext(options))
        {
            var repository = new DocumentRepository(processingContext);
            var document = await repository.GetByIdAsync(
                processingContext.Documents.Select(document => document.Id).Single(),
                "user-1");

            Assert.NotNull(document);

            var replacementChunk = DocumentChunk.Create(document.Id, 0, "new content");
            replacementChunk.SetEmbedding([0.4f, 0.5f, 0.6f]);

            await repository.ReplaceChunksAsync(document, [replacementChunk]);
        }

        await using (var assertionContext = new ApplicationDbContext(options))
        {
            var document = await assertionContext.Documents
                .Include(document => document.Chunks)
                .ThenInclude(chunk => chunk.Embedding)
                .SingleAsync();

            Assert.Equal(DocumentProcessingStatus.Processed, document.Status);
            var chunk = Assert.Single(document.Chunks);
            Assert.Equal("new content", chunk.Text);
            Assert.NotNull(chunk.Embedding);
        }
    }
}
