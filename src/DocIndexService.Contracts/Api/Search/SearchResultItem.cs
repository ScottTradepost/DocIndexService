namespace DocIndexService.Contracts.Api.Search;

public sealed record SearchResultItem(
    Guid DocumentId,
    string? Title,
    string Path,
    double Score,
    string? Snippet,
    string? Summary,
    int? PageStart,
    int? PageEnd);
