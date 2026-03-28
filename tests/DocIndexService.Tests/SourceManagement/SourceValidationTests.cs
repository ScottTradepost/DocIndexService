using DocIndexService.Application.Abstractions.Ingestion;
using DocIndexService.Application.Abstractions.SourceManagement;
using DocIndexService.Core.Interfaces;
using DocIndexService.Infrastructure.Persistence;
using DocIndexService.Infrastructure.Services.SourceManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DocIndexService.Tests.SourceManagement;

public sealed class SourceValidationTests
{
    [Fact]
    public async Task ValidateAsync_ShouldFail_ForRelativePath()
    {
        await using var db = CreateDbContext();
        var service = CreateService(db);

        var request = new CreateDocumentSourceRequest(
            Name: "Source A",
            RootPath: "relative/path",
            IsRecursive: true,
            IncludePatterns: "*.txt",
            ExcludePatterns: string.Empty,
            ScanIntervalMinutes: 15,
            IsEnabled: true);

        var result = await service.ValidateAsync(request, CancellationToken.None);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, message => message.Contains("absolute", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateAsync_ShouldFail_WhenIncludePatternsMissing()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "docindex-validation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            await using var db = CreateDbContext();
            var service = CreateService(db);

            var request = new CreateDocumentSourceRequest(
                Name: "Source B",
                RootPath: tempRoot,
                IsRecursive: true,
                IncludePatterns: " ",
                ExcludePatterns: string.Empty,
                ScanIntervalMinutes: 15,
                IsEnabled: true);

            var result = await service.ValidateAsync(request, CancellationToken.None);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, message => message.Contains("include pattern", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    [Fact]
    public async Task ValidateAsync_ShouldPass_ForValidSourceSettings()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "docindex-validation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            await using var db = CreateDbContext();
            var service = CreateService(db);

            var request = new CreateDocumentSourceRequest(
                Name: "Source C",
                RootPath: tempRoot,
                IsRecursive: true,
                IncludePatterns: "*.md;*.txt",
                ExcludePatterns: "bin/*",
                ScanIntervalMinutes: 15,
                IsEnabled: true);

            var result = await service.ValidateAsync(request, CancellationToken.None);

            Assert.True(result.IsValid);
            Assert.True(result.PathExists);
            Assert.True(result.PathIsAccessible);
            Assert.Empty(result.Errors);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }

    private static DocIndexDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DocIndexDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new DocIndexDbContext(options);
    }

    private static DocumentSourceService CreateService(DocIndexDbContext db)
    {
        return new DocumentSourceService(
            db,
            new NoOpIngestionCoordinator(),
            new TestClock(),
            NullLogger<DocumentSourceService>.Instance);
    }

    private sealed class NoOpIngestionCoordinator : IIngestionCoordinator
    {
        public Task RunScheduledScanCycleAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task TriggerScanAsync(Guid sourceId, bool fullReconciliation, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task ProcessPendingJobsAsync(int take, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task<Guid?> RetryFailedJobAsync(Guid jobId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Guid?>(null);
        }

        public Task<Guid?> EnqueueDocumentReprocessAsync(Guid documentId, CancellationToken cancellationToken)
        {
            return Task.FromResult<Guid?>(null);
        }
    }

    private sealed class TestClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
