using DocIndexService.Core.Entities;

namespace DocIndexService.Application.Abstractions.Ingestion;

public interface IDocumentIndexService
{
    Task IndexAsync(
        Document document,
        TextExtractionResult extraction,
        IReadOnlyList<EmbeddedChunk> embeddedChunks,
        CancellationToken cancellationToken);

    Task MarkDeletedAsync(Document document, CancellationToken cancellationToken);
}
