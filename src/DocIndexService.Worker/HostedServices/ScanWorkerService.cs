using DocIndexService.Application.Abstractions.Ingestion;
using Microsoft.Extensions.Options;
using DocIndexService.Core.Options;

namespace DocIndexService.Worker.HostedServices;

public sealed class ScanWorkerService : BackgroundService
{
    private readonly ILogger<ScanWorkerService> _logger;
    private readonly ScanOptions _scanOptions;
    private readonly IIngestionCoordinator _ingestionCoordinator;

    public ScanWorkerService(
        ILogger<ScanWorkerService> logger,
        IIngestionCoordinator ingestionCoordinator,
        IOptions<ScanOptions> scanOptions)
    {
        _logger = logger;
        _ingestionCoordinator = ingestionCoordinator;
        _scanOptions = scanOptions.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scan worker started with interval {IntervalMinutes} minutes", _scanOptions.IncrementalIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Scheduled scan tick at {UtcNow}", DateTime.UtcNow);

            try
            {
                await _ingestionCoordinator.RunScheduledScanCycleAsync(stoppingToken);
                await _ingestionCoordinator.ProcessPendingJobsAsync(take: 25, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Scheduled scan cycle failed");
            }

            var delay = TimeSpan.FromMinutes(_scanOptions.IncrementalIntervalMinutes);
            await Task.Delay(delay, stoppingToken);
        }
    }
}
