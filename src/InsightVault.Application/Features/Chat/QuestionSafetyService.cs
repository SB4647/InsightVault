namespace InsightVault.Application.Features.Chat;

/// <summary>Rejects a deliberately narrow set of high-confidence prompt-injection patterns before retrieval.</summary>
public sealed class QuestionSafetyService : IQuestionSafetyService
{
    private const int MaximumQuestionLength = 2_000;

    private static readonly string[] BlockedPhrases =
    [
        "ignore previous instructions",
        "ignore all previous instructions",
        "reveal the system prompt",
        "show me the system prompt",
        "developer message",
        "system message"
    ];

    /// <inheritdoc />
    public void ValidateQuestion(string question)
    {
        if (question.Length > MaximumQuestionLength)
        {
            throw new ArgumentException("Question cannot be longer than 2000 characters.", nameof(question));
        }

        if (BlockedPhrases.Any(phrase => question.Contains(phrase, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("This question contains an unsafe instruction pattern.", nameof(question));
        }
    }
}
