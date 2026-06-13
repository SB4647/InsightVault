using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using InsightVault.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace InsightVault.Infrastructure.Chat;

public sealed class AzureOpenAiChatCompletionService(
    HttpClient httpClient,
    IOptions<AzureOpenAiChatOptions> options) : IChatCompletionService
{
    private readonly AzureOpenAiChatOptions _options = options.Value;

    public async Task<string> GenerateAnswerAsync(
        string question,
        IReadOnlyList<ChatCompletionContext> contexts,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException("Question is required for chat completion.", nameof(question));
        }

        if (contexts.Count == 0)
        {
            throw new ArgumentException("At least one source context is required.", nameof(contexts));
        }

        EnsureConfigured();

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildRequestUri())
        {
            Content = JsonContent.Create(new ChatCompletionRequest(
                [
                    new ChatMessage(
                        "system",
                        "You answer questions using only the provided document excerpts. If the excerpts do not contain the answer, say you do not know. Keep answers concise and cite sources using [source number]."),
                    new ChatMessage("user", BuildUserMessage(question, contexts))
                ],
                Temperature: 0.2,
                MaxTokens: 700))
        };
        request.Headers.Add("api-key", _options.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken);
        var answer = body?.Choices.FirstOrDefault()?.Message.Content;

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new InvalidOperationException("Azure OpenAI returned an empty chat response.");
        }

        return answer;
    }

    private Uri BuildRequestUri()
    {
        var endpoint = _options.Endpoint.TrimEnd('/');
        var deploymentName = Uri.EscapeDataString(_options.DeploymentName);
        var apiVersion = Uri.EscapeDataString(_options.ApiVersion);

        return new Uri($"{endpoint}/openai/deployments/{deploymentName}/chat/completions?api-version={apiVersion}");
    }

    private static string BuildUserMessage(
        string question,
        IReadOnlyList<ChatCompletionContext> contexts)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Question:");
        builder.AppendLine(question.Trim());
        builder.AppendLine();
        builder.AppendLine("Sources:");

        for (var i = 0; i < contexts.Count; i++)
        {
            var context = contexts[i];
            builder.AppendLine($"[{i + 1}] {context.DocumentName}, chunk {context.ChunkIndex}");
            builder.AppendLine(context.Text);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.Endpoint))
        {
            throw new InvalidOperationException("AzureOpenAI:Endpoint is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("AzureOpenAI:ApiKey is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.DeploymentName))
        {
            throw new InvalidOperationException("AzureOpenAI:ChatDeploymentName is required.");
        }
    }

    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("messages")] ChatMessage[] Messages,
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("max_tokens")] int MaxTokens);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatCompletionResponse(
        [property: JsonPropertyName("choices")] ChatChoice[] Choices);

    private sealed record ChatChoice(
        [property: JsonPropertyName("message")] ChatMessage Message);
}
