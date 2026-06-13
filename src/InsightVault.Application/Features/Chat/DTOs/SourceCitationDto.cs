namespace InsightVault.Application.Features.Chat.DTOs;

public sealed record SourceCitationDto(
    Guid DocumentId,
    string DocumentName,
    Guid ChunkId,
    int ChunkIndex,
    string Text,
    double Score);
