using System.Security.Cryptography;
using System.Text;
using DocIndexService.Application.Abstractions.External;
using DocIndexService.Application.Abstractions.Ingestion;
using DocIndexService.Core.Options;
using Microsoft.Extensions.Options;

namespace DocIndexService.Infrastructure.Services.Ingestion;

public sealed class EmbeddingService : IEmbeddingService
{
    private readonly IOllamaClient _ollamaClient;
    private readonly OllamaOptions _ollamaOptions;

    public EmbeddingService(
        IOllamaClient ollamaClient,
        IOptions<OllamaOptions> ollamaOptions)
    {
        _ollamaClient = ollamaClient;
        _ollamaOptions = ollamaOptions.Value;
    }

    public async Task<IReadOnlyList<EmbeddedChunk>> GenerateAsync(IReadOnlyList<TextChunk> chunks, CancellationToken cancellationToken)
    {
        var embedded = new List<EmbeddedChunk>(chunks.Count);
        foreach (var chunk in chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var vector = await _ollamaClient.GenerateEmbeddingAsync(chunk.Text, cancellationToken);
            if (vector.Count == 0)
            {
                vector = CreateDeterministicVector(chunk.Text);
            }

            embedded.Add(new EmbeddedChunk(
                chunk,
                vector,
                _ollamaOptions.EmbeddingModel,
                "phase1-placeholder"));
        }

        return embedded;
    }

    private static IReadOnlyList<float> CreateDeterministicVector(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        var vector = new float[32];
        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = (bytes[i] / 255f) * 2f - 1f;
        }

        return vector;
    }
}
