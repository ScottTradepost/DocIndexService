using DocIndexService.Application.Abstractions.Persistence.Repositories;
using DocIndexService.Core.Entities;
using DocIndexService.Core.Enums;
using DocIndexService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocIndexService.Infrastructure.Persistence.Repositories;

public sealed class IngestionJobRepository : IIngestionJobRepository
{
    private readonly DocIndexDbContext _dbContext;

    public IngestionJobRepository(DocIndexDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<IngestionJob?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.IngestionJobsSet
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<IngestionJob>> ListPendingAsync(int take, CancellationToken cancellationToken)
    {
        return await _dbContext.IngestionJobsSet
            .Where(x => x.Status == IngestionJobStatus.Pending)
            .OrderBy(x => x.CreatedUtc)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(IngestionJob job, CancellationToken cancellationToken)
    {
        return _dbContext.IngestionJobsSet.AddAsync(job, cancellationToken).AsTask();
    }

    public Task AddEventAsync(IngestionJobEvent ingestionJobEvent, CancellationToken cancellationToken)
    {
        return _dbContext.IngestionJobEventsSet.AddAsync(ingestionJobEvent, cancellationToken).AsTask();
    }

    public async Task SetStatusAsync(Guid jobId, IngestionJobStatus status, string? errorMessage, CancellationToken cancellationToken)
    {
        var job = await _dbContext.IngestionJobsSet.FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);
        if (job is null)
        {
            return;
        }

        job.Status = status;
        job.ErrorMessage = errorMessage;
        job.UpdatedUtc = DateTime.UtcNow;
        if (status is IngestionJobStatus.Succeeded or IngestionJobStatus.Failed or IngestionJobStatus.Cancelled)
        {
            job.CompletedUtc = DateTime.UtcNow;
        }
    }
}
