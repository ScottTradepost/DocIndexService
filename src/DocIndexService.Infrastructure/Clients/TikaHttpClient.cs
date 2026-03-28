using DocIndexService.Application.Abstractions.External;
using DocIndexService.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocIndexService.Infrastructure.Clients;

public sealed class TikaHttpClient : ITikaClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TikaHttpClient> _logger;

    public TikaHttpClient(
        HttpClient httpClient,
        IOptions<TikaOptions> options,
        ILogger<TikaHttpClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromSeconds(options.Value.TimeoutSeconds);
    }

    public async Task<string> ExtractTextAsync(string fullPath, CancellationToken cancellationToken)
    {
        await using var fileStream = File.OpenRead(fullPath);
        using var content = new StreamContent(fileStream);
        using var request = new HttpRequestMessage(HttpMethod.Put, "/tika")
        {
            Content = content
        };

        request.Headers.TryAddWithoutValidation("Accept", "text/plain");

        var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Tika request failed for {Path} with status {StatusCode}", fullPath, response.StatusCode);
            return string.Empty;
        }

        return await response.Content.ReadAsStringAsync(cancellationToken);
    }
}
