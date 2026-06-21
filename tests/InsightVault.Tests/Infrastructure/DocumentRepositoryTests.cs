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
