namespace InsightVault.Application.Features.Documents.DTOs;

public sealed record DocumentShareDto(
    Guid DocumentId,
    string SharedWithUserId,
    string SharedWithEmail,
    string AccessLevel);
