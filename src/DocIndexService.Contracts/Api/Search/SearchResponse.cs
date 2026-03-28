namespace DocIndexService.Contracts.Api.Search;

public sealed record SearchResponse(
    string Mode,
    string Message,
    IReadOnlyCollection<SearchResultItem> Results,
    DateTime UtcTimestamp);
