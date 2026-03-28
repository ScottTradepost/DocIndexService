using DocIndexService.Core.Enums;

namespace DocIndexService.Core.Entities;

public sealed class IngestionJob
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public Guid? DocumentId { get; set; }
    public IngestionJobType JobType { get; set; }
    public IngestionJobStatus Status { get; set; } = IngestionJobStatus.Pending;
    public DateTime StartedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public DocumentSource? Source { get; set; }
    public Document? Document { get; set; }
    public ICollection<IngestionJobEvent> Events { get; set; } = new List<IngestionJobEvent>();
}
