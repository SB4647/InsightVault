namespace InsightVault.Application.Features.Search.Queries;

public sealed record SearchDocumentsQuery(
    string Query,
    string OwnerUserId,
    int MaxResults = 10);
