namespace InsightVault.Domain.Entities;

/// <summary>
/// Records the exact document chunk that supported a generated answer.
/// </summary>
public sealed class ChatAnswerCitation
{
    private ChatAnswerCitation()
    {
    }

    private ChatAnswerCitation(
        Guid chatAnswerId,
        Guid documentId,
        int documentVersion,
        Guid documentChunkId,
        int sourcePageNumber,
        string? sectionTitle,
        decimal score,
        int rank)
    {
        Id = Guid.NewGuid();
        ChatAnswerId = chatAnswerId;
        DocumentId = documentId;
        DocumentVersion = documentVersion;
        DocumentChunkId = documentChunkId;
        SourcePageNumber = sourcePageNumber;
        SectionTitle = sectionTitle;
        Score = score;
        Rank = rank;
    }

    public Guid Id { get; private set; }
    public Guid ChatAnswerId { get; private set; }
    public Guid DocumentId { get; private set; }
    public int DocumentVersion { get; private set; }
    public Guid DocumentChunkId { get; private set; }
    public int SourcePageNumber { get; private set; }
    public string? SectionTitle { get; private set; }
    public decimal Score { get; private set; }
    public int Rank { get; private set; }

    /// <summary>
    /// Creates an immutable citation snapshot for a source chunk selected by retrieval.
    /// </summary>
    public static ChatAnswerCitation Create(
        Guid chatAnswerId,
        Guid documentId,
        int documentVersion,
        Guid documentChunkId,
        int sourcePageNumber,
        string? sectionTitle,
        decimal score,
        int rank)
    {
        if (chatAnswerId == Guid.Empty || documentId == Guid.Empty || documentChunkId == Guid.Empty)
        {
            throw new ArgumentException("Answer, document, and chunk identifiers are required.");
        }

        if (documentVersion <= 0 || sourcePageNumber <= 0 || rank <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rank));
        }

        if (score is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(score));
        }

        return new ChatAnswerCitation(
            chatAnswerId,
            documentId,
            documentVersion,
            documentChunkId,
            sourcePageNumber,
            string.IsNullOrWhiteSpace(sectionTitle) ? null : sectionTitle.Trim(),
            score,
            rank);
    }
}
