using DocIndexService.Application.Abstractions.Dashboard;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DocIndexService.Admin.Pages.Dashboard;

public sealed class IndexModel : PageModel
{
    private readonly IDashboardStatsService _dashboardStatsService;

    public IndexModel(IDashboardStatsService dashboardStatsService)
    {
        _dashboardStatsService = dashboardStatsService;
    }

    public DashboardStatsDto? Stats { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Stats = await _dashboardStatsService.GetAsync(cancellationToken);
    }
}
