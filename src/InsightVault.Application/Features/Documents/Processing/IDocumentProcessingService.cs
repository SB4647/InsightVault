using InsightVault.Application.Features.Documents.Processing.Commands;
using InsightVault.Application.Features.Documents.Processing.DTOs;

namespace InsightVault.Application.Features.Documents.Processing;

public interface IDocumentProcessingService
{
    Task<DocumentProcessingResultDto> ProcessAsync(
        ProcessDocumentCommand command,
        CancellationToken cancellationToken = default);
}
