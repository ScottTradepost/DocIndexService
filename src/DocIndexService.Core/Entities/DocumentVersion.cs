namespace DocIndexService.Core.Entities;

public sealed class DocumentVersion
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int VersionNumber { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public DateTime FileLastModifiedUtc { get; set; }
    public string? ExtractedTextPath { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }

    public Document? Document { get; set; }
}
