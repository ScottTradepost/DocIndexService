using DocIndexService.Contracts.Api.Documents;

namespace DocIndexService.Application.Abstractions.Api.Documents;

public interface IDocumentApiService
{
    Task<DocumentListResponse> ListAsync(CancellationToken cancellationToken);
    Task<DocumentResponse?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<ReprocessDocumentResponse> ReprocessAsync(Guid id, CancellationToken cancellationToken);
}
