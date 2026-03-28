using DocIndexService.Contracts.Api.Sources;

namespace DocIndexService.Application.Abstractions.Api.Sources;

public interface ISourceApiService
{
    Task<SourceListResponse> ListAsync(CancellationToken cancellationToken);
    Task<SourceResponse?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<SourceActionResponse> CreateAsync(CreateSourceRequest request, CancellationToken cancellationToken);
    Task<SourceActionResponse> UpdateAsync(Guid id, UpdateSourceRequest request, CancellationToken cancellationToken);
    Task<SourceActionResponse> ScanAsync(Guid id, bool fullReconciliation, CancellationToken cancellationToken);
    Task<SourceActionResponse> ReindexAsync(Guid id, CancellationToken cancellationToken);
}
