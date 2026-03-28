namespace DocIndexService.Contracts.Api.Health;

public sealed record HealthStatusResponse(
    string Status,
    DateTime UtcTimestamp,
    IReadOnlyDictionary<string, string>? Dependencies = null);
