namespace InsightVault.Application.Features.Search.Queries;

public sealed record SearchDocumentsQuery(
    string Query,
    string OwnerUserId);
