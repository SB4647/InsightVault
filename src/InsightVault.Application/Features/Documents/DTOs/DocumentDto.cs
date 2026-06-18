namespace InsightVault.Application.Features.Documents.DTOs;

public sealed record DocumentDto(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeInBytes,
    DateTime UploadedAtUtc,
    string Status,
    int ChunkCount,
    bool IsOwner,
    string AccessLevel);
