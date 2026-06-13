using InsightVault.Application.Features.Chat;
using InsightVault.Application.Features.Chat.DTOs;
using InsightVault.Application.Features.Chat.Queries;
using InsightVault.Application.Features.Search;
using InsightVault.Application.Features.Search.DTOs;
using InsightVault.Application.Features.Search.Queries;
using InsightVault.Application.Interfaces;

namespace InsightVault.Tests.Application;

public class ChatServiceTests
{
    [Fact]
    public async Task AskAsync_WithBlankQuestion_ThrowsArgumentException()
    {
        var service = new ChatService(
            new StubSemanticSearchService([]),
            new StubChatCompletionService("unused"));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AskAsync(new AskQuestionQuery(" ")));
    }

    [Fact]
    public async Task AskAsync_WithNoSearchResults_ReturnsNoSourceCitations()
    {
        var chatCompletion = new StubChatCompletionService("unused");
        var service = new ChatService(
            new StubSemanticSearchService([]),
            chatCompletion);

        var response = await service.AskAsync(new AskQuestionQuery("What is covered?"));

        Assert.Equal("I could not find relevant document content to answer that question.", response.Answer);
        Assert.Empty(response.Sources);
        Assert.False(chatCompletion.WasCalled);
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
                0.91)
        };
        var chatCompletion = new StubChatCompletionService("InsightVault answers questions using processed chunks.");
        var service = new ChatService(
            new StubSemanticSearchService(searchResults),
            chatCompletion);

        var response = await service.AskAsync(new AskQuestionQuery("How does chat work?"));

        Assert.Equal("InsightVault answers questions using processed chunks.", response.Answer);
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

    private sealed class StubSemanticSearchService(
        IReadOnlyList<SearchResultDto> results) : ISemanticSearchService
    {
        public Task<IReadOnlyList<SearchResultDto>> SearchAsync(
            SearchDocumentsQuery query,
            CancellationToken cancellationToken = default)
        {
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
}
