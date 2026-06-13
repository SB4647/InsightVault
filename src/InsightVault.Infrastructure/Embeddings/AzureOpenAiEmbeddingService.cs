using System.Net.Http.Json;
using System.Text.Json.Serialization;
using InsightVault.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace InsightVault.Infrastructure.Embeddings;

public sealed class AzureOpenAiEmbeddingService(
    HttpClient httpClient,
    IOptions<AzureOpenAiEmbeddingOptions> options) : IEmbeddingService
{
    private readonly AzureOpenAiEmbeddingOptions _options = options.Value;

    public async Task<IReadOnlyList<float>> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text is required for embedding generation.", nameof(text));
        }

        EnsureConfigured();

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildRequestUri())
        {
            Content = JsonContent.Create(new EmbeddingRequest(text))
        };
        request.Headers.Add("api-key", _options.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken);
        var embedding = body?.Data.FirstOrDefault()?.Embedding;

        if (embedding is null || embedding.Length == 0)
        {
            throw new InvalidOperationException("Azure OpenAI returned an empty embedding.");
        }

        return embedding;
    }

    private Uri BuildRequestUri()
    {
        var endpoint = _options.Endpoint.TrimEnd('/');
        var deploymentName = Uri.EscapeDataString(_options.DeploymentName);
        var apiVersion = Uri.EscapeDataString(_options.ApiVersion);

        return new Uri($"{endpoint}/openai/deployments/{deploymentName}/embeddings?api-version={apiVersion}");
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
            throw new InvalidOperationException("AzureOpenAI:DeploymentName is required.");
        }
    }

    private sealed record EmbeddingRequest([property: JsonPropertyName("input")] string Input);

    private sealed record EmbeddingResponse([property: JsonPropertyName("data")] EmbeddingData[] Data);

    private sealed record EmbeddingData([property: JsonPropertyName("embedding")] float[] Embedding);
}
