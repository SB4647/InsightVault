namespace InsightVault.Application.Features.Documents.Processing;

/// <summary>
/// Represents a text chunk together with the first source page and optional section that produced it.
/// </summary>
public sealed record DocumentTextChunk(int ChunkIndex, string Text, int SourcePageNumber, string? SectionTitle);
