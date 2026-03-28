namespace DocIndexService.Application.Abstractions.Ingestion;

public interface IFileFingerprintService
{
    Task<FileFingerprintResult> ComputeAsync(string fullPath, CancellationToken cancellationToken);
}
