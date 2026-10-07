using InsightVault.Application.Interfaces;

namespace InsightVault.Application.Features.Documents.Processing;

public interface IDocumentChunkingService
{
    IReadOnlyList<DocumentTextChunk> Chunk(string text, int chunkSize, int overlapSize);

    /// <summary>
    /// Splits page-aware extraction output while preserving each chunk's source location.
    /// </summary>
    IReadOnlyList<DocumentTextChunk> Chunk(
        IReadOnlyList<ExtractedDocumentPage> pages,
        int chunkSize,
        int overlapSize);
}
