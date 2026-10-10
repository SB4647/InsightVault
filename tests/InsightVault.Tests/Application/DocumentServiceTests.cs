using InsightVault.Application.Features.Documents;
using InsightVault.Application.Features.Documents.Commands;
using InsightVault.Application.Interfaces;
using InsightVault.Application.ProcessingQueue;
using InsightVault.Domain.Entities;
using InsightVault.Domain.Enums;

namespace InsightVault.Tests.Application;

public class DocumentServiceTests
{
    [Fact]
    public void DocumentDto_DoesNotExposeBlobName()
    {
        var property = typeof(InsightVault.Application.Features.Documents.DTOs.DocumentDto)
            .GetProperty("BlobName");

        Assert.Null(property);
    }

    [Fact]
    public async Task UploadAsync_StoresBlobAndSavesDocumentMetadata()
    {
        var repository = new InMemoryDocumentRepository();
        var blobStorage = new RecordingBlobStorageService();
        var timeProvider = new FixedTimeProvider(new DateTimeOffset(2026, 6, 12, 10, 30, 0, TimeSpan.Zero));
        var service = new DocumentService(
            repository,
            blobStorage,
            timeProvider,
            new StubUserLookupService(),
            new RecordingOutboxRepository());
        await using var content = new MemoryStream([1, 2, 3]);
        var command = new UploadDocumentCommand("Report.pdf", "application/pdf", 3, content, "user-1");

        var result = await service.UploadAsync(command);

        Assert.Equal("Report.pdf", result.OriginalFileName);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(3, result.SizeInBytes);
        Assert.Equal(0, result.ChunkCount);
        Assert.Equal(new DateTime(2026, 6, 12, 10, 30, 0, DateTimeKind.Utc), result.UploadedAtUtc);
        Assert.NotNull(blobStorage.UploadedBlobName);
        Assert.StartsWith("documents/", blobStorage.UploadedBlobName);
        Assert.EndsWith(".pdf", blobStorage.UploadedBlobName);
        Assert.Equal("application/pdf", blobStorage.UploadedContentType);
        Assert.Single(repository.Documents);
        Assert.Equal(1, repository.SaveChangesCallCount);
        Assert.Equal("user-1", repository.Documents.Single().OwnerUserId);
    }

    [Fact]
    public async Task UploadAsync_CreatesOnePendingProcessingOutboxEntry()
    {
        var repository = new InMemoryDocumentRepository();
        var outboxRepository = new RecordingOutboxRepository();
        var service = new DocumentService(
            repository,
            new RecordingBlobStorageService(),
            TimeProvider.System,
            new StubUserLookupService(),
            outboxRepository);
        await using var content = new MemoryStream([1, 2, 3]);

        var result = await service.UploadAsync(
            new UploadDocumentCommand("report.pdf", "application/pdf", 3, content, "user-1"));

        var entry = Assert.Single(outboxRepository.Entries);
        Assert.Equal(result.Id, entry.DocumentId);
        Assert.Equal("user-1", entry.OwnerUserId);
        Assert.Null(entry.DispatchedAtUtc);
    }

    [Fact]
    public async Task UploadAsync_WhenOwnerReachedDocumentQuota_RejectsBeforeStoringTheBlob()
    {
        var repository = new InMemoryDocumentRepository();
        for (var index = 0; index < 100; index++)
        {
            repository.Documents.Add(CreateDocument("owner-1"));
        }

        var blobStorage = new RecordingBlobStorageService();
        var service = new DocumentService(
            repository,
            blobStorage,
            TimeProvider.System,
            new StubUserLookupService(),
            new RecordingOutboxRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(
            new UploadDocumentCommand(
                "quota.pdf",
                "application/pdf",
                100,
                new MemoryStream([1]),
                "owner-1")));

        Assert.Null(blobStorage.UploadedBlobName);
        Assert.Equal(100, repository.Documents.Count);
    }

    [Fact]
    public async Task UploadAsync_ConcurrentRequests_CannotBothConsumeTheFinalDocumentQuotaSlot()
    {
        var repository = new InMemoryDocumentRepository();
        var blobStorage = new FirstUploadBlockingBlobStorageService();
        var service = new DocumentService(
            repository,
            blobStorage,
            TimeProvider.System,
            new StubUserLookupService(),
            new RecordingOutboxRepository(),
            new UploadQuotaOptions
            {
                MaxDocumentsPerOwner = 1,
                MaxStoredBytesPerOwner = 1_000
            });

        var firstUpload = service.UploadAsync(
            new UploadDocumentCommand("first.pdf", "application/pdf", 100, new MemoryStream([1]), "owner-1"));
        await blobStorage.FirstUploadStarted;

        var secondUpload = service.UploadAsync(
            new UploadDocumentCommand("second.pdf", "application/pdf", 100, new MemoryStream([2]), "owner-1"));

        var completedBeforeTheFirstUploadWasReleased = await Task.WhenAny(secondUpload, Task.Delay(250));

        Assert.NotSame(secondUpload, completedBeforeTheFirstUploadWasReleased);

        blobStorage.AllowFirstUpload();
        await firstUpload;
        await Assert.ThrowsAsync<InvalidOperationException>(() => secondUpload);
        Assert.Single(repository.Documents);
    }

    [Fact]
    public async Task UploadAsync_WhenMetadataPersistenceFails_DeletesTheUploadedBlob()
    {
        var repository = new InMemoryDocumentRepository { ThrowOnSaveChanges = true };
        var blobStorage = new RecordingBlobStorageService();
        var service = new DocumentService(
            repository,
            blobStorage,
            TimeProvider.System,
            new StubUserLookupService(),
            new RecordingOutboxRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(
            new UploadDocumentCommand("failed.pdf", "application/pdf", 100, new MemoryStream([1]), "owner-1")));

        Assert.NotNull(blobStorage.UploadedBlobName);
        Assert.Equal(blobStorage.UploadedBlobName, blobStorage.DeletedBlobName);
    }

    [Fact]
    public async Task RetryProcessingAsync_ForFailedOwnedDocument_ResetsStatusAndCreatesOutboxEntry()
    {
        var document = CreateDocument("owner-1");
        document.MarkProcessingFailed();
        var repository = new InMemoryDocumentRepository();
        repository.Documents.Add(document);
        var outboxRepository = new RecordingOutboxRepository();
        var service = new DocumentService(
            repository,
            new RecordingBlobStorageService(),
            TimeProvider.System,
            new StubUserLookupService(),
            outboxRepository);

        var result = await service.RetryProcessingAsync(
            new RetryDocumentProcessingCommand(document.Id, "owner-1"));

        Assert.Equal("Uploaded", result.Status);
        Assert.Equal(DocumentProcessingStatus.Uploaded, document.Status);
        var entry = Assert.Single(outboxRepository.Entries);
        Assert.Equal(document.Id, entry.DocumentId);
        Assert.Equal("owner-1", entry.OwnerUserId);
    }

    [Fact]
    public async Task RetryProcessingAsync_ForNonFailedOrNonOwnedDocument_ThrowsInvalidOperationException()
    {
        var uploaded = CreateDocument("owner-1");
        var failed = CreateDocument("owner-2");
        failed.MarkProcessingFailed();
        var repository = new InMemoryDocumentRepository();
        repository.Documents.AddRange([uploaded, failed]);
        var service = new DocumentService(
            repository,
            new RecordingBlobStorageService(),
            TimeProvider.System,
            new StubUserLookupService(),
            new RecordingOutboxRepository());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RetryProcessingAsync(new RetryDocumentProcessingCommand(uploaded.Id, "owner-1")));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RetryProcessingAsync(new RetryDocumentProcessingCommand(failed.Id, "owner-1")));
    }

    [Theory]
    [InlineData("Report.txt", "application/pdf", 3, "Only PDF files can be uploaded.")]
    [InlineData("Report.pdf", "text/plain", 3, "Only application/pdf files can be uploaded.")]
    [InlineData("Report.pdf", "application/pdf", 0, "File cannot be empty.")]
    [InlineData("Report.pdf", "application/pdf", 25_000_001, "File cannot be larger than 25 MB.")]
    public async Task UploadAsync_WithInvalidFile_ThrowsArgumentException(
        string fileName,
        string contentType,
        long sizeInBytes,
        string expectedMessage)
    {
        var repository = new InMemoryDocumentRepository();
        var blobStorage = new RecordingBlobStorageService();
        var service = new DocumentService(
            repository,
            blobStorage,
            TimeProvider.System,
            new StubUserLookupService());
        await using var content = new MemoryStream([1, 2, 3]);
        var command = new UploadDocumentCommand(fileName, contentType, sizeInBytes, content, "user-1");

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.UploadAsync(command));

        Assert.Contains(expectedMessage, exception.Message);
        Assert.Null(blobStorage.UploadedBlobName);
        Assert.Empty(repository.Documents);
        Assert.Equal(0, repository.SaveChangesCallCount);
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
        var service = new DocumentService(
            repository,
            new RecordingBlobStorageService(),
            TimeProvider.System,
            new StubUserLookupService());

        var documents = await service.GetDocumentsAsync("user-1");

        Assert.Collection(
            documents,
            first => Assert.Equal("newer.pdf", first.OriginalFileName),
            second => Assert.Equal("older.pdf", second.OriginalFileName));
    }

    [Fact]
    public async Task GetDocumentsAsync_ReturnsOwnedAndSharedDocuments()
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
        other.ShareWithViewer("user-1");
        repository.Documents.Add(owned);
        repository.Documents.Add(other);
        var service = new DocumentService(
            repository,
            new RecordingBlobStorageService(),
            TimeProvider.System,
            new StubUserLookupService());

        var documents = await service.GetDocumentsAsync("user-1");

        Assert.Collection(
            documents,
            first =>
            {
                Assert.Equal("other.pdf", first.OriginalFileName);
                Assert.False(first.IsOwner);
                Assert.Equal("Viewer", first.AccessLevel);
            },
            second =>
            {
                Assert.Equal("owned.pdf", second.OriginalFileName);
                Assert.True(second.IsOwner);
                Assert.Equal("Owner", second.AccessLevel);
            });
    }

    [Fact]
    public async Task ShareDocumentAsync_WhenOwnerSharesWithExistingUser_AddsViewerPermission()
    {
        var repository = new InMemoryDocumentRepository();
        var document = Document.Create(
            "owned.pdf",
            "application/pdf",
            100,
            "documents/owned.pdf",
            new DateTime(2026, 6, 12, 10, 0, 0, DateTimeKind.Utc),
            "owner-1");
        repository.Documents.Add(document);
        var service = new DocumentService(
            repository,
            new RecordingBlobStorageService(),
            TimeProvider.System,
            new StubUserLookupService(("viewer@example.com", "viewer-1")));

        var result = await service.ShareDocumentAsync(
            new ShareDocumentCommand(document.Id, "owner-1", "viewer@example.com"));

        Assert.Equal(document.Id, result.DocumentId);
        Assert.Equal("viewer-1", result.SharedWithUserId);
        Assert.Equal("viewer@example.com", result.SharedWithEmail);
        Assert.Equal("Viewer", result.AccessLevel);
        Assert.Contains(document.Permissions, permission => permission.UserId == "viewer-1");
        Assert.Equal(1, repository.AddPermissionCallCount);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ShareDocumentAsync_WhenUserAlreadyHasAccess_DoesNotAddDuplicatePermission()
    {
        var repository = new InMemoryDocumentRepository();
        var document = Document.Create(
            "owned.pdf",
            "application/pdf",
            100,
            "documents/owned.pdf",
            new DateTime(2026, 6, 12, 10, 0, 0, DateTimeKind.Utc),
            "owner-1");
        document.ShareWithViewer("viewer-1");
        repository.Documents.Add(document);
        var service = new DocumentService(
            repository,
            new RecordingBlobStorageService(),
            TimeProvider.System,
            new StubUserLookupService(("viewer@example.com", "viewer-1")));

        var result = await service.ShareDocumentAsync(
            new ShareDocumentCommand(document.Id, "owner-1", "viewer@example.com"));

        Assert.Equal(document.Id, result.DocumentId);
        Assert.Equal("viewer-1", result.SharedWithUserId);
        Assert.Equal("viewer@example.com", result.SharedWithEmail);
        Assert.Equal("Viewer", result.AccessLevel);
        Assert.Single(document.Permissions);
        Assert.Equal(0, repository.AddPermissionCallCount);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ShareDocumentAsync_WhenDocumentIsNotOwnedByUser_ThrowsInvalidOperationException()
    {
        var repository = new InMemoryDocumentRepository();
        var document = Document.Create(
            "owned.pdf",
            "application/pdf",
            100,
            "documents/owned.pdf",
            new DateTime(2026, 6, 12, 10, 0, 0, DateTimeKind.Utc),
            "owner-1");
        repository.Documents.Add(document);
        var service = new DocumentService(
            repository,
            new RecordingBlobStorageService(),
            TimeProvider.System,
            new StubUserLookupService(("viewer@example.com", "viewer-1")));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ShareDocumentAsync(new ShareDocumentCommand(document.Id, "user-2", "viewer@example.com")));
    }

    [Fact]
    public async Task DeleteDocumentAsync_WhenOwnerDeletesDocument_RemovesBlobAndDocumentMetadata()
    {
        var repository = new InMemoryDocumentRepository();
        var blobStorage = new RecordingBlobStorageService();
        var document = Document.Create(
            "owned.pdf",
            "application/pdf",
            100,
            "documents/owned.pdf",
            new DateTime(2026, 6, 12, 10, 0, 0, DateTimeKind.Utc),
            "owner-1");
        repository.Documents.Add(document);
        var service = new DocumentService(
            repository,
            blobStorage,
            TimeProvider.System,
            new StubUserLookupService());

        await service.DeleteDocumentAsync(new DeleteDocumentCommand(document.Id, "owner-1"));

        Assert.Equal("documents/owned.pdf", blobStorage.DeletedBlobName);
        Assert.Empty(repository.Documents);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task DeleteDocumentAsync_WhenDocumentIsNotOwnedByUser_ThrowsInvalidOperationException()
    {
        var repository = new InMemoryDocumentRepository();
        var blobStorage = new RecordingBlobStorageService();
        var document = Document.Create(
            "owned.pdf",
            "application/pdf",
            100,
            "documents/owned.pdf",
            new DateTime(2026, 6, 12, 10, 0, 0, DateTimeKind.Utc),
            "owner-1");
        repository.Documents.Add(document);
        var service = new DocumentService(
            repository,
            blobStorage,
            TimeProvider.System,
            new StubUserLookupService());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteDocumentAsync(new DeleteDocumentCommand(document.Id, "viewer-1")));

        Assert.Null(blobStorage.DeletedBlobName);
        Assert.Single(repository.Documents);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    private sealed class InMemoryDocumentRepository : IDocumentRepository
    {
        private readonly SemaphoreSlim _writeLock = new(1, 1);

        public List<Document> Documents { get; } = [];
        public int AddPermissionCallCount { get; private set; }
        public int SaveChangesCallCount { get; private set; }
        public bool ThrowOnSaveChanges { get; init; }

        public async Task<T> ExecuteSerializableAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            await _writeLock.WaitAsync(cancellationToken);
            try
            {
                return await operation(cancellationToken);
            }
            finally
            {
                _writeLock.Release();
            }
        }

        public Task<DocumentUsage> GetOwnedUsageAsync(
            string ownerUserId,
            CancellationToken cancellationToken = default)
        {
            var ownedDocuments = Documents.Where(document => document.OwnerUserId == ownerUserId).ToList();
            return Task.FromResult(new DocumentUsage(
                ownedDocuments.Count,
                ownedDocuments.Sum(document => document.SizeInBytes)));
        }

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
                Documents
                    .Where(document =>
                        document.OwnerUserId == ownerUserId ||
                        document.Permissions.Any(permission => permission.UserId == ownerUserId))
                    .ToList());
        }

        public void Remove(Document document)
        {
            Documents.Remove(document);
        }

        public Task ReplaceChunksAsync(
            Document document,
            IReadOnlyList<DocumentChunk> chunks,
            CancellationToken cancellationToken = default)
        {
            document.CompleteProcessing(chunks);
            return Task.CompletedTask;
        }

        public void AddPermission(DocumentPermission permission)
        {
            AddPermissionCallCount++;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            if (ThrowOnSaveChanges)
            {
                throw new InvalidOperationException("Simulated persistence failure.");
            }

            return Task.CompletedTask;
        }
    }

    private static Document CreateDocument(string ownerUserId)
    {
        return Document.Create(
            "report.pdf",
            "application/pdf",
            100,
            $"documents/{Guid.NewGuid():N}.pdf",
            new DateTime(2026, 6, 12, 10, 0, 0, DateTimeKind.Utc),
            ownerUserId);
    }

    private sealed class RecordingOutboxRepository : IDocumentProcessingOutboxRepository
    {
        public List<DocumentProcessingOutboxEntry> Entries { get; } = [];

        public Task AddAsync(DocumentProcessingOutboxEntry entry, CancellationToken cancellationToken = default)
        {
            Entries.Add(entry);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<DocumentProcessingOutboxEntry>> ListPendingAsync(
            int maximumCount,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DocumentProcessingOutboxEntry>>(
                Entries.Where(entry => entry.DispatchedAtUtc is null).Take(maximumCount).ToList());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingBlobStorageService : IBlobStorageService
    {
        public string? UploadedBlobName { get; private set; }
        public string? UploadedContentType { get; private set; }
        public string? DeletedBlobName { get; private set; }

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

        public Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
        {
            DeletedBlobName = blobName;
            return Task.CompletedTask;
        }
    }

    private sealed class FirstUploadBlockingBlobStorageService : IBlobStorageService
    {
        private readonly TaskCompletionSource _firstUploadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _allowFirstUpload = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _uploadCount;

        public Task FirstUploadStarted => _firstUploadStarted.Task;

        public async Task UploadAsync(
            string blobName,
            Stream content,
            string contentType,
            CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _uploadCount) != 1)
            {
                return;
            }

            _firstUploadStarted.TrySetResult();
            await _allowFirstUpload.Task.WaitAsync(cancellationToken);
        }

        public Task<Stream> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Stream>(new MemoryStream());
        }

        public Task DeleteAsync(string blobName, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void AllowFirstUpload() => _allowFirstUpload.TrySetResult();
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubUserLookupService(params (string Email, string UserId)[] users) : IUserLookupService
    {
        public Task<UserLookupResult?> FindByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            var user = users.SingleOrDefault(user =>
                string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase));

            return Task.FromResult(user.UserId is null ? null : new UserLookupResult(user.UserId, user.Email));
        }
    }
}
