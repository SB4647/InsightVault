using InsightVault.Domain.Enums;

namespace InsightVault.Domain.Entities;

public class Document
{
    private Document()
    {
    }

    private Document(
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string blobName,
        DateTime uploadedAtUtc)
    {
        Id = Guid.NewGuid();
        OriginalFileName = originalFileName;
        ContentType = contentType;
        SizeInBytes = sizeInBytes;
        BlobName = blobName;
        UploadedAtUtc = uploadedAtUtc;
        Status = DocumentProcessingStatus.Uploaded;
    }

    public Guid Id { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeInBytes { get; private set; }
    public string BlobName { get; private set; } = string.Empty;
    public DateTime UploadedAtUtc { get; private set; }
    public DocumentProcessingStatus Status { get; private set; }

    public static Document Create(
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string blobName,
        DateTime uploadedAtUtc)
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

        return new Document(
            originalFileName.Trim(),
            contentType.Trim(),
            sizeInBytes,
            blobName.Trim(),
            uploadedAtUtc);
    }
}
