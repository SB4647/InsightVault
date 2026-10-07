namespace InsightVault.Application.Features.Chat.DTOs;

public sealed record SourceCitationDto(
    Guid DocumentId,
    string DocumentName,
    Guid ChunkId,
    int ChunkIndex,
    string Text,
    double Score,
    int DocumentVersion = 1,
    int SourcePageNumber = 1,
    string? SectionTitle = null,
    int Rank = 0);
