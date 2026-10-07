using InsightVault.Application.Features.Search;
using InsightVault.Application.Interfaces;

namespace InsightVault.Tests.Application;

public sealed class HybridRetrievalServiceTests
{
    [Fact]
    public void Rank_DeduplicatesSourcesAndUsesStableFusedOrdering()
    {
        var chunkId = Guid.NewGuid();
        var service = new HybridRetrievalService(new RetrievalOptions
        {
            TopK = 2,
            MinimumSimilarity = 0.60,
            FullTextOnlyScore = 0.60
        });

        var results = service.Rank(
        [
            new VectorSearchMatch(Guid.Parse("00000000-0000-0000-0000-000000000002"), "second.pdf", chunkId, 0, "shared source", 0.90, 1, 2, null),
            new VectorSearchMatch(Guid.Parse("00000000-0000-0000-0000-000000000001"), "first.pdf", Guid.NewGuid(), 0, "vector-only source", 0.70, 1, 1, null)
        ],
        [
            new FullTextSearchMatch(Guid.Parse("00000000-0000-0000-0000-000000000002"), "second.pdf", chunkId, 0, "shared source", 0.95, 1, 2, null),
            new FullTextSearchMatch(Guid.Parse("00000000-0000-0000-0000-000000000003"), "third.pdf", Guid.NewGuid(), 0, "text-only source", 0.99, 1, 3, "Appendix")
        ]);

        Assert.Equal(2, results.Count);
        Assert.Equal(chunkId, results[0].ChunkId);
        Assert.Equal(1, results[0].Rank);
        Assert.Equal("vector-only source", results[1].Text);
        Assert.Equal(2, results[1].Rank);
    }

    [Fact]
    public void Rank_ExcludesLowVectorAndConservativeFullTextOnlyCandidates()
    {
        var service = new HybridRetrievalService(new RetrievalOptions
        {
            TopK = 5,
            MinimumSimilarity = 0.70,
            FullTextOnlyScore = 0.60
        });

        var results = service.Rank(
        [
            new VectorSearchMatch(Guid.NewGuid(), "low.pdf", Guid.NewGuid(), 0, "low vector", 0.69, 1, 1, null)
        ],
        [
            new FullTextSearchMatch(Guid.NewGuid(), "text.pdf", Guid.NewGuid(), 0, "text only", 0.99, 1, 1, null)
        ]);

        Assert.Empty(results);
    }
}
