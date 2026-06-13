namespace InsightVault.Application.Features.Chat.Queries;

public sealed record AskQuestionQuery(
    string Question,
    string OwnerUserId,
    int MaxSources = 5);
