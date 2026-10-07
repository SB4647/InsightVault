using InsightVault.Application.Interfaces;

namespace InsightVault.Application.Features.Search;

/// <summary>
/// Fuses bounded vector and full-text candidates into a stable, quality-filtered result set.
/// </summary>
public sealed class HybridRetrievalService(RetrievalOptions options)
{
    private const int ReciprocalRankConstant = 60;

    /// <summary>
    /// De-duplicates candidates by chunk, applies quality eligibility, and ranks survivors with reciprocal-rank fusion.
    /// </summary>
    public IReadOnlyList<RankedRetrievalMatch> Rank(
        IReadOnlyList<VectorSearchMatch> vectorMatches,
        IReadOnlyList<FullTextSearchMatch> fullTextMatches)
    {
        options.Validate();
        var candidates = new Dictionary<Guid, FusedCandidate>();

        for (var index = 0; index < vectorMatches.Count; index++)
        {
            var match = vectorMatches[index];
            var candidate = GetOrCreate(candidates, match);
            candidate.VectorScore = match.Score;
            candidate.FusedScore += ReciprocalRankScore(index + 1);
        }

        for (var index = 0; index < fullTextMatches.Count; index++)
        {
            var match = fullTextMatches[index];
            var candidate = GetOrCreate(candidates, match);
            candidate.HasFullTextMatch = true;
            candidate.FusedScore += ReciprocalRankScore(index + 1);
        }

        return candidates.Values
            .Where(IsEligible)
            .OrderByDescending(candidate => candidate.FusedScore)
            .ThenBy(candidate => candidate.DocumentId)
            .ThenBy(candidate => candidate.ChunkIndex)
            .ThenBy(candidate => candidate.ChunkId)
            .Take(options.TopK)
            .Select((candidate, index) => candidate.ToRankedMatch(index + 1, options.FullTextOnlyScore))
            .ToList();
    }

    private bool IsEligible(FusedCandidate candidate)
    {
        return candidate.VectorScore is double vectorScore
            ? vectorScore >= options.MinimumSimilarity
            : candidate.HasFullTextMatch && options.FullTextOnlyScore >= options.MinimumSimilarity;
    }

    private static double ReciprocalRankScore(int rank) => 1d / (ReciprocalRankConstant + rank);

    private static FusedCandidate GetOrCreate(
        IDictionary<Guid, FusedCandidate> candidates,
        VectorSearchMatch match)
    {
        if (!candidates.TryGetValue(match.ChunkId, out var candidate))
        {
            candidate = new FusedCandidate(
                match.DocumentId,
                match.DocumentName,
                match.DocumentVersion,
                match.ChunkId,
                match.ChunkIndex,
                match.SourcePageNumber,
                match.SectionTitle,
                match.Text);
            candidates.Add(match.ChunkId, candidate);
        }

        return candidate;
    }

    private static FusedCandidate GetOrCreate(
        IDictionary<Guid, FusedCandidate> candidates,
        FullTextSearchMatch match)
    {
        if (!candidates.TryGetValue(match.ChunkId, out var candidate))
        {
            candidate = new FusedCandidate(
                match.DocumentId,
                match.DocumentName,
                match.DocumentVersion,
                match.ChunkId,
                match.ChunkIndex,
                match.SourcePageNumber,
                match.SectionTitle,
                match.Text);
            candidates.Add(match.ChunkId, candidate);
        }

        return candidate;
    }

    private sealed class FusedCandidate(
        Guid documentId,
        string documentName,
        int documentVersion,
        Guid chunkId,
        int chunkIndex,
        int sourcePageNumber,
        string? sectionTitle,
        string text)
    {
        public Guid DocumentId { get; } = documentId;
        public string DocumentName { get; } = documentName;
        public int DocumentVersion { get; } = documentVersion;
        public Guid ChunkId { get; } = chunkId;
        public int ChunkIndex { get; } = chunkIndex;
        public int SourcePageNumber { get; } = sourcePageNumber;
        public string? SectionTitle { get; } = sectionTitle;
        public string Text { get; } = text;
        public double? VectorScore { get; set; }
        public bool HasFullTextMatch { get; set; }
        public double FusedScore { get; set; }

        public RankedRetrievalMatch ToRankedMatch(int rank, double fullTextOnlyScore)
        {
            return new RankedRetrievalMatch(
                DocumentId,
                DocumentName,
                DocumentVersion,
                ChunkId,
                ChunkIndex,
                SourcePageNumber,
                SectionTitle,
                Text,
                VectorScore ?? fullTextOnlyScore,
                rank);
        }
    }
}

/// <summary>
/// Represents a final, permission-safe source chunk selected for an answer context.
/// </summary>
public sealed record RankedRetrievalMatch(
    Guid DocumentId,
    string DocumentName,
    int DocumentVersion,
    Guid ChunkId,
    int ChunkIndex,
    int SourcePageNumber,
    string? SectionTitle,
    string Text,
    double Score,
    int Rank);
