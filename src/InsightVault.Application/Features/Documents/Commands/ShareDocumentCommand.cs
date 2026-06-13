namespace InsightVault.Application.Features.Documents.Commands;

public sealed record ShareDocumentCommand(
    Guid DocumentId,
    string OwnerUserId,
    string SharedWithEmail);
