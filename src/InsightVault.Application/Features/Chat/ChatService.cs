using InsightVault.Application.Features.Chat.DTOs;
using InsightVault.Application.Features.Chat.Queries;
using InsightVault.Application.Features.Search;
using InsightVault.Application.Features.Search.DTOs;
using InsightVault.Application.Features.Search.Queries;
using InsightVault.Application.Interfaces;

namespace InsightVault.Application.Features.Chat;

public sealed class ChatService(
    ISemanticSearchService semanticSearchService,
    IChatCompletionService chatCompletionService) : IChatService
{
    private const string NoRelevantContentAnswer =
        "I could not find relevant document content to answer that question.";

    public async Task<ChatResponseDto> AskAsync(
        AskQuestionQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.Question))
        {
            throw new ArgumentException("Question is required.", nameof(query));
        }

        if (query.MaxSources <= 0)
        {
            throw new ArgumentException("Max sources must be greater than zero.", nameof(query));
        }

        var searchResults = await semanticSearchService.SearchAsync(
            new SearchDocumentsQuery(query.Question, query.MaxSources),
            cancellationToken);

        if (searchResults.Count == 0)
        {
            return new ChatResponseDto(NoRelevantContentAnswer, []);
        }

        var contexts = searchResults.Select(ToContext).ToList();
        var answer = await chatCompletionService.GenerateAnswerAsync(
            query.Question,
            contexts,
            cancellationToken);

        return new ChatResponseDto(
            answer,
            searchResults.Select(ToCitation).ToList());
    }

    private static ChatCompletionContext ToContext(SearchResultDto result)
    {
        return new ChatCompletionContext(
            result.DocumentId,
            result.DocumentName,
            result.ChunkId,
            result.ChunkIndex,
            result.Text,
            result.Score);
    }

    private static SourceCitationDto ToCitation(SearchResultDto result)
    {
        return new SourceCitationDto(
            result.DocumentId,
            result.DocumentName,
            result.ChunkId,
            result.ChunkIndex,
            result.Text,
            result.Score);
    }
}
