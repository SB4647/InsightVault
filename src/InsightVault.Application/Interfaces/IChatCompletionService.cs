namespace InsightVault.Application.Interfaces;

public interface IChatCompletionService
{
    Task<string> GenerateAnswerAsync(
        string question,
        IReadOnlyList<ChatCompletionContext> contexts,
        CancellationToken cancellationToken = default);
}
