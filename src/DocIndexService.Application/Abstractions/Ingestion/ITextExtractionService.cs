using DocIndexService.Core.Entities;

namespace DocIndexService.Application.Abstractions.Ingestion;

public interface ITextExtractionService
{
    Task<TextExtractionResult> ExtractAsync(Document document, CancellationToken cancellationToken);
}
