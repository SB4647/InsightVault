
using InsightVault.Application.Features.Documents.Commands;
using InsightVault.Application.Features.Documents.DTOs;

namespace InsightVault.Application.Features.Documents;

public interface IDocumentService
{
    Task<DocumentDto> UploadAsync(
        UploadDocumentCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default);

    Task<DocumentShareDto> ShareDocumentAsync(
        ShareDocumentCommand command,
        CancellationToken cancellationToken = default);
}
