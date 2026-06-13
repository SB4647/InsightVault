using InsightVault.Application.Features.Documents;
using InsightVault.Application.Features.Documents.Commands;
using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;

namespace InsightVault.Tests.Application;

public class DocumentServiceTests
{
    [Fact]
    public async Task UploadAsync_StoresBlobAndSavesDocumentMetadata()
    {
        var repository = new InMemoryDocumentRepository();
        var blobStorage = new RecordingBlobStorageService();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 6, 12, 10, 30, 0, TimeSpan.Zero));
        var service = new DocumentService(repository, blobStorage, timeProvider);
        await using var content = new MemoryStream([1, 2, 3]);
        var command = new UploadDocumentCommand("Report.pdf", "application/pdf", 3, content, "user-1");

        var result = await service.UploadAsync(command);

        Assert.Equal("Report.pdf", result.OriginalFileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(3, result.SizeInBytes);
        Assert.Equal(0, result.ChunkCount);
        Assert.Equal(new DateTime(2026, 6, 12, 10, 30, 0, DateTimeKind.Utc), result.UploadedAtUtc);
        Assert.EndsWith(".pdf", result.BlobName);
        Assert.Equal(result.BlobName, blobStorage.UploadedBlobName);
        Assert.Equal("application/pdf", blobStorage.UploadedContentType);
        Assert.Single(repository.Documents);
        Assert.Equal(1, repository.SaveChangesCallCount);
        Assert.Equal("user-1", repository.Documents.Single().OwnerUserId);
    }

    [Fact]
    public async Task GetDocumentsAsync_ReturnsDocumentsNewestFirst()
    {
        var repository = new InMemoryDocumentRepository();
        var older = Document.Create(
            "older.pdf",
            "application/pdf",
            100,
            "documents/older.pdf",
            new DateTime(2026, 6, 11, 10, 0, 0, DateTimeKind.Utc),
            "user-1");
        var newer = Document.Create(
            "newer.pdf",
            "application/pdf",
            200,
            "documents/newer.pdf",
            new DateTime(2026, 6, 12, 10, 0, 0, DateTimeKind.Utc),
            "user-1");
        repository.Documents.Add(older);
        repository.Documents.Add(newer);
        var service = new DocumentService(repository, new RecordingBlobStorageService(), TimeProvider.System);

        var documents = await service.GetDocumentsAsync("user-1");

        Assert.Collection(
            documents,
            first => Assert.Equal("newer.pdf", first.OriginalFileName),
            second => Assert.Equal("older.pdf", second.OriginalFileName));
    }

    [Fact]
    public async Task GetDocumentsAsync_ReturnsOnlyDocumentsOwnedByUser()
    {
        var repository = new InMemoryDocumentRepository();
        var owned = Document.Create(
            "owned.pdf",
            "application/pdf",
            100,
            "documents/owned.pdf",
            new DateTime(2026, 6, 12, 10, 0, 0, DateTimeKind.Utc),
            "user-1");
        var other = Document.Create(
            "other.pdf",
            "application/pdf",
            100,
            "documents/other.pdf",
            new DateTime(2026, 6, 12, 11, 0, 0, DateTimeKind.Utc),
            "user-2");
        repository.Documents.Add(owned);
        repository.Documents.Add(other);
        var service = new DocumentService(repository, new RecordingBlobStorageService(), TimeProvider.System);

        var documents = await service.GetDocumentsAsync("user-1");

        Assert.Collection(documents, document => Assert.Equal("owned.pdf", document.OriginalFileName));
    }

    private sealed class InMemoryDocumentRepository : IDocumentRepository
    {
        public List<Document> Documents { get; } = [];
        public int SaveChangesCallCount { get; private set; }

        public Task AddAsync(Document document, CancellationToken cancellationToken = default)
        {
            Documents.Add(document);
            return Task.CompletedTask;
        }

        public Task<Document?> GetByIdAsync(
            Guid id,
            string ownerUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Documents.SingleOrDefault(document =>
                document.Id == id && document.OwnerUserId == ownerUserId));
        }

        public Task<IReadOnlyList<Document>> ListAsync(
            string ownerUserId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Document>>(
                Documents.Where(document => document.OwnerUserId == ownerUserId).ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingBlobStorageService : IBlobStorageService
    {
        public string? UploadedBlobName { get; private set; }
        public string? UploadedContentType { get; private set; }

        public Task UploadAsync(
            string blobName,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            UploadedBlobName = blobName;
            UploadedContentType = contentType;
            return Task.CompletedTask;
        }

        public Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Stream>(new MemoryStream());
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
