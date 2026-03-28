using DocIndexService.Application.Abstractions.Persistence.Repositories;
using DocIndexService.Core.Entities;
using DocIndexService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DocIndexService.Infrastructure.Persistence.Repositories;

public sealed class DocumentSourceRepository : IDocumentSourceRepository
{
    private readonly DocIndexDbContext _dbContext;

    public DocumentSourceRepository(DocIndexDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<DocumentSource?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _dbContext.DocumentSourcesSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentSource>> ListAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.DocumentSourcesSet
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentSource>> ListEnabledAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.DocumentSourcesSet
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(DocumentSource source, CancellationToken cancellationToken)
    {
        return _dbContext.DocumentSourcesSet.AddAsync(source, cancellationToken).AsTask();
    }
}
