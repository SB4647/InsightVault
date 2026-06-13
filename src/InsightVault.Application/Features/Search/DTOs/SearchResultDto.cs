namespace InsightVault.Application.Features.Search.DTOs;

public sealed record SearchResultDto(
    Guid DocumentId,
    string DocumentName,
    Guid ChunkId,
    int ChunkIndex,
    string Text,
    double Score);
