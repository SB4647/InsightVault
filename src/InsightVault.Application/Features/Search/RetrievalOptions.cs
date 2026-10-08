namespace InsightVault.Application.Features.Search;

/// <summary>
/// Defines server-owned limits and quality controls for document retrieval.
/// </summary>
public sealed class RetrievalOptions
{
    public const string SectionName = "Retrieval";

    public int TopK { get; init; } = 5;
    public int CandidateMultiplier { get; init; } = 4;
    public double MinimumSimilarity { get; init; } = 0.60;
    public double FullTextOnlyScore { get; init; } = 0.60;

    /// <summary>
    /// Validates bounded server-side retrieval settings before they are used for a search.
    /// </summary>
    public void Validate()
    {
        if (TopK is < 1 or > 50)
        {
            throw new InvalidOperationException("Retrieval TopK must be between 1 and 50.");
        }

        if (CandidateMultiplier is < 1 or > 20)
        {
            throw new InvalidOperationException("Retrieval CandidateMultiplier must be between 1 and 20.");
        }

        if (MinimumSimilarity is < 0 or > 1 || FullTextOnlyScore is < 0 or > 1)
        {
            throw new InvalidOperationException("Retrieval similarity scores must be between 0 and 1.");
        }
    }
}
