namespace InsightVault.Application.Interfaces;

public interface IVectorSearchRepository
{
    /// <summary>
    /// Finds the highest-ranked document chunks the requesting user is allowed to read.
    /// </summary>
    Task<IReadOnlyList<VectorSearchMatch>> SearchAsync(
        VectorSearchRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record VectorSearchRequest(
    string OwnerUserId,
    IReadOnlyList<float> QueryEmbedding,
    int MaxResults);

public sealed record VectorSearchMatch(
    Guid DocumentId,
    string DocumentName,
    Guid ChunkId,
    int ChunkIndex,
    string Text,
    double Score,
    int DocumentVersion = 1,
    int SourcePageNumber = 1,
    string? SectionTitle = null);
