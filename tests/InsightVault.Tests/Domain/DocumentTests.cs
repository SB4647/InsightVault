using InsightVault.Domain.Entities;
using InsightVault.Domain.Enums;

namespace InsightVault.Tests.Domain;

public class DocumentTests
{
    [Fact]
    public void Create_WithValidMetadata_ReturnsUploadedDocument()
    {
        var uploadedAt = new DateTime(2026, 6, 12, 10, 30, 0, DateTimeKind.Utc);

        var document = Document.Create(
            "requirements.pdf",
            "application/pdf",
            1024,
            "documents/requirements.pdf",
            uploadedAt);

        Assert.Equal("requirements.pdf", document.OriginalFileName);
        Assert.Equal("application/pdf", document.ContentType);
        Assert.Equal(1024, document.SizeInBytes);
        Assert.Equal("documents/requirements.pdf", document.BlobName);
        Assert.Equal(uploadedAt, document.UploadedAtUtc);
        Assert.Equal(DocumentProcessingStatus.Uploaded, document.Status);
    }

    [Theory]
    [InlineData("", "application/pdf", 1024, "documents/file.pdf")]
    [InlineData("file.pdf", "", 1024, "documents/file.pdf")]
    [InlineData("file.pdf", "application/pdf", 0, "documents/file.pdf")]
    [InlineData("file.pdf", "application/pdf", 1024, "")]
    public void Create_WithInvalidMetadata_ThrowsArgumentException(
        string originalFileName,
        string contentType,
        long sizeInBytes,
        string blobName)
    {
        var uploadedAt = new DateTime(2026, 6, 12, 10, 30, 0, DateTimeKind.Utc);

        Assert.Throws<ArgumentException>(() => Document.Create(
            originalFileName,
            contentType,
            sizeInBytes,
            blobName,
            uploadedAt));
    }
}
