namespace DocIndexService.Application.Abstractions.Dashboard;

public interface IDashboardStatsService
{
    Task<DashboardStatsDto> GetAsync(CancellationToken cancellationToken);
}
