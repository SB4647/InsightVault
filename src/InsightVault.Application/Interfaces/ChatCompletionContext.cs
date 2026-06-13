namespace InsightVault.Application.Interfaces;

public sealed record ChatCompletionContext(
    Guid DocumentId,
    string DocumentName,
    Guid ChunkId,
    int ChunkIndex,
    string Text,
    double Score);
