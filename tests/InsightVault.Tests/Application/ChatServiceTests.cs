using InsightVault.Application.Features.Chat;
using InsightVault.Application.Features.Chat.DTOs;
using InsightVault.Application.Features.Chat.Queries;
using InsightVault.Application.Features.Search;
using InsightVault.Application.Features.Search.DTOs;
using InsightVault.Application.Features.Search.Queries;
using InsightVault.Application.Interfaces;
using InsightVault.Domain.Entities;

namespace InsightVault.Tests.Application;

public class ChatServiceTests
{
    [Fact]
    public async Task AskAsync_WithBlankQuestion_ThrowsArgumentException()
    {
        var service = CreateService(
            new StubSemanticSearchService([]),
            new StubChatCompletionService("unused"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AskAsync(new AskQuestionQuery(" ", "user-1")));
    }

    [Fact]
    public async Task AskAsync_WithNoSearchResults_ReturnsNoSourceCitations()
    {
        var chatCompletion = new StubChatCompletionService("unused");
        var answerRepository = new StubChatAnswerRepository();
        var service = CreateService(
            new StubSemanticSearchService([]),
            chatCompletion,
            answerRepository);

        var response = await service.AskAsync(new AskQuestionQuery("What is covered?", "user-1"));

        Assert.Equal("I could not find relevant document content to answer that question.", response.Answer);
        Assert.Empty(response.Sources);
        Assert.False(chatCompletion.WasCalled);
        Assert.Empty(answerRepository.SavedAnswers);
    }

    [Fact]
    public async Task AskAsync_ReturnsAnswerAndSourceCitationsFromSearchResults()
    {
        var documentId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();
        var searchResults = new[]
        {
            new SearchResultDto(
                documentId,
                "strategy.pdf",
                chunkId,
                2,
                "InsightVault uses retrieval augmented generation over processed document chunks.",
                0.91,
                1,
                3,
                "Architecture",
                1)
        };
        var search = new StubSemanticSearchService(searchResults);
        var chatCompletion = new StubChatCompletionService("InsightVault answers questions using processed chunks.");
        var answerRepository = new StubChatAnswerRepository();
        var service = CreateService(
            search,
            chatCompletion,
            answerRepository);

        var response = await service.AskAsync(new AskQuestionQuery("How does chat work?", "user-1"));

        Assert.Equal("InsightVault answers questions using processed chunks.", response.Answer);
        var savedAnswer = Assert.Single(answerRepository.SavedAnswers);
        var savedCitation = Assert.Single(savedAnswer.Citations);
        Assert.Equal(3, savedCitation.SourcePageNumber);
        Assert.Equal("Architecture", savedCitation.SectionTitle);
        Assert.Equal(1, savedCitation.Rank);
        Assert.Collection(
            response.Sources,
            source =>
            {
                Assert.Equal(documentId, source.DocumentId);
                Assert.Equal("strategy.pdf", source.DocumentName);
                Assert.Equal(chunkId, source.ChunkId);
                Assert.Equal(2, source.ChunkIndex);
                Assert.Equal("InsightVault uses retrieval augmented generation over processed document chunks.", source.Text);
        Assert.Equal(0.91, source.Score);
            });
        Assert.True(chatCompletion.WasCalled);
        Assert.Equal("user-1", search.Query?.OwnerUserId);
        Assert.Equal("How does chat work?", chatCompletion.Question);
        Assert.Collection(
            chatCompletion.Contexts,
            context =>
            {
                Assert.Equal(documentId, context.DocumentId);
                Assert.Equal(chunkId, context.ChunkId);
                Assert.Equal("strategy.pdf", context.DocumentName);
                Assert.Equal(2, context.ChunkIndex);
            });
    }

    [Fact]
    public async Task AskAsync_WhenGenerationFails_DoesNotPersistProvenance()
    {
        var answerRepository = new StubChatAnswerRepository();
        var service = CreateService(
            new StubSemanticSearchService([CreateSearchResult()]),
            new ThrowingChatCompletionService(),
            answerRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AskAsync(new AskQuestionQuery("How does chat work?", "user-1")));

        Assert.Empty(answerRepository.SavedAnswers);
    }

    [Fact]
    public async Task AskAsync_WhenProvenanceSaveFails_PropagatesTheFailure()
    {
        var answerRepository = new StubChatAnswerRepository(throwOnSave: true);
        var service = CreateService(
            new StubSemanticSearchService([CreateSearchResult()]),
            new StubChatCompletionService("Generated answer."),
            answerRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AskAsync(new AskQuestionQuery("How does chat work?", "user-1")));

        Assert.Empty(answerRepository.SavedAnswers);
        Assert.Equal(1, answerRepository.SaveAttempts);
    }

    private static ChatService CreateService(
        ISemanticSearchService semanticSearchService,
        IChatCompletionService chatCompletionService,
        IChatAnswerRepository? chatAnswerRepository = null)
    {
        return new ChatService(
            semanticSearchService,
            chatCompletionService,
            chatAnswerRepository ?? new StubChatAnswerRepository(),
            new RetrievalOptions());
    }

    private static SearchResultDto CreateSearchResult()
    {
        return new SearchResultDto(
            Guid.NewGuid(),
            "strategy.pdf",
            Guid.NewGuid(),
            0,
            "Processed document content.",
            0.90,
            1,
            1,
            null,
            1);
    }

    private sealed class StubSemanticSearchService(
        IReadOnlyList<SearchResultDto> results) : ISemanticSearchService
    {
        public SearchDocumentsQuery? Query { get; private set; }

        public Task<IReadOnlyList<SearchResultDto>> SearchAsync(
            SearchDocumentsQuery query,
            CancellationToken cancellationToken = default)
        {
            Query = query;
            return Task.FromResult(results);
        }
    }

    private sealed class StubChatCompletionService(string answer) : IChatCompletionService
    {
        public bool WasCalled { get; private set; }
        public string? Question { get; private set; }
        public IReadOnlyList<ChatCompletionContext> Contexts { get; private set; } = [];

        public Task<string> GenerateAnswerAsync(
            string question,
            IReadOnlyList<ChatCompletionContext> contexts,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            Question = question;
            Contexts = contexts;

            return Task.FromResult(answer);
        }
    }

    private sealed class ThrowingChatCompletionService : IChatCompletionService
    {
        public Task<string> GenerateAnswerAsync(
            string question,
            IReadOnlyList<ChatCompletionContext> contexts,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Generation failed.");
        }
    }

    private sealed class StubChatAnswerRepository(bool throwOnSave = false) : IChatAnswerRepository
    {
        public List<ChatAnswer> SavedAnswers { get; } = [];
        public int SaveAttempts { get; private set; }

        public Task SaveAsync(ChatAnswer answer, CancellationToken cancellationToken = default)
        {
            SaveAttempts++;
            if (throwOnSave)
            {
                throw new InvalidOperationException("Save failed.");
            }

            SavedAnswers.Add(answer);
            return Task.CompletedTask;
        }
    }
}
