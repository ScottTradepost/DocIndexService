using DocIndexService.Contracts.Api.Search;

namespace DocIndexService.Application.Abstractions.Search;

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse> SimilarAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse> AskAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse> SummarizeAsync(SearchRequest request, CancellationToken cancellationToken);
    Task<SearchResponse> ExtractAsync(SearchRequest request, CancellationToken cancellationToken);
}
