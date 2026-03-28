namespace DocIndexService.Core.Options;

public sealed class ScanOptions
{
    public const string SectionName = "Scanning";

    public int IncrementalIntervalMinutes { get; init; } = 15;
    public int FullReconciliationHourUtc { get; init; } = 2;
    public int MaxFileSizeMb { get; init; } = 50;
}
