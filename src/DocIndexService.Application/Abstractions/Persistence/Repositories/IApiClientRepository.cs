using DocIndexService.Core.Entities;

namespace DocIndexService.Application.Abstractions.Persistence.Repositories;

public interface IApiClientRepository
{
    Task<ApiClient?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken);
    Task AddAsync(ApiClient apiClient, CancellationToken cancellationToken);
}
