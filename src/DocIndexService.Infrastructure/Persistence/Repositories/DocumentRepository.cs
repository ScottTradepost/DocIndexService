using DocIndexService.Application.Abstractions.Persistence.Repositories;
using DocIndexService.Core.Entities;
using DocIndexService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocIndexService.Infrastructure.Persistence.Repositories;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly DocIndexDbContext _dbContext;

    public DocumentRepository(DocIndexDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.DocumentsSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<Document?> GetBySourceAndRelativePathAsync(Guid sourceId, string relativePath, CancellationToken cancellationToken)
    {
        return _dbContext.DocumentsSet
            .FirstOrDefaultAsync(x => x.SourceId == sourceId && x.RelativePath == relativePath, cancellationToken);
    }

    public Task AddAsync(Document document, CancellationToken cancellationToken)
    {
        return _dbContext.DocumentsSet.AddAsync(document, cancellationToken).AsTask();
    }
}
