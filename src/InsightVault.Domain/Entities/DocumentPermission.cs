using InsightVault.Domain.Enums;

namespace InsightVault.Domain.Entities;

public class DocumentPermission
{
    private DocumentPermission()
    {
    }

    private DocumentPermission(Guid documentId, string userId, DocumentPermissionLevel level)
    {
        Id = Guid.NewGuid();
        DocumentId = documentId;
        UserId = userId;
        Level = level;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public DocumentPermissionLevel Level { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    public static DocumentPermission CreateViewer(Guid documentId, string userId)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("Document id is required.", nameof(documentId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        return new DocumentPermission(documentId, userId.Trim(), DocumentPermissionLevel.Viewer);
    }
}
