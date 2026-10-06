namespace InsightVault.Application.ProcessingQueue;

/// <summary>
/// Publishes pending outbox entries to the configured durable processing queue.
/// </summary>
public interface IProcessingOutboxDispatcher
{
    /// <summary>Attempts to dispatch a bounded batch of pending entries.</summary>
    Task DispatchPendingAsync(CancellationToken cancellationToken = default);
}
