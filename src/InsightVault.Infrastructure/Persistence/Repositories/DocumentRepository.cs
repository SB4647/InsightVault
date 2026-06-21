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
            .Include(document => document.Permissions)
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
            .Include(document => document.Permissions)
            .Where(document =>
                document.OwnerUserId == ownerUserId ||
                document.Permissions.Any(permission => permission.UserId == ownerUserId))
            .OrderByDescending(document => document.UploadedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ReplaceChunksAsync(
        Document document,
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken = default)
    {
        if (chunks.Count == 0)
        {
            throw new ArgumentException("At least one chunk is required.", nameof(chunks));
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            DetachTrackedChunks(document.Id);
            dbContext.Entry(document).State = EntityState.Detached;

            await dbContext.Embeddings
                .Where(embedding => dbContext.DocumentChunks
                    .Where(chunk => chunk.DocumentId == document.Id)
                    .Select(chunk => chunk.Id)
                    .Contains(embedding.DocumentChunkId))
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.DocumentChunks
                .Where(chunk => chunk.DocumentId == document.Id)
                .ExecuteDeleteAsync(cancellationToken);

            var trackedDocument = await dbContext.Documents
                .SingleAsync(existingDocument => existingDocument.Id == document.Id, cancellationToken);

            trackedDocument.CompleteProcessing(chunks);
            MarkChunksAsAdded(chunks);
            await dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });

        document.CompleteProcessing(chunks);
    }

    public void Remove(Document document)
    {
        dbContext.Documents.Remove(document);
    }

    public async Task<IReadOnlyList<Document>> ListProcessedDocumentsAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Documents
            .AsNoTracking()
            .Include(document => document.Permissions)
            .Include(document => document.Chunks)
            .ThenInclude(chunk => chunk.Embedding)
            .Where(document =>
                (document.OwnerUserId == ownerUserId ||
                 document.Permissions.Any(permission => permission.UserId == ownerUserId)) &&
                document.Status == DocumentProcessingStatus.Processed)
            .ToListAsync(cancellationToken);
    }

    private void DetachTrackedChunks(Guid documentId)
    {
        var trackedChunkIds = dbContext.ChangeTracker
            .Entries<DocumentChunk>()
            .Where(entry => entry.Entity.DocumentId == documentId)
            .Select(entry => entry.Entity.Id)
            .ToHashSet();

        foreach (var entry in dbContext.ChangeTracker.Entries<Embedding>()
                     .Where(entry => trackedChunkIds.Contains(entry.Entity.DocumentChunkId)))
        {
            entry.State = EntityState.Detached;
        }

        foreach (var entry in dbContext.ChangeTracker.Entries<DocumentChunk>()
                     .Where(entry => entry.Entity.DocumentId == documentId))
        {
            entry.State = EntityState.Detached;
        }
    }

    private void MarkChunksAsAdded(IEnumerable<DocumentChunk> chunks)
    {
        foreach (var chunk in chunks)
        {
            dbContext.Entry(chunk).State = EntityState.Added;

            if (chunk.Embedding is not null)
            {
                dbContext.Entry(chunk.Embedding).State = EntityState.Added;
            }
        }
    }
}
