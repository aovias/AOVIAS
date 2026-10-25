using Aovias.CommandCenter.Infrastructure;
using Aovias.Release.Contracts.RemoteDeployment;
using Microsoft.AspNetCore.Mvc;

namespace Aovias.CommandCenter.Api.Controllers;

/// <summary>
/// Fleet Registry endpoints (Sprint M2.7): registration, listing, fleet-wide search/filter (by
/// text, environment type, labels/tags, online/maintenance status), and per-environment details.
/// </summary>
[ApiController]
[Route("api/environments")]
[Produces("application/json")]
public sealed class EnvironmentsController : ControllerBase
{
    private readonly IFleetStatusService fleetStatusService;

    public EnvironmentsController(IFleetStatusService fleetStatusService)
    {
        this.fleetStatusService = fleetStatusService;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] FleetRegistrationRequest request)
        => Ok(fleetStatusService.RegisterEnvironment(request));

    /// <summary>Fleet-wide search/filter — every parameter is optional and AND-combined; an empty query lists every registered environment.</summary>
    [HttpGet]
    public IActionResult List(
        [FromQuery] string? text,
        [FromQuery] EnvironmentType? environmentType,
        [FromQuery] bool? onlineOnly,
        [FromQuery] bool? maintenanceOnly,
        [FromQuery] Dictionary<string, string>? labels)
    {
        var query = new FleetSearchQuery
        {
            Text = text,
            EnvironmentType = environmentType,
            OnlineOnly = onlineOnly,
            MaintenanceOnly = maintenanceOnly,
            Labels = labels ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };

        return Ok(fleetStatusService.ListEnvironments(query));
    }

    [HttpGet("{environmentId}")]
    public IActionResult GetDetails(string environmentId)
    {
        var details = fleetStatusService.GetEnvironmentDetails(environmentId);
        return details is null ? NotFound() : Ok(details);
    }
}
