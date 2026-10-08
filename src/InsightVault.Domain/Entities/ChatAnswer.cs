namespace InsightVault.Domain.Entities;

/// <summary>
/// Stores a successfully generated answer and the retrieval settings that produced it.
/// </summary>
public sealed class ChatAnswer
{
    private readonly List<ChatAnswerCitation> _citations = [];

    private ChatAnswer()
    {
    }

    private ChatAnswer(
        string ownerUserId,
        string question,
        string answer,
        DateTime createdAtUtc,
        int topK,
        decimal minimumSimilarity,
        int retrievalStrategyVersion)
    {
        Id = Guid.NewGuid();
        OwnerUserId = ownerUserId;
        Question = question;
        Answer = answer;
        CreatedAtUtc = createdAtUtc;
        TopK = topK;
        MinimumSimilarity = minimumSimilarity;
        RetrievalStrategyVersion = retrievalStrategyVersion;
    }

    public Guid Id { get; private set; }
    public string OwnerUserId { get; private set; } = string.Empty;
    public string Question { get; private set; } = string.Empty;
    public string Answer { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }
    public int TopK { get; private set; }
    public decimal MinimumSimilarity { get; private set; }
    public int RetrievalStrategyVersion { get; private set; }
    public IReadOnlyCollection<ChatAnswerCitation> Citations => _citations.AsReadOnly();

    /// <summary>
    /// Creates an auditable answer record after the language model has generated a non-empty response.
    /// </summary>
    public static ChatAnswer Create(
        string ownerUserId,
        string question,
        string answer,
        DateTime createdAtUtc,
        int topK,
        decimal minimumSimilarity,
        int retrievalStrategyVersion)
    {
        if (string.IsNullOrWhiteSpace(ownerUserId))
        {
            throw new ArgumentException("Owner user id is required.", nameof(ownerUserId));
        }

        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question is required.", nameof(question));
        }

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new ArgumentException("Answer is required.", nameof(answer));
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Creation timestamp must be UTC.", nameof(createdAtUtc));
        }

        if (topK <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(topK));
        }

        if (minimumSimilarity is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumSimilarity));
        }

        if (retrievalStrategyVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(retrievalStrategyVersion));
        }

        return new ChatAnswer(
            ownerUserId.Trim(),
            question.Trim(),
            answer.Trim(),
            createdAtUtc,
            topK,
            minimumSimilarity,
            retrievalStrategyVersion);
    }

    /// <summary>
    /// Adds a source citation that belongs to this answer and has a unique final rank.
    /// </summary>
    public void AddCitation(ChatAnswerCitation citation)
    {
        ArgumentNullException.ThrowIfNull(citation);

        if (citation.ChatAnswerId != Id)
        {
            throw new ArgumentException("Citation belongs to another answer.", nameof(citation));
        }

        if (_citations.Any(existingCitation => existingCitation.Rank == citation.Rank))
        {
            throw new ArgumentException("Citation rank must be unique within an answer.", nameof(citation));
        }

        _citations.Add(citation);
    }
}
