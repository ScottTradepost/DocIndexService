namespace DocIndexService.Contracts.Persistence;

public sealed record UserDto(
    Guid Id,
    string UserName,
    string Email,
    bool IsEnabled,
    DateTime? LastLoginUtc,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record UserRoleDto(
    Guid UserId,
    Guid RoleId,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record ApiClientDto(
    Guid Id,
    string Name,
    string ClientId,
    string AllowedScopes,
    bool IsEnabled,
    DateTime? LastUsedUtc,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record DocumentSourceDto(
    Guid Id,
    string Name,
    string RootPath,
    bool IsEnabled,
    bool IsRecursive,
    string IncludePatterns,
    string ExcludePatterns,
    int ScanIntervalMinutes,
    DateTime? LastScanUtc,
    DateTime? LastSuccessfulScanUtc,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record DocumentDto(
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
    string MetadataJson,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record DocumentVersionDto(
    Guid Id,
    Guid DocumentId,
    int VersionNumber,
    string Sha256,
    DateTime FileLastModifiedUtc,
    string? ExtractedTextPath,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record DocumentChunkDto(
    Guid Id,
    Guid DocumentId,
    int ChunkIndex,
    int? PageStart,
    int? PageEnd,
    string Text,
    int TokenCount,
    float[]? Embedding,
    string? EmbeddingModel,
    string? EmbeddingVersion,
    string MetadataJson,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record IngestionJobDto(
    Guid Id,
    Guid SourceId,
    Guid? DocumentId,
    string JobType,
    string Status,
    DateTime StartedUtc,
    DateTime? CompletedUtc,
    string? ErrorMessage,
    int AttemptCount,
    string PayloadJson,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record IngestionJobEventDto(
    Guid Id,
    Guid IngestionJobId,
    string EventType,
    string? Message,
    string DetailsJson,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record AuditLogDto(
    Guid Id,
    Guid? UserId,
    string ActionType,
    string EntityType,
    string EntityId,
    string DetailsJson,
    string? IpAddress,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);

public sealed record SystemSettingDto(
    Guid Id,
    string Key,
    string Value,
    bool IsSensitive,
    string? Description,
    DateTime CreatedUtc,
    DateTime UpdatedUtc);
