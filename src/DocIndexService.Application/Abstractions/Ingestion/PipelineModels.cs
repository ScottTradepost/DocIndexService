namespace DocIndexService.Application.Abstractions.Ingestion;

public enum IngestionOperation
{
    New = 0,
    Updated = 1,
    Deleted = 2,
    Reconcile = 3
}

public sealed record ScannedFileEntry(
    string RelativePath,
    string FullPath,
    DateTime LastModifiedUtc,
    long FileSize,
    string Extension);

public sealed record SourceScanSnapshot(
    Guid SourceId,
    string RootPath,
    DateTime ScannedAtUtc,
    IReadOnlyList<ScannedFileEntry> Files,
    bool FullReconciliation);

public sealed record FileFingerprintResult(
    string Sha256,
    DateTime LastModifiedUtc,
    long FileSize);

public sealed record TextExtractionResult(
    string Text,
    string MimeType,
    bool IsPlaceholder);

public sealed record TextChunk(
    int ChunkIndex,
    string Text,
    int TokenCount,
    int? PageStart = null,
    int? PageEnd = null);

public sealed record EmbeddedChunk(
    TextChunk Chunk,
    IReadOnlyList<float> Vector,
    string EmbeddingModel,
    string EmbeddingVersion);

public sealed record PipelineExecutionResult(
    bool Succeeded,
    string? ErrorMessage = null);
