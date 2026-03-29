using DocIndexService.Contracts.Api.Health;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocIndexService.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthStatusResponse> Get()
    {
        return Ok(new HealthStatusResponse(
            Status: "ok",
            UtcTimestamp: DateTime.UtcNow));
    }

    [HttpGet("dependencies")]
    public ActionResult<HealthStatusResponse> GetDependencies()
    {
        var dependencies = new Dictionary<string, string>
        {
            ["postgres"] = "not-checked",
            ["tika"] = "not-checked",
            ["ollama"] = "not-checked"
        };

        // TODO: Add real dependency probes once infra clients are implemented.
        return Ok(new HealthStatusResponse(
            Status: "degraded",
            UtcTimestamp: DateTime.UtcNow,
            Dependencies: dependencies));
    }
}
