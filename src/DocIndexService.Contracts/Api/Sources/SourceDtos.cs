namespace DocIndexService.Contracts.Api.Sources;

public sealed record SourceResponse(
    Guid Id,
    string Name,
    string RootPath,
    bool IsEnabled,
    bool IsRecursive,
    string IncludePatterns,
    string ExcludePatterns,
    int ScanIntervalMinutes,
    DateTime? LastScanUtc,
    DateTime? LastSuccessfulScanUtc,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record SourceListResponse(
    IReadOnlyCollection<SourceResponse> Items,
    int Count,
    DateTime UtcTimestamp);

public sealed record CreateSourceRequest(
    string Name,
    string RootPath,
    bool IsRecursive,
    string IncludePatterns,
    string ExcludePatterns,
    int ScanIntervalMinutes,
    bool IsEnabled);

public sealed record UpdateSourceRequest(
    string Name,
    string RootPath,
    bool IsRecursive,
    string IncludePatterns,
    string ExcludePatterns,
    int ScanIntervalMinutes,
    bool IsEnabled);

public sealed record SourceActionResponse(
    bool Succeeded,
    string Message,
    DateTime UtcTimestamp);

public sealed record ScanSourceRequest(bool FullReconciliation);
