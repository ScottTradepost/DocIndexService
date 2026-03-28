namespace DocIndexService.Application.Abstractions.Dashboard;

public sealed record DashboardStatsDto(
    int TotalSources,
    int ActiveSources,
    int TotalDocuments,
    int IndexedDocuments,
    int FailedJobs,
    int PendingJobs,
    int DeletedDocuments,
    DateTime? LastScanUtc,
    IReadOnlyList<RecentIngestionActivityDto> RecentActivity);

public sealed record RecentIngestionActivityDto(
    Guid JobId,
    Guid SourceId,
    Guid? DocumentId,
    string JobType,
    string Status,
    DateTime StartedUtc,
    DateTime? CompletedUtc,
    string? ErrorMessage);
