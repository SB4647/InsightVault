namespace InsightVault.Application.Features.Documents.Processing;

public interface IDocumentChunkingService
{
    IReadOnlyList<DocumentTextChunk> Chunk(string text, int chunkSize, int overlapSize);
}
