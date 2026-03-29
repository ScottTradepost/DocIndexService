using DocIndexService.Application.Abstractions.Api.Documents;
using DocIndexService.Contracts.Api.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocIndexService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentApiService _documentApiService;

    public DocumentsController(IDocumentApiService documentApiService)
    {
        _documentApiService = documentApiService;
    }

    [HttpGet]
    public async Task<ActionResult<DocumentListResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var response = await _documentApiService.ListAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _documentApiService.GetAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/reprocess")]
    public async Task<ActionResult<ReprocessDocumentResponse>> ReprocessAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _documentApiService.ReprocessAsync(id, cancellationToken);
        if (!response.Succeeded && response.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(response);
        }

        return response.Succeeded ? Ok(response) : BadRequest(response);
    }
}
