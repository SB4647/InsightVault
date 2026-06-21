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
    public const long MaxUploadSizeInBytes = 25_000_000;

    public async Task<DocumentDto> UploadAsync(
        UploadDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        var safeFileName = Path.GetFileName(command.FileName);
        ValidateUpload(command, safeFileName);

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

    private static void ValidateUpload(UploadDocumentCommand command, string safeFileName)
    {
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            throw new ArgumentException("File name is required.", nameof(command));
        }

        if (command.SizeInBytes <= 0)
        {
            throw new ArgumentException("File cannot be empty.", nameof(command));
        }

        if (command.SizeInBytes > MaxUploadSizeInBytes)
        {
            throw new ArgumentException("File cannot be larger than 25 MB.", nameof(command));
        }

        if (!string.Equals(Path.GetExtension(safeFileName), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only PDF files can be uploaded.", nameof(command));
        }

        if (!string.Equals(command.ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Only application/pdf files can be uploaded.", nameof(command));
        }
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
        documentRepository.AddPermission(permission);
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
            document.UploadedAtUtc,
            document.Status.ToString(),
            document.Chunks.Count,
            isOwner,
            isOwner ? "Owner" : "Viewer");
    }
}
