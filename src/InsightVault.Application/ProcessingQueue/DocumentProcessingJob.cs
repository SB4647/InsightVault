namespace InsightVault.Application.ProcessingQueue;

/// <summary>
/// Versioned, secret-free payload for a durable document-processing queue message.
/// </summary>
public sealed record DocumentProcessingJob(int Version, Guid OutboxEntryId, Guid DocumentId, string OwnerUserId)
{
    public const int CurrentVersion = 1;
}

/// <summary>
/// Represents a queue message together with the receipt required to acknowledge it.
/// </summary>
public sealed record ReceivedDocumentProcessingJob(DocumentProcessingJob Job, string ReceiptHandle);
