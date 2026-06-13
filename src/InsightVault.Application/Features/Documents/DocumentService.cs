using InsightVault.Application.Features.Documents.Commands;
using InsightVault.Application.Features.Documents.DTOs;
using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;

namespace InsightVault.Application.Features.Documents;

public sealed class DocumentService(
    IDocumentRepository documentRepository,
    IBlobStorageService blobStorageService,
    TimeProvider timeProvider) : IDocumentService
{
    public async Task<DocumentDto> UploadAsync(
        UploadDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Content.Length <= 0)
        {
            throw new ArgumentException("File cannot be empty.", nameof(command));
        }

        var safeFileName = Path.GetFileName(command.FileName);
        var extension = Path.GetExtension(safeFileName);
        var blobName = $"documents/{Guid.NewGuid():N}{extension}";

        if (command.Content.CanSeek)
        {
            command.Content.Position = 0;
        }

        await blobStorageService.UploadAsync(
            blobName,
            command.Content,
            command.ContentType,
            cancellationToken);

        var document = Document.Create(
            safeFileName,
            command.ContentType,
            command.SizeInBytes,
            blobName,
            timeProvider.GetUtcNow().UtcDateTime);

        await documentRepository.AddAsync(document, cancellationToken);
        await documentRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(document);
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var documents = await documentRepository.ListAsync(cancellationToken);

        return documents
            .OrderByDescending(document => document.UploadedAtUtc)
            .Select(MapToDto)
            .ToList();
    }

    private static DocumentDto MapToDto(Document document)
    {
        return new DocumentDto(
            document.Id,
            document.OriginalFileName,
            document.ContentType,
            document.SizeInBytes,
            document.BlobName,
            document.UploadedAtUtc,
            document.Status.ToString(),
            document.Chunks.Count);
    }
}
