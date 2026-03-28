using DocIndexService.Core.Entities;
using DocIndexService.Core.Enums;

namespace DocIndexService.Application.Abstractions.Persistence.Repositories;

public interface IIngestionJobRepository
{
    Task<IngestionJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<IngestionJob>> ListPendingAsync(int take, CancellationToken cancellationToken);
    Task AddAsync(IngestionJob job, CancellationToken cancellationToken);
    Task AddEventAsync(IngestionJobEvent ingestionJobEvent, CancellationToken cancellationToken);
    Task SetStatusAsync(Guid jobId, IngestionJobStatus status, string? errorMessage, CancellationToken cancellationToken);
}
