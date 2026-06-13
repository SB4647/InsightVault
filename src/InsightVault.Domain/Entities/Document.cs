using InsightVault.Domain.Enums;

namespace InsightVault.Domain.Entities;

public class Document
{
    private readonly List<DocumentChunk> _chunks = [];
    private readonly List<DocumentPermission> _permissions = [];

    private Document()
    {
    }

    private Document(
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string blobName,
        DateTime uploadedAtUtc,
        string ownerUserId)
    {
        Id = Guid.NewGuid();
        OriginalFileName = originalFileName;
        ContentType = contentType;
        SizeInBytes = sizeInBytes;
        BlobName = blobName;
        UploadedAtUtc = uploadedAtUtc;
        OwnerUserId = ownerUserId;
        Status = DocumentProcessingStatus.Uploaded;
    }

    public Guid Id { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeInBytes { get; private set; }
    public string BlobName { get; private set; } = string.Empty;
    public DateTime UploadedAtUtc { get; private set; }
    public string OwnerUserId { get; private set; } = string.Empty;
    public DocumentProcessingStatus Status { get; private set; }
    public IReadOnlyCollection<DocumentChunk> Chunks => _chunks.AsReadOnly();
    public IReadOnlyCollection<DocumentPermission> Permissions => _permissions.AsReadOnly();

    public static Document Create(
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string blobName,
        DateTime uploadedAtUtc,
        string ownerUserId)
    {
        if (string.IsNullOrWhiteSpace(originalFileName))
        {
            throw new ArgumentException("Original file name is required.", nameof(originalFileName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        if (sizeInBytes <= 0)
        {
            throw new ArgumentException("File size must be greater than zero.", nameof(sizeInBytes));
        }

        if (string.IsNullOrWhiteSpace(blobName))
        {
            throw new ArgumentException("Blob name is required.", nameof(blobName));
        }

        if (uploadedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Upload timestamp must be UTC.", nameof(uploadedAtUtc));
        }

        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            throw new ArgumentException("Owner user id is required.", nameof(ownerUserId));
        }

        return new Document(
            originalFileName.Trim(),
            contentType.Trim(),
            sizeInBytes,
            blobName.Trim(),
            uploadedAtUtc,
            ownerUserId.Trim());
    }

    public void StartProcessing()
    {
        Status = DocumentProcessingStatus.Processing;
        _chunks.Clear();
    }

    public void CompleteProcessing(IEnumerable<DocumentChunk> chunks)
    {
        var chunkList = chunks.ToList();

        if (chunkList.Count == 0)
        {
            throw new ArgumentException("At least one chunk is required.", nameof(chunks));
        }

        _chunks.Clear();
        _chunks.AddRange(chunkList);
        Status = DocumentProcessingStatus.Processed;
    }

    public void MarkProcessingFailed()
    {
        Status = DocumentProcessingStatus.Failed;
    }

    public DocumentPermission ShareWithViewer(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        var normalizedUserId = userId.Trim();
        if (normalizedUserId == OwnerUserId)
        {
            throw new ArgumentException("Document owner already has access.", nameof(userId));
        }

        var existingPermission = _permissions.SingleOrDefault(permission => permission.UserId == normalizedUserId);
        if (existingPermission is not null)
        {
            return existingPermission;
        }

        var permission = DocumentPermission.CreateViewer(Id, normalizedUserId);
        _permissions.Add(permission);

        return permission;
    }
}
