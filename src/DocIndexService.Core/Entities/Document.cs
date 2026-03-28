using DocIndexService.Core.Enums;

namespace DocIndexService.Core.Entities;

public sealed class Document
{
    public Guid Id { get; set; }
    public Guid SourceId { get; set; }
    public string RelativePath { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime FileLastModifiedUtc { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    public bool IsDeleted { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public DateTime? LastIndexedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public string MetadataJson { get; set; } = "{}";

    public DocumentSource? Source { get; set; }
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
    public ICollection<DocumentVersion> Versions { get; set; } = new List<DocumentVersion>();
    public ICollection<IngestionJob> IngestionJobs { get; set; } = new List<IngestionJob>();
}
