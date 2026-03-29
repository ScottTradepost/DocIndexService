using DocIndexService.Application.Abstractions.Api.Sources;
using DocIndexService.Contracts.Api.Sources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocIndexService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/sources")]
public sealed class SourcesController : ControllerBase
{
    private readonly ISourceApiService _sourceApiService;

    public SourcesController(ISourceApiService sourceApiService)
    {
        _sourceApiService = sourceApiService;
    }

    [HttpGet]
    public Task<ActionResult<SourceListResponse>> ListAsync(CancellationToken cancellationToken)
        => ExecuteAsync(() => _sourceApiService.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SourceResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _sourceApiService.GetAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<SourceActionResponse>> CreateAsync(
        [FromBody] CreateSourceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sourceApiService.CreateAsync(request, cancellationToken);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SourceActionResponse>> UpdateAsync(
        Guid id,
        [FromBody] UpdateSourceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sourceApiService.UpdateAsync(id, request, cancellationToken);
        if (!response.Succeeded && response.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(response);
        }

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{id:guid}/scan")]
    public async Task<ActionResult<SourceActionResponse>> ScanAsync(
        Guid id,
        [FromBody] ScanSourceRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sourceApiService.ScanAsync(id, request.FullReconciliation, cancellationToken);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    [HttpPost("{id:guid}/reindex")]
    public async Task<ActionResult<SourceActionResponse>> ReindexAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _sourceApiService.ReindexAsync(id, cancellationToken);
        return response.Succeeded ? Ok(response) : BadRequest(response);
    }

    private async Task<ActionResult<T>> ExecuteAsync<T>(Func<Task<T>> handler)
    {
        var response = await handler();
        return Ok(response);
    }
}
