using Aovias.Runtime.Agent.Application.Health.Queries;
using Aovias.Runtime.Agent.Application.Monitoring.Queries;
using Aovias.Runtime.Agent.Application.Observability.Queries;
using Aovias.Runtime.Agent.Application.Runtime.Queries;
using Aovias.Runtime.Agent.Infrastructure.SignalR;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Aovias.Runtime.Agent.Api.Controllers;

/// <summary>
/// Exposes runtime control plane discovery endpoints.
/// </summary>
[ApiController]
[Route("api/runtime")]
[Produces("application/json")]
public sealed class RuntimeController : ControllerBase
{
    private readonly IMediator mediator;
    private readonly RuntimeCapabilitiesProvider capabilitiesProvider;
    private readonly Microsoft.Extensions.Options.IOptionsMonitor<Aovias.Configuration.Settings.RuntimeSettings> runtimeSettings;

    /// <summary>
    /// Initializes a new instance of the <see cref="RuntimeController"/> class.
    /// </summary>
    public RuntimeController(
        IMediator mediator,
        RuntimeCapabilitiesProvider capabilitiesProvider,
        Microsoft.Extensions.Options.IOptionsMonitor<Aovias.Configuration.Settings.RuntimeSettings> runtimeSettings)
    {
        this.mediator = mediator;
        this.capabilitiesProvider = capabilitiesProvider;
        this.runtimeSettings = runtimeSettings;
    }

    /// <summary>
    /// Gets the canonical runtime manifest.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The runtime manifest.</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuntimeAsync(CancellationToken cancellationToken)
    {
        var manifest = await mediator.Send(new GetRuntimeManifestQuery(), cancellationToken);
        return Ok(manifest);
    }

    /// <summary>
    /// Gets the runtime health snapshot.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>The runtime health snapshot.</returns>
    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuntimeHealthAsync(CancellationToken cancellationToken)
    {
        var health = await mediator.Send(new GetRuntimeHealthQuery(), cancellationToken);
        return Ok(health);
    }

    /// <summary>
    /// Gets runtime host information.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Runtime host information.</returns>
    [HttpGet("info")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuntimeInfoAsync(CancellationToken cancellationToken)
    {
        var info = await mediator.Send(new GetRuntimeInfoQuery(), cancellationToken);
        return Ok(info);
    }

    /// <summary>
    /// Gets runtime version metadata.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Runtime version metadata.</returns>
    [HttpGet("version")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuntimeVersionAsync(CancellationToken cancellationToken)
    {
        var version = await mediator.Send(new GetRuntimeVersionQuery(), cancellationToken);
        return Ok(version);
    }

    /// <summary>
    /// Gets runtime environment metadata.
    /// </summary>
    /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
    /// <returns>Runtime environment metadata.</returns>
    [HttpGet("environment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuntimeEnvironmentAsync(CancellationToken cancellationToken)
    {
        var environment = await mediator.Send(new GetRuntimeEnvironmentQuery(), cancellationToken);
        return Ok(environment);
    }

    /// <summary>
    /// Gets runtime monitoring statistics.
    /// </summary>
    [HttpGet("statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuntimeStatisticsAsync(CancellationToken cancellationToken)
    {
        var statistics = await mediator.Send(new GetRuntimeStatisticsQuery(), cancellationToken);
        return Ok(statistics);
    }

    /// <summary>
    /// Gets runtime capabilities advertised by the control plane.
    /// </summary>
    [HttpGet("capabilities")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetCapabilities()
    {
        var capabilities = capabilitiesProvider.GetCapabilities(runtimeSettings.CurrentValue.RuntimeVersion);
        return Ok(new Aovias.Runtime.Contracts.Runtime.RuntimeCapabilitiesDto
        {
            RuntimeVersion = capabilities.RuntimeVersion,
            Capabilities = capabilities.Capabilities
        });
    }

    /// <summary>
    /// Gets the current runtime observability state snapshot.
    /// </summary>
    [HttpGet("state")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuntimeStateAsync(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetRuntimeStateQuery(), cancellationToken));

    /// <summary>
    /// Gets the runtime observability timeline.
    /// </summary>
    [HttpGet("timeline")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuntimeTimelineAsync(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(new GetRuntimeTimelineQuery(search, category, limit), cancellationToken));

    /// <summary>
    /// Gets recent runtime metric snapshots.
    /// </summary>
    [HttpGet("metrics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuntimeMetricsAsync([FromQuery] int limit = 60, CancellationToken cancellationToken = default)
        => Ok(await mediator.Send(new GetRuntimeMetricsQuery(limit), cancellationToken));

    /// <summary>
    /// Gets observed containers from runtime state.
    /// </summary>
    [HttpGet("containers")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuntimeContainersAsync(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetRuntimeContainersQuery(), cancellationToken));

    /// <summary>
    /// Gets observed networks from runtime state.
    /// </summary>
    [HttpGet("networks")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuntimeNetworksAsync(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetRuntimeNetworksQuery(), cancellationToken));

    /// <summary>
    /// Gets observed releases from runtime state.
    /// </summary>
    [HttpGet("releases")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuntimeReleasesAsync(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetRuntimeReleasesQuery(), cancellationToken));

    /// <summary>
    /// Gets observed workflows from runtime state.
    /// </summary>
    [HttpGet("workflows")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRuntimeWorkflowsAsync(CancellationToken cancellationToken)
        => Ok(await mediator.Send(new GetRuntimeWorkflowsQuery(), cancellationToken));
}
