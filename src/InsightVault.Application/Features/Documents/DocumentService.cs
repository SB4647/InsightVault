using InsightVault.Application.Features.Documents.Commands;
using InsightVault.Application.Features.Documents.DTOs;
using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;

namespace InsightVault.Application.Features.Documents;

public sealed class DocumentService(
    IDocumentRepository documentRepository,
    IBlobStorageService blobStorageService,
    TimeProvider timeProvider,
    IUserLookupService userLookupService) : IDocumentService
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
            timeProvider.GetUtcNow().UtcDateTime,
            command.OwnerUserId);

        await documentRepository.AddAsync(document, cancellationToken);
        await documentRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(document, command.OwnerUserId);
    }

    public async Task<IReadOnlyList<DocumentDto>> GetDocumentsAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var documents = await documentRepository.ListAsync(ownerUserId, cancellationToken);

        return documents
            .OrderByDescending(document => document.UploadedAtUtc)
            .Select(document => MapToDto(document, ownerUserId))
            .ToList();
    }

    public async Task<DocumentShareDto> ShareDocumentAsync(
        ShareDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.SharedWithEmail))
        {
            throw new ArgumentException("Shared user email is required.", nameof(command));
        }

        var document = await documentRepository.GetByIdAsync(
                command.DocumentId,
                command.OwnerUserId,
                cancellationToken)
            ?? throw new InvalidOperationException($"Document '{command.DocumentId}' was not found.");

        var sharedWithUser = await userLookupService.FindByEmailAsync(
                command.SharedWithEmail,
                cancellationToken)
            ?? throw new InvalidOperationException($"User '{command.SharedWithEmail}' was not found.");

        var permission = document.ShareWithViewer(sharedWithUser.UserId);
        await documentRepository.SaveChangesAsync(cancellationToken);

        return new DocumentShareDto(
            document.Id,
            sharedWithUser.UserId,
            sharedWithUser.Email,
            permission.Level.ToString());
    }

    public async Task DeleteDocumentAsync(
        DeleteDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        var document = await documentRepository.GetByIdAsync(
                command.DocumentId,
                command.OwnerUserId,
                cancellationToken)
            ?? throw new InvalidOperationException($"Document '{command.DocumentId}' was not found.");

        await blobStorageService.DeleteAsync(document.BlobName, cancellationToken);
        documentRepository.Remove(document);
        await documentRepository.SaveChangesAsync(cancellationToken);
    }

    private static DocumentDto MapToDto(Document document, string currentUserId)
    {
        var isOwner = document.OwnerUserId == currentUserId;

        return new DocumentDto(
            document.Id,
            document.OriginalFileName,
            document.ContentType,
            document.SizeInBytes,
            document.BlobName,
            document.UploadedAtUtc,
            document.Status.ToString(),
            document.Chunks.Count,
            isOwner,
            isOwner ? "Owner" : "Viewer");
    }
}
