using InsightVault.Application.Features.Documents.Processing.Commands;
using InsightVault.Application.Features.Documents.Processing.DTOs;
using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;
using InsightVault.Domain.Enums;

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
        var document = await documentRepository.GetByIdAsync(
                command.DocumentId,
                command.OwnerUserId,
                cancellationToken)
            ?? throw new InvalidOperationException($"Document '{command.DocumentId}' was not found.");

        if (document.Status == DocumentProcessingStatus.Processed)
        {
            return new DocumentProcessingResultDto(
                document.Id,
                document.Chunks.Count,
                document.Status.ToString());
        }

        document.StartProcessing();

        try
        {
            await using var fileStream = await blobStorageService.DownloadAsync(document.BlobName, cancellationToken);
            var extractedPages = await textExtractionService.ExtractPagesAsync(fileStream, cancellationToken);
            var textChunks = chunkingService.Chunk(extractedPages, command.ChunkSize, command.OverlapSize);

            if (textChunks.Count == 0)
            {
                throw new InvalidOperationException("No text could be extracted from the document.");
            }

            var chunks = new List<DocumentChunk>();
            foreach (var textChunk in textChunks)
            {
                var chunk = DocumentChunk.Create(
                    document.Id,
                    textChunk.ChunkIndex,
                    textChunk.Text,
                    textChunk.SourcePageNumber,
                    textChunk.SectionTitle);
                var vector = await embeddingService.GenerateEmbeddingAsync(textChunk.Text, cancellationToken);
                chunk.SetEmbedding(vector);
                chunks.Add(chunk);
            }

            await documentRepository.ReplaceChunksAsync(document, chunks, cancellationToken);

            return new DocumentProcessingResultDto(
                document.Id,
                chunks.Count,
                document.Status.ToString());
        }
        catch
        {
            if (document.Status != DocumentProcessingStatus.Processed)
            {
                document.MarkProcessingFailed();
                await documentRepository.SaveChangesAsync(cancellationToken);
            }

            throw;
        }
    }
}
