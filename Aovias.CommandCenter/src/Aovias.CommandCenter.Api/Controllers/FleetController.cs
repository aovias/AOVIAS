using Aovias.CommandCenter.Infrastructure;
using Aovias.Release.Contracts.RemoteDeployment;
using Microsoft.AspNetCore.Mvc;

namespace Aovias.CommandCenter.Api.Controllers;

/// <summary>
/// The Operations Command Center's fleet-wide aggregation and control APIs (Sprint M2.7) — the
/// brief's 11 required Command Center APIs, minus "List environments"/"Environment details"
/// (<see cref="EnvironmentsController"/>). <see cref="TriggerReconciliationAsync"/>,
/// <see cref="SetMaintenanceModeAsync"/>, and <see cref="ResumeFromMaintenanceAsync"/> are the
/// only actions here — every one of them calls straight through to the target Runtime Agent's own
/// existing API via <see cref="IFleetStatusService"/> (Final Engineering Rule: "Command Center
/// never performs provisioning directly").
/// </summary>
[ApiController]
[Route("api/fleet")]
[Produces("application/json")]
public sealed class FleetController : ControllerBase
{
    private readonly IFleetStatusService fleetStatusService;

    public FleetController(IFleetStatusService fleetStatusService)
    {
        this.fleetStatusService = fleetStatusService;
    }

    [HttpGet("health")]
    public IActionResult GetHealth() => Ok(fleetStatusService.GetFleetHealth());

    [HttpGet("drift")]
    public IActionResult GetDrift() => Ok(fleetStatusService.GetFleetDrift());

    [HttpGet("reconciliation")]
    public IActionResult GetReconciliation() => Ok(fleetStatusService.GetFleetReconciliation());

    [HttpGet("deployment-history")]
    public async Task<IActionResult> GetDeploymentHistoryAsync([FromQuery] int limitPerEnvironment, CancellationToken cancellationToken)
        => Ok(await fleetStatusService.GetFleetDeploymentHistoryAsync(limitPerEnvironment <= 0 ? 20 : limitPerEnvironment, cancellationToken));

    [HttpGet("metrics")]
    public IActionResult GetMetrics() => Ok(fleetStatusService.GetFleetMetrics());

    [HttpGet("events")]
    public IActionResult GetEvents([FromQuery] string? environmentId, [FromQuery] int limit = 100)
        => Ok(fleetStatusService.GetFleetEvents(environmentId, limit));

    [HttpPost("{environmentId}/reconcile")]
    public async Task<IActionResult> TriggerReconciliationAsync(string environmentId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await fleetStatusService.TriggerReconciliationAsync(environmentId, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    [HttpPost("{environmentId}/maintenance")]
    public async Task<IActionResult> SetMaintenanceModeAsync(
        string environmentId,
        [FromBody] MaintenanceModeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await fleetStatusService.SetMaintenanceModeAsync(environmentId, request, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }

    [HttpPost("{environmentId}/maintenance/resume")]
    public async Task<IActionResult> ResumeFromMaintenanceAsync(
        string environmentId,
        [FromBody] MaintenanceModeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await fleetStatusService.ResumeFromMaintenanceAsync(environmentId, request.RequestedBy, cancellationToken));
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new { error = exception.Message });
        }
    }
}
