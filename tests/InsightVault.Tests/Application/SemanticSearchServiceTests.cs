using InsightVault.Application.Features.Search;
using InsightVault.Application.Features.Search.Queries;
using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;

namespace InsightVault.Tests.Application;

public class SemanticSearchServiceTests
{
    [Fact]
    public void Constructor_DependsOnVectorSearchRepository()
    {
        var constructor = typeof(SemanticSearchService).GetConstructors().Single();

        Assert.Contains(
            constructor.GetParameters(),
            parameter => parameter.ParameterType.Name == "IVectorSearchRepository");
    }

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

        var service = CreateService(
            new StubEmbeddingService([1.0f, 0.0f]),
            new InMemoryVectorSearchRepository([firstDocument, secondDocument]));

        var results = await service.SearchAsync(new SearchDocumentsQuery("alpha", "user-1"));

        var first = Assert.Single(results);
        Assert.Equal(firstDocument.Id, first.DocumentId);
        Assert.Equal("first.pdf", first.DocumentName);
        Assert.Equal("alpha content", first.Text);
        Assert.Equal(1.0, first.Score, precision: 5);
    }

    [Fact]
    public async Task SearchAsync_WithBlankQuery_ThrowsArgumentException()
    {
        var service = CreateService(
            new StubEmbeddingService([1.0f]),
            new InMemoryVectorSearchRepository([]));

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

        var service = CreateService(
            new StubEmbeddingService([1.0f]),
            new InMemoryVectorSearchRepository([document]));

        var results = await service.SearchAsync(new SearchDocumentsQuery("anything", "user-1"));

        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_ReturnsOwnedAndSharedDocuments()
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
        otherDocument.ShareWithViewer("user-1");

        var service = CreateService(
            new StubEmbeddingService([1.0f]),
            new InMemoryVectorSearchRepository([ownedDocument, otherDocument]));

        var results = await service.SearchAsync(new SearchDocumentsQuery("content", "user-1"));

        Assert.Collection(
            results,
            first => Assert.Equal("other.pdf", first.DocumentName),
            second => Assert.Equal("owned.pdf", second.DocumentName));
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

    private static SemanticSearchService CreateService(
        IEmbeddingService embeddingService,
        IVectorSearchRepository vectorSearchRepository)
    {
        var options = new RetrievalOptions();
        return new SemanticSearchService(
            embeddingService,
            vectorSearchRepository,
            new EmptyFullTextSearchRepository(),
            new HybridRetrievalService(options),
            options);
    }

    private sealed class EmptyFullTextSearchRepository : IFullTextSearchRepository
    {
        public Task<IReadOnlyList<FullTextSearchMatch>> SearchAsync(
            FullTextSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<FullTextSearchMatch>>([]);
        }
    }

    private sealed class InMemoryVectorSearchRepository(
        IReadOnlyList<Document> documents) : IVectorSearchRepository
    {
        public Task<IReadOnlyList<VectorSearchMatch>> SearchAsync(
            VectorSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<VectorSearchMatch>>(
                documents
                    .Where(document =>
                        document.OwnerUserId == request.OwnerUserId ||
                        document.Permissions.Any(permission => permission.UserId == request.OwnerUserId))
                    .SelectMany(document => document.Chunks
                        .Where(chunk => chunk.Embedding is not null)
                        .Select(chunk => new VectorSearchMatch(
                            document.Id,
                            document.OriginalFileName,
                            chunk.Id,
                            chunk.ChunkIndex,
                            chunk.Text,
                            CosineSimilarity(request.QueryEmbedding, chunk.Embedding!.GetVector()))))
                    .OrderByDescending(match => match.Score)
                    .ThenBy(match => match.DocumentName)
                    .ThenBy(match => match.ChunkIndex)
                    .Take(request.MaxResults)
                    .ToList());
        }

        private static double CosineSimilarity(
            IReadOnlyList<float> left,
            IReadOnlyList<float> right)
        {
            if (left.Count == 0 || right.Count == 0 || left.Count != right.Count)
            {
                return 0;
            }

            double dotProduct = 0;
            double leftMagnitude = 0;
            double rightMagnitude = 0;

            for (var index = 0; index < left.Count; index++)
            {
                dotProduct += left[index] * right[index];
                leftMagnitude += left[index] * left[index];
                rightMagnitude += right[index] * right[index];
            }

            return leftMagnitude == 0 || rightMagnitude == 0
                ? 0
                : dotProduct / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
        }
    }
}
