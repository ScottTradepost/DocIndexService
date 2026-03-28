using System.IO.Enumeration;
using DocIndexService.Application.Abstractions.Ingestion;
using DocIndexService.Core.Entities;
using DocIndexService.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DocIndexService.Infrastructure.Services.Ingestion;

public sealed class FileScannerService : IFileScanner
{
    private readonly ScanOptions _scanOptions;
    private readonly ILogger<FileScannerService> _logger;

    public FileScannerService(
        IOptions<ScanOptions> scanOptions,
        ILogger<FileScannerService> logger)
    {
        _scanOptions = scanOptions.Value;
        _logger = logger;
    }

    public Task<SourceScanSnapshot> ScanAsync(DocumentSource source, bool fullReconciliation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rootPath = source.RootPath.Trim();
        var includePatterns = SplitPatterns(source.IncludePatterns);
        var excludePatterns = SplitPatterns(source.ExcludePatterns);
        var files = new List<ScannedFileEntry>();

        if (!Directory.Exists(rootPath))
        {
            _logger.LogWarning("Source root path does not exist for source {SourceId}: {RootPath}", source.Id, rootPath);
            return Task.FromResult(new SourceScanSnapshot(source.Id, rootPath, DateTime.UtcNow, files, fullReconciliation));
        }

        var searchOption = source.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var maxBytes = _scanOptions.MaxFileSizeMb * 1024L * 1024L;

        foreach (var fullPath in Directory.EnumerateFiles(rootPath, "*", searchOption))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(rootPath, fullPath).Replace('\\', '/');
            if (!MatchesAny(relativePath, includePatterns, includeWhenEmpty: true))
            {
                continue;
            }

            if (MatchesAny(relativePath, excludePatterns, includeWhenEmpty: false))
            {
                continue;
            }

            FileInfo info;
            try
            {
                info = new FileInfo(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Skipping unreadable path during scan: {Path}", fullPath);
                continue;
            }

            if (info.Length > maxBytes)
            {
                _logger.LogDebug("Skipping oversized file {Path} with size {Size}", fullPath, info.Length);
                continue;
            }

            files.Add(new ScannedFileEntry(
                relativePath,
                fullPath,
                info.LastWriteTimeUtc,
                info.Length,
                info.Extension));
        }

        _logger.LogInformation(
            "Scanned source {SourceId} at {RootPath}; found {Count} matching files",
            source.Id,
            rootPath,
            files.Count);

        return Task.FromResult(new SourceScanSnapshot(source.Id, rootPath, DateTime.UtcNow, files, fullReconciliation));
    }

    private static string[] SplitPatterns(string patterns)
    {
        return patterns
            .Split([';', ',', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool MatchesAny(string path, IReadOnlyList<string> patterns, bool includeWhenEmpty)
    {
        if (patterns.Count == 0)
        {
            return includeWhenEmpty;
        }

        return patterns.Any(pattern => FileSystemName.MatchesSimpleExpression(pattern, path, ignoreCase: true));
    }
}
