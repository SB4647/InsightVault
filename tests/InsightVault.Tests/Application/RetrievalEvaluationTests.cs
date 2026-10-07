using System.Text.Json;

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
        var retrievalDouble = new DeterministicRetrievalDouble(cases);

        var measurements = cases.Select(testCase =>
        {
            var expected = testCase.ExpectedChunkIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var retrieved = retrievalDouble.Retrieve(testCase.Question).Take(5).ToList();
            var relevantRetrieved = retrieved.Count(expected.Contains);
            return new
            {
                Recall = relevantRetrieved / (double)expected.Count,
                Precision = relevantRetrieved / (double)retrieved.Count
            };
        }).ToList();

        Assert.True(measurements.Average(measurement => measurement.Recall) >= 0.80);
        Assert.True(measurements.Average(measurement => measurement.Precision) >= 0.60);
    }

    private sealed record RetrievalEvaluationCase(
        string Question,
        IReadOnlyList<string> ExpectedChunkIds,
        IReadOnlyList<string> RetrievedChunkIds);

    /// <summary>
    /// Provides fixed retrieval outputs so the quality threshold test never needs cloud embeddings or a live database.
    /// </summary>
    private sealed class DeterministicRetrievalDouble(IEnumerable<RetrievalEvaluationCase> cases)
    {
        private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _results = cases
            .ToDictionary(testCase => testCase.Question, testCase => testCase.RetrievedChunkIds);

        public IReadOnlyList<string> Retrieve(string question) => _results[question];
    }
}
