using InsightVault.Application.Features.Search;
using InsightVault.Application.Features.Search.Queries;
using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;

namespace InsightVault.Tests.Application;

public class SemanticSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_RanksChunksByCosineSimilarity()
    {
        var firstDocument = Document.Create(
            "first.pdf",
            "application/pdf",
            100,
            "documents/first.pdf",
            new DateTime(2026, 6, 12, 10, 0, 0, DateTimeKind.Utc),
            "user-1");
        var firstChunk = DocumentChunk.Create(firstDocument.Id, 0, "alpha content");
        firstChunk.SetEmbedding([1.0f, 0.0f]);
        firstDocument.CompleteProcessing([firstChunk]);

        var secondDocument = Document.Create(
            "second.pdf",
            "application/pdf",
            100,
            "documents/second.pdf",
            new DateTime(2026, 6, 12, 11, 0, 0, DateTimeKind.Utc),
            "user-1");
        var secondChunk = DocumentChunk.Create(secondDocument.Id, 0, "beta content");
        secondChunk.SetEmbedding([0.0f, 1.0f]);
        secondDocument.CompleteProcessing([secondChunk]);

        var service = new SemanticSearchService(
            new StubEmbeddingService([1.0f, 0.0f]),
            new InMemoryDocumentSearchRepository([firstDocument, secondDocument]));

        var results = await service.SearchAsync(new SearchDocumentsQuery("alpha", "user-1"));

        Assert.Collection(
            results,
            first =>
            {
                Assert.Equal(firstDocument.Id, first.DocumentId);
                Assert.Equal("first.pdf", first.DocumentName);
                Assert.Equal("alpha content", first.Text);
                Assert.Equal(1.0, first.Score, precision: 5);
            },
            second =>
            {
                Assert.Equal(secondDocument.Id, second.DocumentId);
                Assert.Equal(0.0, second.Score, precision: 5);
            });
    }

    [Fact]
    public async Task SearchAsync_WithBlankQuery_ThrowsArgumentException()
    {
        var service = new SemanticSearchService(
            new StubEmbeddingService([1.0f]),
            new InMemoryDocumentSearchRepository([]));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SearchAsync(new SearchDocumentsQuery(" ", "user-1")));
    }

    [Fact]
    public async Task SearchAsync_ExcludesChunksWithoutEmbeddings()
    {
        var document = Document.Create(
            "unprocessed.pdf",
            "application/pdf",
            100,
            "documents/unprocessed.pdf",
            new DateTime(2026, 6, 12, 10, 0, 0, DateTimeKind.Utc),
            "user-1");

        var service = new SemanticSearchService(
            new StubEmbeddingService([1.0f]),
            new InMemoryDocumentSearchRepository([document]));

        var results = await service.SearchAsync(new SearchDocumentsQuery("anything", "user-1"));

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsOnlyDocumentsOwnedByUser()
    {
        var ownedDocument = Document.Create(
            "owned.pdf",
            "application/pdf",
            100,
            "documents/owned.pdf",
            new DateTime(2026, 6, 12, 10, 0, 0, DateTimeKind.Utc),
            "user-1");
        var ownedChunk = DocumentChunk.Create(ownedDocument.Id, 0, "owned content");
        ownedChunk.SetEmbedding([1.0f]);
        ownedDocument.CompleteProcessing([ownedChunk]);

        var otherDocument = Document.Create(
            "other.pdf",
            "application/pdf",
            100,
            "documents/other.pdf",
            new DateTime(2026, 6, 12, 11, 0, 0, DateTimeKind.Utc),
            "user-2");
        var otherChunk = DocumentChunk.Create(otherDocument.Id, 0, "other content");
        otherChunk.SetEmbedding([1.0f]);
        otherDocument.CompleteProcessing([otherChunk]);

        var service = new SemanticSearchService(
            new StubEmbeddingService([1.0f]),
            new InMemoryDocumentSearchRepository([ownedDocument, otherDocument]));

        var results = await service.SearchAsync(new SearchDocumentsQuery("content", "user-1"));

        Assert.Collection(results, result => Assert.Equal("owned.pdf", result.DocumentName));
    }

    private sealed class StubEmbeddingService(IReadOnlyList<float> vector) : IEmbeddingService
    {
        public Task<IReadOnlyList<float>> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(vector);
        }
    }

    private sealed class InMemoryDocumentSearchRepository(
        IReadOnlyList<Document> documents) : IDocumentSearchRepository
    {
        public Task<IReadOnlyList<Document>> ListProcessedDocumentsAsync(
            string ownerUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Document>>(
                documents.Where(document => document.OwnerUserId == ownerUserId).ToList());
        }
    }
}
