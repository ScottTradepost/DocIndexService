using DocIndexService.Core.Entities;

namespace DocIndexService.Application.Abstractions.Ingestion;

public interface IFileScanner
{
    Task<SourceScanSnapshot> ScanAsync(DocumentSource source, bool fullReconciliation, CancellationToken cancellationToken);
}
