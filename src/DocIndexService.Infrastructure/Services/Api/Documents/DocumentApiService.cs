using DocIndexService.Application.Abstractions.Api.Documents;
using DocIndexService.Application.Abstractions.Ingestion;
using DocIndexService.Contracts.Api.Documents;
using DocIndexService.Core.Interfaces;
using DocIndexService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocIndexService.Infrastructure.Services.Api.Documents;

public sealed class DocumentApiService : IDocumentApiService
{
    private readonly DocIndexDbContext _dbContext;
    private readonly IIngestionCoordinator _ingestionCoordinator;
    private readonly IClock _clock;

    public DocumentApiService(
        DocIndexDbContext dbContext,
        IIngestionCoordinator ingestionCoordinator,
        IClock clock)
    {
        _dbContext = dbContext;
        _ingestionCoordinator = ingestionCoordinator;
        _clock = clock;
    }

    public async Task<DocumentListResponse> ListAsync(CancellationToken cancellationToken)
    {
        var items = await _dbContext.DocumentsSet
            .OrderByDescending(x => x.UpdatedUtc)
            .Take(250)
            .Select(x => new DocumentResponse(
                x.Id,
                x.SourceId,
                x.RelativePath,
                x.FullPath,
                x.FileName,
                x.Extension,
                x.MimeType,
                x.Sha256,
                x.FileSize,
                x.FileLastModifiedUtc,
                x.Status.ToString(),
                x.IsDeleted,
                x.Title,
                x.Summary,
                x.LastIndexedUtc,
                x.CreatedUtc,
                x.UpdatedUtc))
            .ToArrayAsync(cancellationToken);

        return new DocumentListResponse(items, items.Length, _clock.UtcNow);
    }

    public async Task<DocumentResponse?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.DocumentsSet
            .Where(x => x.Id == id)
            .Select(x => new DocumentResponse(
                x.Id,
                x.SourceId,
                x.RelativePath,
                x.FullPath,
                x.FileName,
                x.Extension,
                x.MimeType,
                x.Sha256,
                x.FileSize,
                x.FileLastModifiedUtc,
                x.Status.ToString(),
                x.IsDeleted,
                x.Title,
                x.Summary,
                x.LastIndexedUtc,
                x.CreatedUtc,
                x.UpdatedUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ReprocessDocumentResponse> ReprocessAsync(Guid id, CancellationToken cancellationToken)
    {
        var jobId = await _ingestionCoordinator.EnqueueDocumentReprocessAsync(id, cancellationToken);
        if (jobId is null)
        {
            return new ReprocessDocumentResponse(false, "Document not found.", null, _clock.UtcNow);
        }

        return new ReprocessDocumentResponse(true, "Reprocess job queued.", jobId, _clock.UtcNow);
    }
}
