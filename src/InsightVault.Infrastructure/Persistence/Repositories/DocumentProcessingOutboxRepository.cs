using InsightVault.Application.ProcessingQueue;
using InsightVault.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InsightVault.Infrastructure.Persistence.Repositories;

public sealed class DocumentProcessingOutboxRepository(ApplicationDbContext dbContext)
    : IDocumentProcessingOutboxRepository
{
    public Task AddAsync(DocumentProcessingOutboxEntry entry, CancellationToken cancellationToken = default)
    {
        return dbContext.DocumentProcessingOutboxEntries.AddAsync(entry, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<DocumentProcessingOutboxEntry>> ListPendingAsync(
        int maximumCount,
        CancellationToken cancellationToken = default)
    {
        if (maximumCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumCount));
        }

        return await dbContext.DocumentProcessingOutboxEntries
            .Where(entry => entry.DispatchedAtUtc == null)
            .OrderBy(entry => entry.CreatedAtUtc)
            .Take(maximumCount)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
