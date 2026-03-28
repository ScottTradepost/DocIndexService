namespace DocIndexService.Core.Entities;

public sealed class DocumentChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
    public string Text { get; set; } = string.Empty;
    public int TokenCount { get; set; }
    public string? EmbeddingModel { get; set; }
    public string? EmbeddingVersion { get; set; }

    // TODO: Switch from nullable vector to concrete embedding data once generation pipeline is implemented.
    public float[]? Embedding { get; set; }

    public string MetadataJson { get; set; } = "{}";
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public Document? Document { get; set; }
}
