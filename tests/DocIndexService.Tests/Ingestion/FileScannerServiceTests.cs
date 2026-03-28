using DocIndexService.Core.Entities;
using DocIndexService.Core.Options;
using DocIndexService.Infrastructure.Services.Ingestion;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DocIndexService.Tests.Ingestion;

public sealed class FileScannerServiceTests
{
    [Fact]
    public async Task ScanAsync_ShouldRespectIncludeAndExcludePatterns()
    {
        var root = Path.Combine(Path.GetTempPath(), "docindex-scan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(Path.Combine(root, "keep.txt"), "ok");
            File.WriteAllText(Path.Combine(root, "drop.log"), "log");
            File.WriteAllText(Path.Combine(root, "skip.tmp"), "tmp");

            var source = CreateSource(root, includePatterns: "*.txt;*.log", excludePatterns: "*.tmp;*.log", isRecursive: false);
            var scanner = CreateScanner(maxFileSizeMb: 10);

            var snapshot = await scanner.ScanAsync(source, fullReconciliation: false, CancellationToken.None);

            Assert.Single(snapshot.Files);
            Assert.Equal("keep.txt", snapshot.Files[0].RelativePath);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ScanAsync_ShouldSkipFilesOverConfiguredSize()
    {
        var root = Path.Combine(Path.GetTempPath(), "docindex-scan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            File.WriteAllText(Path.Combine(root, "small.txt"), "tiny");
            File.WriteAllText(Path.Combine(root, "big.txt"), new string('x', 5000));

            var source = CreateSource(root, includePatterns: "*.txt", excludePatterns: string.Empty, isRecursive: false);
            var scanner = CreateScanner(maxFileSizeMb: 0); // 0 MB max means only empty files qualify.

            var snapshot = await scanner.ScanAsync(source, fullReconciliation: true, CancellationToken.None);

            Assert.Empty(snapshot.Files);
            Assert.True(snapshot.FullReconciliation);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static FileScannerService CreateScanner(int maxFileSizeMb)
    {
        var options = Options.Create(new ScanOptions
        {
            MaxFileSizeMb = maxFileSizeMb,
            IncrementalIntervalMinutes = 15,
            FullReconciliationHourUtc = 2
        });

        return new FileScannerService(options, NullLogger<FileScannerService>.Instance);
    }

    private static DocumentSource CreateSource(string rootPath, string includePatterns, string excludePatterns, bool isRecursive)
    {
        var now = DateTime.UtcNow;
        return new DocumentSource
        {
            Id = Guid.NewGuid(),
            Name = "Scanner Test Source",
            RootPath = rootPath,
            IsRecursive = isRecursive,
            IncludePatterns = includePatterns,
            ExcludePatterns = excludePatterns,
            ScanIntervalMinutes = 15,
            IsEnabled = true,
            CreatedUtc = now,
            UpdatedUtc = now
        };
    }
}
