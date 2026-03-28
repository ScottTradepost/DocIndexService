namespace DocIndexService.Contracts.Api.Search;

public sealed record SearchRequest(
    string Query,
    int Limit = 20,
    int Offset = 0);
