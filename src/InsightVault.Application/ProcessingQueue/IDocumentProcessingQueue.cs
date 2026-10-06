namespace InsightVault.Application.ProcessingQueue;

/// <summary>
/// Sends and receives durable document-processing jobs without coupling Application to a queue provider.
/// </summary>
public interface IDocumentProcessingQueue
{
    /// <summary>Publishes a processing job for asynchronous handling.</summary>
    Task SendAsync(DocumentProcessingJob job, CancellationToken cancellationToken = default);

    /// <summary>Long-polls for available processing jobs.</summary>
    Task<IReadOnlyList<ReceivedDocumentProcessingJob>> ReceiveAsync(
        int maximumMessages,
        TimeSpan waitTime,
        CancellationToken cancellationToken = default);

    /// <summary>Acknowledges a queue message after a successful or safely idempotent outcome.</summary>
    Task AcknowledgeAsync(string receiptHandle, CancellationToken cancellationToken = default);
}
