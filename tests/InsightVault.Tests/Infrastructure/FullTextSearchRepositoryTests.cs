using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;
using InsightVault.Infrastructure.Persistence;
using InsightVault.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InsightVault.Tests.Infrastructure;

public sealed class FullTextSearchRepositoryTests
{
    [Fact]
    public async Task SearchAsync_ReturnsOnlyProcessedOwnedOrSharedChunksWithSourceMetadata()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;

        await using (var setupContext = new ApplicationDbContext(options))
        {
            await setupContext.Database.EnsureCreatedAsync();
            var ownerDocument = CreateDocument("owner.pdf", "owner-1", "retrieval quality policy", 2, "Policy");
            var sharedDocument = CreateDocument("shared.pdf", "other-1", "retrieval quality guide", 4, "Guide");
            var privateDocument = CreateDocument("private.pdf", "other-1", "retrieval quality secrets", 6, null);
            setupContext.Documents.AddRange(ownerDocument, sharedDocument, privateDocument);
            setupContext.DocumentPermissions.Add(sharedDocument.ShareWithViewer("viewer-1").Permission);
            await setupContext.SaveChangesAsync();
        }

        await using var searchContext = new ApplicationDbContext(options);
        var repository = new DocumentFullTextSearchRepository(searchContext);

        var ownerResults = await repository.SearchAsync(new FullTextSearchRequest("owner-1", "retrieval quality", 5));
        var viewerResults = await repository.SearchAsync(new FullTextSearchRequest("viewer-1", "retrieval quality", 5));
        var unrelatedResults = await repository.SearchAsync(new FullTextSearchRequest("unrelated-1", "retrieval quality", 5));

        var ownerResult = Assert.Single(ownerResults);
        Assert.Equal("owner.pdf", ownerResult.DocumentName);
        Assert.Equal(2, ownerResult.SourcePageNumber);
        Assert.Equal("Policy", ownerResult.SectionTitle);

        var viewerResult = Assert.Single(viewerResults);
        Assert.Equal("shared.pdf", viewerResult.DocumentName);
        Assert.Empty(unrelatedResults);
    }

    private static Document CreateDocument(
        string fileName,
        string ownerUserId,
        string text,
        int pageNumber,
        string? sectionTitle)
    {
        var document = Document.Create(
            fileName,
            "application/pdf",
            100,
            $"documents/{fileName}",
            DateTime.UtcNow,
            ownerUserId);
        document.CompleteProcessing([
            DocumentChunk.Create(document.Id, 0, text, pageNumber, sectionTitle)
        ]);
        return document;
    }
}
