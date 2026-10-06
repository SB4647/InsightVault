namespace InsightVault.Application.Features.Documents.Commands;

/// <summary>
/// Requests a fresh asynchronous processing attempt for a failed document owned by the caller.
/// </summary>
public sealed record RetryDocumentProcessingCommand(Guid DocumentId, string OwnerUserId);
