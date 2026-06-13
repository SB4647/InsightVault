namespace InsightVault.Application.Features.Documents.Processing.Commands;

public sealed record ProcessDocumentCommand(
    Guid DocumentId,
    string OwnerUserId,
    int ChunkSize = 1_200,
    int OverlapSize = 200);
