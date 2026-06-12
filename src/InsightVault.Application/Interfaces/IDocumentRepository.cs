using InsightVault.Domain.Entities;

namespace InsightVault.Application.Interfaces;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Document>> ListAsync(CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
