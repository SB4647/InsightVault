using InsightVault.Application.Features.Documents.Processing;
using InsightVault.Application.Features.Documents.Processing.Commands;
using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;
using InsightVault.Domain.Enums;

namespace InsightVault.Tests.Application;

public class DocumentProcessingServiceTests
{
    [Fact]
    public async Task ProcessAsync_ExtractsTextChunksEmbedsAndPersistsVectors()
    {
        var document = Document.Create(
            "sample.pdf",
            "application/pdf",
            256,
            "documents/sample.pdf",
            new DateTime(2026, 6, 12, 10, 30, 0, DateTimeKind.Utc),
            "user-1");
        var repository = new InMemoryDocumentRepository(document);
        var blobStorage = new RecordingBlobStorageService();
        var extractor = new StubTextExtractionService("alpha beta gamma delta epsilon zeta eta theta iota kappa lambda");
        var chunker = new DocumentChunkingService();
        var embeddings = new StubEmbeddingService();
        var service = new DocumentProcessingService(repository, blobStorage, extractor, chunker, embeddings);

        var result = await service.ProcessAsync(
            new ProcessDocumentCommand(document.Id, "user-1", ChunkSize: 30, OverlapSize: 5));

        Assert.Equal(document.Id, result.DocumentId);
        Assert.True(result.ChunkCount > 1);
        Assert.Equal(DocumentProcessingStatus.Processed, document.Status);
        Assert.Equal(result.ChunkCount, document.Chunks.Count);
        Assert.All(document.Chunks, chunk => Assert.NotNull(chunk.Embedding));
        Assert.Equal(result.ChunkCount, embeddings.RequestedTexts.Count);
        Assert.Equal("documents/sample.pdf", blobStorage.DownloadedBlobName);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ProcessAsync_WhenDocumentDoesNotExist_ThrowsInvalidOperationException()
    {
        var repository = new InMemoryDocumentRepository();
        var service = new DocumentProcessingService(
            repository,
            new RecordingBlobStorageService(),
            new StubTextExtractionService("content"),
            new DocumentChunkingService(),
            new StubEmbeddingService());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessAsync(new ProcessDocumentCommand(Guid.NewGuid(), "user-1")));
    }

    [Fact]
    public async Task ProcessAsync_WhenDocumentBelongsToAnotherUser_ThrowsInvalidOperationException()
    {
        var document = Document.Create(
            "sample.pdf",
            "application/pdf",
            256,
            "documents/sample.pdf",
            new DateTime(2026, 6, 12, 10, 30, 0, DateTimeKind.Utc),
            "user-2");
        var repository = new InMemoryDocumentRepository(document);
        var service = new DocumentProcessingService(
            repository,
            new RecordingBlobStorageService(),
            new StubTextExtractionService("content"),
            new DocumentChunkingService(),
            new StubEmbeddingService());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessAsync(new ProcessDocumentCommand(document.Id, "user-1")));
    }

    private sealed class InMemoryDocumentRepository(params Document[] documents) : IDocumentRepository
    {
        private readonly List<Document> _documents = [.. documents];

        public int SaveChangesCallCount { get; private set; }

        public Task AddAsync(Document document, CancellationToken cancellationToken = default)
        {
            _documents.Add(document);
            return Task.CompletedTask;
        }

        public Task<Document?> GetByIdAsync(
            Guid id,
            string ownerUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_documents.SingleOrDefault(document =>
                document.Id == id && document.OwnerUserId == ownerUserId));
        }

        public Task<IReadOnlyList<Document>> ListAsync(
            string ownerUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Document>>(
                _documents.Where(document => document.OwnerUserId == ownerUserId).ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingBlobStorageService : IBlobStorageService
    {
        public string? DownloadedBlobName { get; private set; }

        public Task UploadAsync(
            string blobName,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
        {
            DownloadedBlobName = blobName;
            return Task.FromResult<Stream>(new MemoryStream([1, 2, 3]));
        }
    }

    private sealed class StubTextExtractionService(string text) : ITextExtractionService
    {
        public Task<string> ExtractTextAsync(Stream document, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(text);
        }
    }

    private sealed class StubEmbeddingService : IEmbeddingService
    {
        public List<string> RequestedTexts { get; } = [];

        public Task<IReadOnlyList<float>> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            RequestedTexts.Add(text);
            return Task.FromResult<IReadOnlyList<float>>([1.0f, 2.0f, 3.0f]);
        }
    }
}
