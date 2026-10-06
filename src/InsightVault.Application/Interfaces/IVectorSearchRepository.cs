namespace InsightVault.Application.Interfaces;

public interface IVectorSearchRepository
{
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
    double Score);
