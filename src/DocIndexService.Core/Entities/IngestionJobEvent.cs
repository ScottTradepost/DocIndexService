namespace DocIndexService.Core.Entities;

public sealed class IngestionJobEvent
{
    public Guid Id { get; set; }
    public Guid IngestionJobId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string DetailsJson { get; set; } = "{}";
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public IngestionJob? IngestionJob { get; set; }
}
