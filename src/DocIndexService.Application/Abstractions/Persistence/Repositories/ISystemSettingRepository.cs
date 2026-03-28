using DocIndexService.Core.Entities;

namespace DocIndexService.Application.Abstractions.Persistence.Repositories;

public interface ISystemSettingRepository
{
    Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken);
    Task UpsertAsync(SystemSetting setting, CancellationToken cancellationToken);
}
