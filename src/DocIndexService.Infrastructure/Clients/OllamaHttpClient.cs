using System.Text;
using System.Text.Json;
using DocIndexService.Application.Abstractions.External;
using DocIndexService.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocIndexService.Infrastructure.Clients;

public sealed class OllamaHttpClient : IOllamaClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly OllamaOptions _ollamaOptions;
    private readonly ILogger<OllamaHttpClient> _logger;

    public OllamaHttpClient(
        HttpClient httpClient,
        IOptions<OllamaOptions> options,
        ILogger<OllamaHttpClient> logger)
    {
        _httpClient = httpClient;
        _ollamaOptions = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_ollamaOptions.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<IReadOnlyList<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            model = _ollamaOptions.EmbeddingModel,
            prompt = text
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/embeddings")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(requestBody, SerializerOptions),
                Encoding.UTF8,
                "application/json")
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Ollama embedding request failed with status {StatusCode}", response.StatusCode);
            return Array.Empty<float>();
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var parsed = await JsonSerializer.DeserializeAsync<OllamaEmbeddingResponse>(stream, SerializerOptions, cancellationToken);

        return parsed?.Embedding ?? Array.Empty<float>();
    }

    private sealed record OllamaEmbeddingResponse(float[] Embedding);
}
