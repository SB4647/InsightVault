using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;
using InsightVault.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InsightVault.Infrastructure.Persistence.Repositories;

public sealed class DocumentRepository(ApplicationDbContext dbContext) : IDocumentRepository, IDocumentSearchRepository
{
    public async Task AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        await dbContext.Documents.AddAsync(document, cancellationToken);
    }

    public async Task<Document?> GetByIdAsync(
        Guid id,
        string ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Documents
            .Include(document => document.Chunks)
            .ThenInclude(chunk => chunk.Embedding)
            .SingleOrDefaultAsync(
                document => document.Id == id && document.OwnerUserId == ownerUserId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> ListAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .Include(document => document.Chunks)
            .Where(document => document.OwnerUserId == ownerUserId)
            .OrderByDescending(document => document.UploadedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Document>> ListProcessedDocumentsAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .Include(document => document.Chunks)
            .ThenInclude(chunk => chunk.Embedding)
            .Where(document =>
                document.OwnerUserId == ownerUserId &&
                document.Status == DocumentProcessingStatus.Processed)
            .ToListAsync(cancellationToken);
    }
}
