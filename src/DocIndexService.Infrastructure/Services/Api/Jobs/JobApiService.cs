using DocIndexService.Application.Abstractions.Api.Jobs;
using DocIndexService.Application.Abstractions.Ingestion;
using DocIndexService.Contracts.Api.Jobs;
using DocIndexService.Core.Interfaces;
using DocIndexService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocIndexService.Infrastructure.Services.Api.Jobs;

public sealed class JobApiService : IJobApiService
{
    private readonly DocIndexDbContext _dbContext;
    private readonly IIngestionCoordinator _ingestionCoordinator;
    private readonly IClock _clock;

    public JobApiService(
        DocIndexDbContext dbContext,
        IIngestionCoordinator ingestionCoordinator,
        IClock clock)
    {
        _dbContext = dbContext;
        _ingestionCoordinator = ingestionCoordinator;
        _clock = clock;
    }

    public async Task<JobListResponse> ListAsync(CancellationToken cancellationToken)
    {
        var jobs = await _dbContext.IngestionJobsSet
            .Include(x => x.Events)
            .OrderByDescending(x => x.CreatedUtc)
            .Take(250)
            .ToListAsync(cancellationToken);

        var items = jobs.Select(Map).ToArray();
        return new JobListResponse(items, items.Length, _clock.UtcNow);
    }

    public async Task<JobResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await _dbContext.IngestionJobsSet
            .Include(x => x.Events)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return job is null ? null : Map(job);
    }

    public async Task<RetryJobResponse> RetryAsync(Guid id, CancellationToken cancellationToken)
    {
        var jobId = await _ingestionCoordinator.RetryFailedJobAsync(id, cancellationToken);
        if (jobId is null)
        {
            return new RetryJobResponse(false, "Failed job not found.", null, _clock.UtcNow);
        }

        return new RetryJobResponse(true, "Retry job queued.", jobId, _clock.UtcNow);
    }

    private static JobResponse Map(DocIndexService.Core.Entities.IngestionJob job)
    {
        var events = job.Events
            .OrderByDescending(x => x.CreatedUtc)
            .Select(x => new JobEventResponse(
                x.Id,
                x.EventType,
                x.Message,
                x.DetailsJson,
                x.CreatedUtc))
            .ToArray();

        return new JobResponse(
            job.Id,
            job.SourceId,
            job.DocumentId,
            job.JobType.ToString(),
            job.Status.ToString(),
            job.StartedUtc,
            job.CompletedUtc,
            job.ErrorMessage,
            job.AttemptCount,
            job.PayloadJson,
            job.CreatedUtc,
            job.UpdatedUtc,
            events);
    }
}
