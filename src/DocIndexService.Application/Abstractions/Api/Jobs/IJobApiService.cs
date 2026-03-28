using DocIndexService.Contracts.Api.Jobs;

namespace DocIndexService.Application.Abstractions.Api.Jobs;

public interface IJobApiService
{
    Task<JobListResponse> ListAsync(CancellationToken cancellationToken);
    Task<JobResponse?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<RetryJobResponse> RetryAsync(Guid id, CancellationToken cancellationToken);
}
