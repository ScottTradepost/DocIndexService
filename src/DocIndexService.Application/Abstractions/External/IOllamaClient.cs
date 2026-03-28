namespace DocIndexService.Application.Abstractions.External;

public interface IOllamaClient
{
    Task<IReadOnlyList<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken);
}
