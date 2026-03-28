using DocIndexService.Core.Entities;

namespace DocIndexService.Application.Abstractions.Persistence.Repositories;

public interface IDocumentVersionRepository
{
    Task AddAsync(DocumentVersion version, CancellationToken cancellationToken);
    Task<IReadOnlyList<DocumentVersion>> ListForDocumentAsync(Guid documentId, CancellationToken cancellationToken);
}
