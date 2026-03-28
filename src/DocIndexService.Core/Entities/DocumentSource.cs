namespace DocIndexService.Core.Entities;

public sealed class DocumentSource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RootPath { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsRecursive { get; set; } = true;
    public string IncludePatterns { get; set; } = "*";
    public string ExcludePatterns { get; set; } = string.Empty;
    public int ScanIntervalMinutes { get; set; } = 15;
    public DateTime? LastScanUtc { get; set; }
    public DateTime? LastSuccessfulScanUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<IngestionJob> IngestionJobs { get; set; } = new List<IngestionJob>();
}
