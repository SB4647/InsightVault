using InsightVault.Domain.Entities;

namespace InsightVault.Application.ProcessingQueue;

/// <summary>
/// Publishes durable outbox entries and records the result after each attempt.
/// </summary>
public sealed class DocumentProcessingOutboxDispatcher(
    IDocumentProcessingOutboxRepository outboxRepository,
    IDocumentProcessingQueue documentProcessingQueue,
    TimeProvider timeProvider) : IProcessingOutboxDispatcher
{
    public async Task DispatchPendingAsync(CancellationToken cancellationToken = default)
    {
        var entries = await outboxRepository.ListPendingAsync(25, cancellationToken);
        foreach (var entry in entries)
        {
            entry.RecordDispatchAttempt(timeProvider.GetUtcNow().UtcDateTime);
            try
            {
                await documentProcessingQueue.SendAsync(
                    new DocumentProcessingJob(DocumentProcessingJob.CurrentVersion, entry.Id, entry.DocumentId, entry.OwnerUserId),
                    cancellationToken);
                entry.MarkDispatched(timeProvider.GetUtcNow().UtcDateTime);
            }
            catch
            {
                await outboxRepository.SaveChangesAsync(cancellationToken);
                throw;
            }
        }

        await outboxRepository.SaveChangesAsync(cancellationToken);
    }
}
