namespace DocIndexService.Application.Abstractions.Ingestion;

public interface IChunkingService
{
    Task<IReadOnlyList<TextChunk>> ChunkAsync(string text, CancellationToken cancellationToken);
}
