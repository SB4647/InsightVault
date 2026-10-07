namespace InsightVault.Domain.Entities;

/// <summary>
/// Represents a durable request to publish a document-processing job after the document is saved.
/// </summary>
public sealed class DocumentProcessingOutboxEntry
{
    private DocumentProcessingOutboxEntry()
    {
    }

    private DocumentProcessingOutboxEntry(Guid documentId, string ownerUserId, DateTime createdAtUtc)
    {
        Id = Guid.NewGuid();
        DocumentId = documentId;
        OwnerUserId = ownerUserId;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public string OwnerUserId { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? DispatchedAtUtc { get; private set; }
    public DateTime? LastDispatchAttemptAtUtc { get; private set; }
    public int DispatchAttemptCount { get; private set; }
    public Document? Document { get; private set; }

    /// <summary>
    /// Creates a pending durable request for a document-processing queue message.
    /// </summary>
    public static DocumentProcessingOutboxEntry Create(Guid documentId, string ownerUserId, DateTime createdAtUtc)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("Document id is required.", nameof(documentId));
        }

        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            throw new ArgumentException("Owner user id is required.", nameof(ownerUserId));
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Creation timestamp must be UTC.", nameof(createdAtUtc));
        }

        return new DocumentProcessingOutboxEntry(documentId, ownerUserId.Trim(), createdAtUtc);
    }

    /// <summary>
    /// Records a publish attempt so a transient queue outage remains observable and retryable.
    /// </summary>
    public void RecordDispatchAttempt(DateTime attemptedAtUtc)
    {
        if (attemptedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Attempt timestamp must be UTC.", nameof(attemptedAtUtc));
        }

        DispatchAttemptCount++;
        LastDispatchAttemptAtUtc = attemptedAtUtc;
    }

    /// <summary>
    /// Marks the entry as published only after the queue transport has accepted the job.
    /// </summary>
    public void MarkDispatched(DateTime dispatchedAtUtc)
    {
        if (dispatchedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Dispatch timestamp must be UTC.", nameof(dispatchedAtUtc));
        }

        DispatchedAtUtc = dispatchedAtUtc;
    }
}
