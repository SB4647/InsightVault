using InsightVault.Application.Features.Chat.DTOs;
using InsightVault.Application.Features.Chat.Queries;
using InsightVault.Application.Features.Search;
using InsightVault.Application.Features.Search.DTOs;
using InsightVault.Application.Features.Search.Queries;
using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;

namespace InsightVault.Application.Features.Chat;

public sealed class ChatService(
    ISemanticSearchService semanticSearchService,
    IChatCompletionService chatCompletionService,
    IChatAnswerRepository chatAnswerRepository,
    RetrievalOptions retrievalOptions) : IChatService
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

        if (string.IsNullOrWhiteSpace(query.OwnerUserId))
        {
            throw new ArgumentException("Owner user id is required.", nameof(query));
        }

        var searchResults = await semanticSearchService.SearchAsync(
            new SearchDocumentsQuery(query.Question, query.OwnerUserId),
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

        var citations = searchResults
            .Select((result, index) => ToCitation(result, GetRank(result, index)))
            .ToList();
        var answerRecord = ChatAnswer.Create(
            query.OwnerUserId,
            query.Question,
            answer,
            DateTime.UtcNow,
            retrievalOptions.TopK,
            Convert.ToDecimal(retrievalOptions.MinimumSimilarity),
            retrievalStrategyVersion: 1);
        foreach (var citation in citations)
        {
            answerRecord.AddCitation(ChatAnswerCitation.Create(
                answerRecord.Id,
                citation.DocumentId,
                citation.DocumentVersion,
                citation.ChunkId,
                citation.SourcePageNumber,
                citation.SectionTitle,
                Convert.ToDecimal(citation.Score),
                citation.Rank));
        }

        await chatAnswerRepository.SaveAsync(answerRecord, cancellationToken);

        return new ChatResponseDto(
            answer,
            citations);
    }

    private static ChatCompletionContext ToContext(SearchResultDto result)
    {
        return new ChatCompletionContext(
            result.DocumentId,
            result.DocumentName,
            result.ChunkId,
            result.ChunkIndex,
            result.Text,
            result.Score,
            result.DocumentVersion,
            result.SourcePageNumber,
            result.SectionTitle,
            result.Rank);
    }

    private static SourceCitationDto ToCitation(SearchResultDto result, int rank)
    {
        return new SourceCitationDto(
            result.DocumentId,
            result.DocumentName,
            result.ChunkId,
            result.ChunkIndex,
            result.Text,
            result.Score,
            result.DocumentVersion,
            result.SourcePageNumber,
            result.SectionTitle,
            rank);
    }

    private static int GetRank(SearchResultDto result, int index) => result.Rank > 0 ? result.Rank : index + 1;
}
