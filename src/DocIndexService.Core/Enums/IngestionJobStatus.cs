namespace DocIndexService.Core.Enums;

public enum IngestionJobStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4
}
