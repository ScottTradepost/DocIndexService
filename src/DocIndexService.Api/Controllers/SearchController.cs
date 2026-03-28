using DocIndexService.Application.Abstractions.Search;
using DocIndexService.Contracts.Api.Search;
using Microsoft.AspNetCore.Mvc;

namespace DocIndexService.Api.Controllers;

[ApiController]
[Route("api/v1")]
public sealed class SearchController : ControllerBase
{
    private readonly ISearchService _searchService;

    public SearchController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpPost("search")]
    public async Task<ActionResult<SearchResponse>> SearchAsync(
        [FromBody] SearchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _searchService.SearchAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("search/similar")]
    public async Task<ActionResult<SearchResponse>> SimilarAsync(
        [FromBody] SearchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _searchService.SimilarAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("ask")]
    public async Task<ActionResult<SearchResponse>> AskAsync(
        [FromBody] SearchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _searchService.AskAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("summarize")]
    public async Task<ActionResult<SearchResponse>> SummarizeAsync(
        [FromBody] SearchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _searchService.SummarizeAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("extract")]
    public async Task<ActionResult<SearchResponse>> ExtractAsync(
        [FromBody] SearchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _searchService.ExtractAsync(request, cancellationToken);
        return Ok(response);
    }
}
