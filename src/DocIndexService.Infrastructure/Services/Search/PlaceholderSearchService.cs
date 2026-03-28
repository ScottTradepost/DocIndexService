using DocIndexService.Application.Abstractions.Search;
using DocIndexService.Contracts.Api.Search;

namespace DocIndexService.Infrastructure.Services.Search;

public sealed class PlaceholderSearchService : ISearchService
{
    public Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
        => Task.FromResult(CreatePlaceholder("keyword", request));

    public Task<SearchResponse> SimilarAsync(SearchRequest request, CancellationToken cancellationToken)
        => Task.FromResult(CreatePlaceholder("similar", request));

    public Task<SearchResponse> AskAsync(SearchRequest request, CancellationToken cancellationToken)
        => Task.FromResult(CreatePlaceholder("ask", request));

    public Task<SearchResponse> SummarizeAsync(SearchRequest request, CancellationToken cancellationToken)
        => Task.FromResult(CreatePlaceholder("summarize", request));

    public Task<SearchResponse> ExtractAsync(SearchRequest request, CancellationToken cancellationToken)
        => Task.FromResult(CreatePlaceholder("extract", request));

    private static SearchResponse CreatePlaceholder(string mode, SearchRequest request)
    {
        return new SearchResponse(
            Mode: mode,
            Message: $"Placeholder {mode} response for query '{request.Query}'.",
            Results: Array.Empty<SearchResultItem>(),
            UtcTimestamp: DateTime.UtcNow);
    }
}
