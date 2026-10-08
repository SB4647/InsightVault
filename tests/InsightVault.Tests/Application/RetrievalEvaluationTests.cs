using System.Text.Json;
using InsightVault.Application.Features.Search;
using InsightVault.Application.Features.Search.Queries;
using InsightVault.Application.Interfaces;

namespace InsightVault.Tests.Application;

public sealed class RetrievalEvaluationTests
{
    [Fact]
    public async Task EvaluationDataset_MeetsDocumentedRecallAndPrecisionTargetsAtFive()
    {
        var datasetPath = Path.Combine(AppContext.BaseDirectory, "TestData", "retrieval-evaluation.json");
        await using var stream = File.OpenRead(datasetPath);
        var cases = await JsonSerializer.DeserializeAsync<List<RetrievalEvaluationCase>>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("The retrieval evaluation dataset is empty.");
        var options = new RetrievalOptions();
        var service = new SemanticSearchService(
            new DeterministicEmbeddingService(),
            new DeterministicVectorSearchRepository(cases),
            new DeterministicFullTextSearchRepository(cases),
            new HybridRetrievalService(options),
            options);

        var measurements = new List<RetrievalMeasurement>();
        foreach (var testCase in cases)
        {
            var expected = testCase.ExpectedChunkIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var results = await service.SearchAsync(new(testCase.Question, "evaluation-user"));
            var retrieved = results.Take(5).Select(result => result.ChunkId.ToString()).ToList();
            var relevantRetrieved = retrieved.Count(expected.Contains);
            measurements.Add(new(
                relevantRetrieved / (double)expected.Count,
                relevantRetrieved / (double)retrieved.Count));
        }

        Assert.True(measurements.Average(measurement => measurement.Recall) >= 0.80);
        Assert.True(measurements.Average(measurement => measurement.Precision) >= 0.60);
    }

    private sealed record RetrievalEvaluationCase(
        string Question,
        IReadOnlyList<string> ExpectedChunkIds,
        IReadOnlyList<EvaluationCandidate> VectorCandidates,
        IReadOnlyList<EvaluationCandidate> FullTextCandidates);

    private sealed record EvaluationCandidate(string ChunkId, double Score);

    private sealed record RetrievalMeasurement(double Recall, double Precision);

    /// <summary>
    /// Prevents the evaluation from calling an external embedding model while exercising the production search service.
    /// </summary>
    private sealed class DeterministicEmbeddingService : IEmbeddingService
    {
        public Task<IReadOnlyList<float>> GenerateEmbeddingAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<float>>([1f]);
        }
    }

    /// <summary>
    /// Supplies fixed vector candidates to exercise the production hybrid-ranking pipeline without cloud services.
    /// </summary>
    private sealed class DeterministicVectorSearchRepository(IEnumerable<RetrievalEvaluationCase> cases)
        : IVectorSearchRepository
    {
        private readonly IReadOnlyDictionary<string, IReadOnlyList<EvaluationCandidate>> _candidates = cases
            .ToDictionary(testCase => testCase.Question, testCase => testCase.VectorCandidates);

        public Task<IReadOnlyList<VectorSearchMatch>> SearchAsync(
            VectorSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            var matches = _candidates[request.Query]
                .Take(request.MaxResults)
                .Select((candidate, index) => ToVectorMatch(candidate, index))
                .ToList();
            return Task.FromResult<IReadOnlyList<VectorSearchMatch>>(matches);
        }

        private static VectorSearchMatch ToVectorMatch(EvaluationCandidate candidate, int index)
        {
            return new VectorSearchMatch(
                EvaluationDocumentId,
                "evaluation.pdf",
                Guid.Parse(candidate.ChunkId),
                index,
                $"Evaluation content {index}",
                candidate.Score,
                1,
                1,
                null);
        }
    }

    /// <summary>
    /// Supplies fixed full-text candidates to exercise fusion with the matching vector candidates.
    /// </summary>
    private sealed class DeterministicFullTextSearchRepository(IEnumerable<RetrievalEvaluationCase> cases)
        : IFullTextSearchRepository
    {
        private readonly IReadOnlyDictionary<string, IReadOnlyList<EvaluationCandidate>> _candidates = cases
            .ToDictionary(testCase => testCase.Question, testCase => testCase.FullTextCandidates);

        public Task<IReadOnlyList<FullTextSearchMatch>> SearchAsync(
            FullTextSearchRequest request,
            CancellationToken cancellationToken = default)
        {
            var matches = _candidates[request.Query]
                .Take(request.MaxResults)
                .Select((candidate, index) => new FullTextSearchMatch(
                    EvaluationDocumentId,
                    "evaluation.pdf",
                    Guid.Parse(candidate.ChunkId),
                    index,
                    $"Evaluation content {index}",
                    candidate.Score,
                    1,
                    1,
                    null))
                .ToList();
            return Task.FromResult<IReadOnlyList<FullTextSearchMatch>>(matches);
        }
    }

    private static readonly Guid EvaluationDocumentId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
}
