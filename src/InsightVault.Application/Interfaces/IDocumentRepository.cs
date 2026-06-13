using InsightVault.Domain.Entities;

namespace InsightVault.Application.Interfaces;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken = default);

    Task<Document?> GetByIdAsync(
        Guid id,
        string ownerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Document>> ListAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
