using DocIndexService.Application.Abstractions.Api.Jobs;
using DocIndexService.Contracts.Api.Jobs;
using Microsoft.AspNetCore.Mvc;

namespace DocIndexService.Api.Controllers;

[ApiController]
[Route("api/v1/jobs")]
public sealed class JobsController : ControllerBase
{
    private readonly IJobApiService _jobApiService;

    public JobsController(IJobApiService jobApiService)
    {
        _jobApiService = jobApiService;
    }

    [HttpGet]
    public async Task<ActionResult<JobListResponse>> ListAsync(CancellationToken cancellationToken)
    {
        var response = await _jobApiService.ListAsync(cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobResponse>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _jobApiService.GetAsync(id, cancellationToken);
        return response is null ? NotFound() : Ok(response);
    }

    [HttpPost("{id:guid}/retry")]
    public async Task<ActionResult<RetryJobResponse>> RetryAsync(Guid id, CancellationToken cancellationToken)
    {
        var response = await _jobApiService.RetryAsync(id, cancellationToken);
        if (!response.Succeeded)
        {
            return NotFound(response);
        }

        return Ok(response);
    }
}
