using DocIndexService.Core.Entities;

namespace DocIndexService.Application.Abstractions.Persistence.Repositories;

public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Document?> GetBySourceAndRelativePathAsync(Guid sourceId, string relativePath, CancellationToken cancellationToken);
    Task AddAsync(Document document, CancellationToken cancellationToken);
}
