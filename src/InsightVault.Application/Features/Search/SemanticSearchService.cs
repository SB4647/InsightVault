using InsightVault.Application.Features.Search.DTOs;
using InsightVault.Application.Features.Search.Queries;
using InsightVault.Application.Interfaces;

namespace InsightVault.Application.Features.Search;

public sealed class SemanticSearchService(
    IEmbeddingService embeddingService,
    IVectorSearchRepository vectorSearchRepository,
    IFullTextSearchRepository fullTextSearchRepository,
    HybridRetrievalService hybridRetrievalService,
    RetrievalOptions retrievalOptions) : ISemanticSearchService
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

        retrievalOptions.Validate();
        var maximumCandidates = retrievalOptions.TopK * retrievalOptions.CandidateMultiplier;
        var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(query.Query, cancellationToken);
        var vectorTask = vectorSearchRepository.SearchAsync(
            new VectorSearchRequest(query.OwnerUserId, queryEmbedding, maximumCandidates, query.Query),
            cancellationToken);
        var fullTextTask = fullTextSearchRepository.SearchAsync(
            new FullTextSearchRequest(query.OwnerUserId, query.Query, maximumCandidates),
            cancellationToken);
        await Task.WhenAll(vectorTask, fullTextTask);

        return hybridRetrievalService.Rank(vectorTask.Result, fullTextTask.Result)
            .Select(match => new SearchResultDto(
                match.DocumentId,
                match.DocumentName,
                match.ChunkId,
                match.ChunkIndex,
                match.Text,
                match.Score,
                match.DocumentVersion,
                match.SourcePageNumber,
                match.SectionTitle,
                match.Rank))
            .ToList();
    }
}
