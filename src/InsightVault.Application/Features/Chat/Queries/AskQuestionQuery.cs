namespace InsightVault.Application.Features.Chat.Queries;

public sealed record AskQuestionQuery(
    string Question,
    int MaxSources = 5);
