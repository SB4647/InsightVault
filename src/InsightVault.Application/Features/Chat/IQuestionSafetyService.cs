namespace InsightVault.Application.Features.Chat;

/// <summary>Validates that a chat question does not contain an obvious instruction-override attempt.</summary>
public interface IQuestionSafetyService
{
    /// <summary>Throws when a question is too large or matches a blocked prompt-injection pattern.</summary>
    void ValidateQuestion(string question);
}
