using InsightVault.Application.ProcessingQueue;

namespace InsightVault.Infrastructure.ProcessingQueue;

/// <summary>Prevents accidental processing when queue support has not been enabled.</summary>
public sealed class DisabledDocumentProcessingQueue : IDocumentProcessingQueue
{
    public Task SendAsync(DocumentProcessingJob job, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task<IReadOnlyList<ReceivedDocumentProcessingJob>> ReceiveAsync(int maximumMessages, TimeSpan waitTime, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ReceivedDocumentProcessingJob>>([]);
    public Task AcknowledgeAsync(string receiptHandle, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
