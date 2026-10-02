using InsightVault.Application.Features.Search.DTOs;
using InsightVault.Application.Features.Search.Queries;
using InsightVault.Application.Interfaces;

namespace InsightVault.Application.Features.Search;

public sealed class SemanticSearchService(
    IEmbeddingService embeddingService,
    IVectorSearchRepository vectorSearchRepository) : ISemanticSearchService
{
    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(
        SearchDocumentsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            throw new ArgumentException("Search query is required.", nameof(query));
        }

        if (string.IsNullOrWhiteSpace(query.OwnerUserId))
        {
            throw new ArgumentException("Owner user id is required.", nameof(query));
        }

        if (query.MaxResults <= 0)
        {
            throw new ArgumentException("Max results must be greater than zero.", nameof(query));
        }

        var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(query.Query, cancellationToken);
        var matches = await vectorSearchRepository.SearchAsync(
            new VectorSearchRequest(query.OwnerUserId, queryEmbedding, query.MaxResults),
            cancellationToken);

        return matches
            .Select(match => new SearchResultDto(
                match.DocumentId,
                match.DocumentName,
                match.ChunkId,
                match.ChunkIndex,
                match.Text,
                match.Score))
            .ToList();
    }
}
