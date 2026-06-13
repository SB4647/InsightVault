using InsightVault.Application.Features.Documents.Processing.Commands;
using InsightVault.Application.Features.Documents.Processing.DTOs;
using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;

namespace InsightVault.Application.Features.Documents.Processing;

public sealed class DocumentProcessingService(
    IDocumentRepository documentRepository,
    IBlobStorageService blobStorageService,
    ITextExtractionService textExtractionService,
    IDocumentChunkingService chunkingService,
    IEmbeddingService embeddingService) : IDocumentProcessingService
{
    public async Task<DocumentProcessingResultDto> ProcessAsync(
        ProcessDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        var document = await documentRepository.GetByIdAsync(command.DocumentId, cancellationToken)
            ?? throw new InvalidOperationException($"Document '{command.DocumentId}' was not found.");

        document.StartProcessing();

        try
        {
            await using var fileStream = await blobStorageService.DownloadAsync(document.BlobName, cancellationToken);
            var extractedText = await textExtractionService.ExtractTextAsync(fileStream, cancellationToken);
            var textChunks = chunkingService.Chunk(extractedText, command.ChunkSize, command.OverlapSize);

            if (textChunks.Count == 0)
            {
                throw new InvalidOperationException("No text could be extracted from the document.");
            }

            var chunks = new List<DocumentChunk>();
            foreach (var textChunk in textChunks)
            {
                var chunk = DocumentChunk.Create(document.Id, textChunk.ChunkIndex, textChunk.Text);
                var vector = await embeddingService.GenerateEmbeddingAsync(textChunk.Text, cancellationToken);
                chunk.SetEmbedding(vector);
                chunks.Add(chunk);
            }

            document.CompleteProcessing(chunks);
            await documentRepository.SaveChangesAsync(cancellationToken);

            return new DocumentProcessingResultDto(
                document.Id,
                chunks.Count,
                document.Status.ToString());
        }
        catch
        {
            document.MarkProcessingFailed();
            await documentRepository.SaveChangesAsync(cancellationToken);
            throw;
        }
    }
}
