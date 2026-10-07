using InsightVault.Domain.Entities;

namespace InsightVault.Application.ProcessingQueue;

/// <summary>
/// Persists pending document-processing publications alongside documents.
/// </summary>
public interface IDocumentProcessingOutboxRepository
{
    /// <summary>Adds a pending outbox entry to the current persistence unit of work.</summary>
    Task AddAsync(DocumentProcessingOutboxEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Lists pending entries in creation order for queue dispatch.</summary>
    Task<IReadOnlyList<DocumentProcessingOutboxEntry>> ListPendingAsync(
        int maximumCount,
        CancellationToken cancellationToken = default);

    /// <summary>Saves queued outbox changes in the surrounding persistence context.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
