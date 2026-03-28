namespace DocIndexService.Contracts.Api.Jobs;

public sealed record JobEventResponse(
    Guid Id,
    string EventType,
    string? Message,
    string DetailsJson,
    DateTime CreatedUtc);

public sealed record JobResponse(
    Guid Id,
    Guid SourceId,
    Guid? DocumentId,
    string JobType,
    string Status,
    DateTime StartedUtc,
    DateTime? CompletedUtc,
    string? ErrorMessage,
    int AttemptCount,
    string PayloadJson,
    DateTime CreatedUtc,
    DateTime UpdatedUtc,
    IReadOnlyCollection<JobEventResponse> Events);

public sealed record JobListResponse(
    IReadOnlyCollection<JobResponse> Items,
    int Count,
    DateTime UtcTimestamp);

public sealed record RetryJobResponse(
    bool Succeeded,
    string Message,
    Guid? NewJobId,
    DateTime UtcTimestamp);
