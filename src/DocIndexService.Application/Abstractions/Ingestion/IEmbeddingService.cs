namespace DocIndexService.Application.Abstractions.Ingestion;

public interface IEmbeddingService
{
    Task<IReadOnlyList<EmbeddedChunk>> GenerateAsync(IReadOnlyList<TextChunk> chunks, CancellationToken cancellationToken);
}
