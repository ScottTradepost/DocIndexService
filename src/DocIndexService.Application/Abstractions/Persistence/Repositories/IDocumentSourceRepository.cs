using DocIndexService.Core.Entities;

namespace DocIndexService.Application.Abstractions.Persistence.Repositories;

public interface IDocumentSourceRepository
{
    Task<DocumentSource?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentSource>> ListAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentSource>> ListEnabledAsync(CancellationToken cancellationToken);
    Task AddAsync(DocumentSource source, CancellationToken cancellationToken);
}
