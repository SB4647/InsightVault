namespace InsightVault.Application.Interfaces;

/// <summary>
/// Retrieves permission-filtered text matches for the hybrid retrieval pipeline.
/// </summary>
public interface IFullTextSearchRepository
{
    /// <summary>
    /// Finds the highest-ranked text matches the requesting user is allowed to read.
    /// </summary>
    Task<IReadOnlyList<FullTextSearchMatch>> SearchAsync(
        FullTextSearchRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record FullTextSearchRequest(string OwnerUserId, string Query, int MaxResults);

public sealed record FullTextSearchMatch(
    Guid DocumentId,
    string DocumentName,
    Guid ChunkId,
    int ChunkIndex,
    string Text,
    double Score,
    int DocumentVersion = 1,
    int SourcePageNumber = 1,
    string? SectionTitle = null);
