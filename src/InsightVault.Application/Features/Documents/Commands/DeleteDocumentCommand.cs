namespace InsightVault.Application.Features.Documents.Commands;

public sealed record DeleteDocumentCommand(Guid DocumentId, string OwnerUserId);
