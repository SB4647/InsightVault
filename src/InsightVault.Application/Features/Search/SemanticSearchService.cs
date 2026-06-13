using InsightVault.Application.Features.Search.DTOs;
using InsightVault.Application.Features.Search.Queries;
using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;

namespace InsightVault.Application.Features.Search;

public sealed class SemanticSearchService(
    IEmbeddingService embeddingService,
    IDocumentSearchRepository documentSearchRepository) : ISemanticSearchService
{
    public async Task<IReadOnlyList<SearchResultDto>> SearchAsync(
        SearchDocumentsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            throw new ArgumentException("Search query is required.", nameof(query));
        }

        if (query.MaxResults <= 0)
        {
            throw new ArgumentException("Max results must be greater than zero.", nameof(query));
        }

        var queryEmbedding = await embeddingService.GenerateEmbeddingAsync(query.Query, cancellationToken);
        var documents = await documentSearchRepository.ListProcessedDocumentsAsync(cancellationToken);

        return documents
            .SelectMany(document => BuildResults(document, queryEmbedding))
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.DocumentName)
            .ThenBy(result => result.ChunkIndex)
            .Take(query.MaxResults)
            .ToList();
    }

    private static IEnumerable<SearchResultDto> BuildResults(
        Document document,
        IReadOnlyList<float> queryEmbedding)
    {
        foreach (var chunk in document.Chunks)
        {
            if (chunk.Embedding is null)
            {
                continue;
            }

            var score = CosineSimilarity(queryEmbedding, chunk.Embedding.GetVector());

            yield return new SearchResultDto(
                document.Id,
                document.OriginalFileName,
                chunk.Id,
                chunk.ChunkIndex,
                chunk.Text,
                score);
        }
    }

    private static double CosineSimilarity(
        IReadOnlyList<float> left,
        IReadOnlyList<float> right)
    {
        if (left.Count == 0 || right.Count == 0 || left.Count != right.Count)
        {
            return 0;
        }

        double dotProduct = 0;
        double leftMagnitude = 0;
        double rightMagnitude = 0;

        for (var i = 0; i < left.Count; i++)
        {
            dotProduct += left[i] * right[i];
            leftMagnitude += left[i] * left[i];
            rightMagnitude += right[i] * right[i];
        }

        if (leftMagnitude == 0 || rightMagnitude == 0)
        {
            return 0;
        }

        return dotProduct / (Math.Sqrt(leftMagnitude) * Math.Sqrt(rightMagnitude));
    }
}
