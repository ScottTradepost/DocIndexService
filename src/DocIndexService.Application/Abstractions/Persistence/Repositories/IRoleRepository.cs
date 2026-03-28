using DocIndexService.Core.Entities;

namespace DocIndexService.Application.Abstractions.Persistence.Repositories;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken);
    Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken);
}
