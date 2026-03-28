namespace DocIndexService.Contracts.Api.Documents;

public sealed record DocumentResponse(
    Guid Id,
    Guid SourceId,
    string RelativePath,
    string FullPath,
    string FileName,
    string Extension,
    string? MimeType,
    string Sha256,
    long FileSize,
    DateTime FileLastModifiedUtc,
    string Status,
    bool IsDeleted,
    string? Title,
    string? Summary,
    DateTime? LastIndexedUtc,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record DocumentListResponse(
    IReadOnlyCollection<DocumentResponse> Items,
    int Count,
    DateTime UtcTimestamp);

public sealed record ReprocessDocumentResponse(
    bool Succeeded,
    string Message,
    Guid? JobId,
    DateTime UtcTimestamp);
