using InsightVault.Application.Features.Documents.Commands;
using InsightVault.Application.Features.Documents.DTOs;
using InsightVault.Application.Interfaces;
using InsightVault.Application.ProcessingQueue;
using InsightVault.Domain.Entities;

namespace InsightVault.Application.Features.Documents;

public sealed class DocumentService(
    IDocumentRepository documentRepository,
    IBlobStorageService blobStorageService,
    TimeProvider timeProvider,
    IUserLookupService userLookupService,
    IDocumentProcessingOutboxRepository? documentProcessingOutboxRepository = null,
    UploadQuotaOptions? uploadQuotaOptions = null) : IDocumentService
{
    public const long MaxUploadSizeInBytes = 25_000_000;

    private readonly UploadQuotaOptions _uploadQuotaOptions = uploadQuotaOptions ?? new UploadQuotaOptions();

    public async Task<DocumentDto> UploadAsync(
        UploadDocumentCommand command,
        CancellationToken cancellationToken = default)
    {
        var safeFileName = Path.GetFileName(command.FileName);
        ValidateUpload(command, safeFileName);
        string? uploadedBlobName = null;

        try
        {
            return await documentRepository.ExecuteSerializableAsync(async transactionCancellationToken =>
            {
                await EnsureOwnerHasAvailableQuotaAsync(command, transactionCancellationToken);

                var extension = Path.GetExtension(safeFileName);
                uploadedBlobName = $"documents/{Guid.NewGuid():N}{extension}";

                if (command.Content.CanSeek)
                {
                    command.Content.Position = 0;
                }

                await blobStorageService.UploadAsync(
                    uploadedBlobName,
                    command.Content,
                    command.ContentType,
                    transactionCancellationToken);

                var document = Document.Create(
                    safeFileName,
                    command.ContentType,
                    command.SizeInBytes,
                    uploadedBlobName,
                    timeProvider.GetUtcNow().UtcDateTime,
                    command.OwnerUserId);

                await documentRepository.AddAsync(document, transactionCancellationToken);
                await GetOutboxRepository().AddAsync(
                    DocumentProcessingOutboxEntry.Create(
                        document.Id,
                        document.OwnerUserId,
                        timeProvider.GetUtcNow().UtcDateTime),
                    transactionCancellationToken);
                await documentRepository.SaveChangesAsync(transactionCancellationToken);

                return MapToDto(document, command.OwnerUserId);
            }, cancellationToken);
        }
        catch
        {
            if (!string.IsNullOrEmpty(uploadedBlobName))
            {
                try
                {
                    await blobStorageService.DeleteAsync(uploadedBlobName, CancellationToken.None);
                }
                catch
                {
                    // Preserve the original upload failure when best-effort storage cleanup also fails.
                }
            }

            throw;
        }
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

    private async Task EnsureOwnerHasAvailableQuotaAsync(
        UploadDocumentCommand command,
        CancellationToken cancellationToken)
    {
        var usage = await documentRepository.GetOwnedUsageAsync(command.OwnerUserId, cancellationToken);
        if (usage.DocumentCount >= _uploadQuotaOptions.MaxDocumentsPerOwner)
        {
            throw new InvalidOperationException("Document upload quota has been reached for this user.");
        }

        if (usage.StoredBytes > _uploadQuotaOptions.MaxStoredBytesPerOwner - command.SizeInBytes)
        {
            throw new InvalidOperationException("Storage upload quota has been reached for this user.");
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

        var shareResult = document.ShareWithViewer(sharedWithUser.UserId);
        if (shareResult.Created)
        {
            documentRepository.AddPermission(shareResult.Permission);
        }

        await documentRepository.SaveChangesAsync(cancellationToken);

        return new DocumentShareDto(
            document.Id,
            sharedWithUser.UserId,
            sharedWithUser.Email,
            shareResult.Permission.Level.ToString());
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

    /// <summary>
    /// Resets a failed document and records a new durable processing request for its owner.
    /// </summary>
    public async Task<DocumentDto> RetryProcessingAsync(
        RetryDocumentProcessingCommand command,
        CancellationToken cancellationToken = default)
    {
        var document = await documentRepository.GetByIdAsync(
                command.DocumentId,
                command.OwnerUserId,
                cancellationToken)
            ?? throw new InvalidOperationException($"Document '{command.DocumentId}' was not found.");

        document.ResetForRetry();
        await GetOutboxRepository().AddAsync(
            DocumentProcessingOutboxEntry.Create(
                document.Id,
                document.OwnerUserId,
                timeProvider.GetUtcNow().UtcDateTime),
            cancellationToken);
        await documentRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(document, command.OwnerUserId);
    }

    private IDocumentProcessingOutboxRepository GetOutboxRepository()
    {
        return documentProcessingOutboxRepository
            ?? throw new InvalidOperationException(
                "Document processing outbox persistence must be configured before uploads or retries are accepted.");
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
