namespace DocIndexService.Application.Abstractions.SourceManagement;

public sealed record DocumentSourceDto(
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

public sealed record CreateDocumentSourceRequest(
    string Name,
    string RootPath,
    bool IsRecursive,
    string IncludePatterns,
    string ExcludePatterns,
    int ScanIntervalMinutes,
    bool IsEnabled);

public sealed record UpdateDocumentSourceRequest(
    string Name,
    string RootPath,
    bool IsRecursive,
    string IncludePatterns,
    string ExcludePatterns,
    int ScanIntervalMinutes,
    bool IsEnabled);

public sealed record SourceValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors,
    bool PathExists,
    bool PathIsAccessible);

public sealed record ManualScanRequest(
    Guid SourceId,
    bool FullReconciliation);

public sealed record SourceOperationResult(
    bool Succeeded,
    string Message);
