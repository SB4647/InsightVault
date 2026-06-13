namespace InsightVault.Application.Features.Documents.Processing.DTOs;

public sealed record DocumentProcessingResultDto(
    Guid DocumentId,
    int ChunkCount,
    string Status);
