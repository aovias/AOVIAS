using Microsoft.AspNetCore.Mvc;

namespace Aovias.Runtime.Agent.Api.Controllers;

/// <summary>
/// Exposes the platform health endpoint.
/// </summary>
[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    /// <summary>
    /// Gets the platform health status.
    /// </summary>
    /// <returns>A healthy status response.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new { status = "Healthy" });
    }
}
