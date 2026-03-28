using DocIndexService.Application.Abstractions.Api.Sources;
using DocIndexService.Application.Abstractions.SourceManagement;
using DocIndexService.Contracts.Api.Sources;
using DocIndexService.Core.Interfaces;

namespace DocIndexService.Infrastructure.Services.Api.Sources;

public sealed class SourceApiService : ISourceApiService
{
    private readonly IDocumentSourceService _documentSourceService;
    private readonly IClock _clock;

    public SourceApiService(IDocumentSourceService documentSourceService, IClock clock)
    {
        _documentSourceService = documentSourceService;
        _clock = clock;
    }

    public async Task<SourceListResponse> ListAsync(CancellationToken cancellationToken)
    {
        var sources = await _documentSourceService.ListAsync(cancellationToken);
        var items = sources.Select(Map).ToArray();
        return new SourceListResponse(items, items.Length, _clock.UtcNow);
    }

    public async Task<SourceResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var source = await _documentSourceService.GetByIdAsync(id, cancellationToken);
        return source is null ? null : Map(source);
    }

    public async Task<SourceActionResponse> CreateAsync(CreateSourceRequest request, CancellationToken cancellationToken)
    {
        var result = await _documentSourceService.CreateAsync(
            new CreateDocumentSourceRequest(
                request.Name,
                request.RootPath,
                request.IsRecursive,
                request.IncludePatterns,
                request.ExcludePatterns,
                request.ScanIntervalMinutes,
                request.IsEnabled),
            cancellationToken);

        return new SourceActionResponse(result.Succeeded, result.Message, _clock.UtcNow);
    }

    public async Task<SourceActionResponse> UpdateAsync(Guid id, UpdateSourceRequest request, CancellationToken cancellationToken)
    {
        var result = await _documentSourceService.UpdateAsync(
            id,
            new UpdateDocumentSourceRequest(
                request.Name,
                request.RootPath,
                request.IsRecursive,
                request.IncludePatterns,
                request.ExcludePatterns,
                request.ScanIntervalMinutes,
                request.IsEnabled),
            cancellationToken);

        return new SourceActionResponse(result.Succeeded, result.Message, _clock.UtcNow);
    }

    public async Task<SourceActionResponse> ScanAsync(Guid id, bool fullReconciliation, CancellationToken cancellationToken)
    {
        var result = await _documentSourceService.TriggerManualScanAsync(
            new ManualScanRequest(id, fullReconciliation),
            cancellationToken);

        return new SourceActionResponse(result.Succeeded, result.Message, _clock.UtcNow);
    }

    public Task<SourceActionResponse> ReindexAsync(Guid id, CancellationToken cancellationToken)
    {
        return ScanAsync(id, fullReconciliation: true, cancellationToken);
    }

    private static SourceResponse Map(DocumentSourceDto source)
    {
        return new SourceResponse(
            source.Id,
            source.Name,
            source.RootPath,
            source.IsEnabled,
            source.IsRecursive,
            source.IncludePatterns,
            source.ExcludePatterns,
            source.ScanIntervalMinutes,
            source.LastScanUtc,
            source.LastSuccessfulScanUtc,
            source.CreatedUtc,
            source.UpdatedUtc);
    }
}
