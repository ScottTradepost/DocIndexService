using DocIndexService.Application.Abstractions.Dashboard;
using DocIndexService.Core.Enums;
using DocIndexService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocIndexService.Infrastructure.Services.Dashboard;

public sealed class DashboardStatsService : IDashboardStatsService
{
    private readonly DocIndexDbContext _dbContext;

    public DashboardStatsService(DocIndexDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardStatsDto> GetAsync(CancellationToken cancellationToken)
    {
        var totalSources = await _dbContext.DocumentSourcesSet.CountAsync(cancellationToken);
        var activeSources = await _dbContext.DocumentSourcesSet.CountAsync(x => x.IsEnabled, cancellationToken);
        var totalDocuments = await _dbContext.DocumentsSet.CountAsync(cancellationToken);
        var indexedDocuments = await _dbContext.DocumentsSet.CountAsync(x => x.Status == DocumentStatus.Indexed, cancellationToken);
        var failedJobs = await _dbContext.IngestionJobsSet.CountAsync(x => x.Status == IngestionJobStatus.Failed, cancellationToken);
        var pendingJobs = await _dbContext.IngestionJobsSet.CountAsync(x => x.Status == IngestionJobStatus.Pending, cancellationToken);
        var deletedDocuments = await _dbContext.DocumentsSet.CountAsync(x => x.IsDeleted, cancellationToken);
        var lastScanUtc = await _dbContext.DocumentSourcesSet.MaxAsync(x => x.LastScanUtc, cancellationToken);

        var recent = await _dbContext.IngestionJobsSet
            .OrderByDescending(x => x.StartedUtc)
            .Take(10)
            .Select(x => new RecentIngestionActivityDto(
                x.Id,
                x.SourceId,
                x.DocumentId,
                x.JobType.ToString(),
                x.Status.ToString(),
                x.StartedUtc,
                x.CompletedUtc,
                x.ErrorMessage))
            .ToListAsync(cancellationToken);

        return new DashboardStatsDto(
            totalSources,
            activeSources,
            totalDocuments,
            indexedDocuments,
            failedJobs,
            pendingJobs,
            deletedDocuments,
            lastScanUtc,
            recent);
    }
}
