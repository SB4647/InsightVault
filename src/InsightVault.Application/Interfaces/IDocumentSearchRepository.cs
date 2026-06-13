using InsightVault.Domain.Entities;

namespace InsightVault.Application.Interfaces;

public interface IDocumentSearchRepository
{
    Task<IReadOnlyList<Document>> ListProcessedDocumentsAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default);
}
