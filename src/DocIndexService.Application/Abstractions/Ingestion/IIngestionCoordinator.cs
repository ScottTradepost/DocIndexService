namespace DocIndexService.Application.Abstractions.Ingestion;

public interface IIngestionCoordinator
{
    Task RunScheduledScanCycleAsync(CancellationToken cancellationToken);
    Task TriggerScanAsync(Guid sourceId, bool fullReconciliation, CancellationToken cancellationToken);
    Task ProcessPendingJobsAsync(int take, CancellationToken cancellationToken);
    Task<Guid?> RetryFailedJobAsync(Guid jobId, CancellationToken cancellationToken);
    Task<Guid?> EnqueueDocumentReprocessAsync(Guid documentId, CancellationToken cancellationToken);
}
