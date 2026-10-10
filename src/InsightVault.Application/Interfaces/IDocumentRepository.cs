using InsightVault.Domain.Entities;

namespace InsightVault.Application.Interfaces;

public interface IDocumentRepository
{
    /// <summary>Returns the count and total stored bytes for documents owned by a user.</summary>
    Task<DocumentUsage> GetOwnedUsageAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Document document, CancellationToken cancellationToken = default);

    Task<Document?> GetByIdAsync(
        Guid id,
        string ownerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Document>> ListAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default);

    Task ReplaceChunksAsync(
        Document document,
        IReadOnlyList<DocumentChunk> chunks,
        CancellationToken cancellationToken = default);

    void AddPermission(DocumentPermission permission);

    void Remove(Document document);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Represents the storage usage used to decide whether an owner may upload another document.</summary>
public sealed record DocumentUsage(int DocumentCount, long StoredBytes);
