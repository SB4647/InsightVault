namespace InsightVault.Application.Features.Chat.DTOs;

public sealed record ChatResponseDto(
    string Answer,
    IReadOnlyList<SourceCitationDto> Sources);
