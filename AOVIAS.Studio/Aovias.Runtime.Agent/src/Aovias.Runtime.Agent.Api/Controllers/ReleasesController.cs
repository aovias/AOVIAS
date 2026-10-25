using Aovias.Configuration.Deployment;
using Aovias.Configuration.Settings;
using Aovias.Release.Contracts;
using Aovias.Release.Contracts.Deployment;
using Aovias.Release.Contracts.Enums;
using Aovias.Release.Contracts.RemoteDeployment;
using Aovias.Release.Contracts.Releases;
using Aovias.Runtime.Agent.Api.Diagnostics;
using Aovias.Runtime.Agent.Application.Releases.Commands;
using Aovias.Runtime.Agent.Application.Releases.Queries;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Aovias.Runtime.Agent.Api.Controllers;

/// <summary>
/// Exposes release management endpoints for the AOVIAS Runtime Agent.
/// </summary>
[ApiController]
[Route("api/releases")]
[Produces("application/json")]
public sealed class ReleasesController : ControllerBase
{
    private readonly IMediator mediator;
    private readonly IDeploymentPathResolver pathResolver;
    private readonly IOptionsMonitor<ReleaseManagementSettings> releaseSettings;

    public ReleasesController(
        IMediator mediator,
        IDeploymentPathResolver pathResolver,
        IOptionsMonitor<ReleaseManagementSettings> releaseSettings)
    {
        this.mediator = mediator;
        this.pathResolver = pathResolver;
        this.releaseSettings = releaseSettings;
    }

    /// <summary>
    /// Uploads a release package to the runtime incoming folder.
    /// </summary>
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [RequestSizeLimit(524288000)]
    public async Task<IActionResult> UploadAsync(
        IFormFile package,
        [FromForm] string runtime,
        [FromForm] string environment,
        [FromForm] string uploadedBy,
        CancellationToken cancellationToken)
    {
        if (package is null || package.Length == 0)
        {
            return BadRequest("Package file is required.");
        }

        var extension = Path.GetExtension(package.FileName);
        if (!string.Equals(extension, PackageLayout.Extension, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(extension, ".aovdeploy", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Package must use the .aovpkg or .aovdeploy extension.");
        }

        try
        {
            DeploymentExecutionDiagnostics.BeginUploadTrace();
            DeploymentExecutionDiagnostics.LogPipelineStage("Controller", nameof(ReleasesController), nameof(UploadAsync));
            DeploymentExecutionDiagnostics.LogConfigurationSnapshot(
                releaseSettings.CurrentValue,
                pathResolver,
                "ReleasesController.UploadAsync");

            var storageValidation = pathResolver.ValidateStorageLayout(createMissing: false);
            if (!storageValidation.IsValid)
            {
                return MapStorageFailure(storageValidation, "Upload");
            }

            var settings = releaseSettings.CurrentValue;
            pathResolver.EnsureDirectory(settings.TemporaryFolder);
            var tempPath = Path.Combine(settings.TemporaryFolder, $"{Guid.NewGuid():N}{extension}");

            await using (var stream = System.IO.File.Create(tempPath))
            {
                await package.CopyToAsync(stream, cancellationToken);
            }

            var result = await mediator.Send(
                new UploadReleaseCommand(new UploadPackageRequest
                {
                    PackagePath = tempPath,
                    Runtime = runtime,
                    Environment = environment,
                    UploadedBy = uploadedBy
                }),
                cancellationToken);

            DeploymentExecutionDiagnostics.EndUploadTrace();
            return Ok(result);
        }
        catch (UnauthorizedAccessException exception)
        {
            DeploymentExecutionDiagnostics.EndUploadTrace();
            return MapPathException(exception, "Upload");
        }
        catch (IOException exception)
        {
            DeploymentExecutionDiagnostics.EndUploadTrace();
            return MapPathException(exception, "Upload");
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("legacy", StringComparison.OrdinalIgnoreCase)
                                                            || exception.Message.Contains("outside configured", StringComparison.OrdinalIgnoreCase))
        {
            DeploymentExecutionDiagnostics.EndUploadTrace();
            return MapPathException(exception, "Upload");
        }
        catch (Exception exception)
        {
            DeploymentExecutionDiagnostics.EndUploadTrace();
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                DeploymentFailureMapper.FromException(
                    exception,
                    phase: "Upload",
                    component: nameof(ReleasesController),
                    operation: "UploadAsync",
                    category: DeploymentFailureMapper.Classify(exception)));
        }
    }

    private IActionResult MapStorageFailure(Aovias.Configuration.Deployment.DeploymentStorageValidationResult validation, string phase)
    {
        var structured = validation.StructuredFailure;
        if (structured is not null)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new Aovias.Release.Contracts.Deployment.DeploymentFailure
            {
                Category = structured.Category,
                Phase = phase,
                Component = structured.Component,
                Operation = structured.Operation,
                ExceptionType = structured.ExceptionType,
                Message = BuildPathDiagnosticMessage(structured),
                Recovery = structured.Recovery,
                Path = structured.ResolvedPath
            });
        }

        return StatusCode(StatusCodes.Status500InternalServerError, new Aovias.Release.Contracts.Deployment.DeploymentFailure
        {
            Category = "Filesystem",
            Phase = phase,
            Component = nameof(ReleasesController),
            Operation = "ValidateStorageLayout",
            Message = validation.FailureMessage ?? "Deployment storage validation failed.",
            Recovery = "Verify deployment configuration and directory permissions."
        });
    }

    private IActionResult MapPathException(Exception exception, string phase)
    {
        var failure = DeploymentFailureMapper.FromException(
            exception,
            phase: phase,
            component: nameof(ReleasesController),
            operation: "UploadAsync",
            category: DeploymentFailureCategory.Infrastructure,
            path: ExtractPathFromException(exception));

        if (IsLegacyPathMismatch(failure.Path ?? exception.Message))
        {
            failure = new Aovias.Release.Contracts.Deployment.DeploymentFailure
            {
                Category = "Filesystem",
                Phase = phase,
                Component = nameof(ConfiguredDeploymentPathResolver),
                Operation = "ResolveUploadDirectory",
                ExceptionType = exception.GetType().Name,
                Message = BuildLegacyMismatchMessage(failure.Path ?? exception.Message),
                Recovery = "DeploymentPathResolver returned an unexpected path. Verify deployment configuration.",
                Path = failure.Path
            };
        }

        return StatusCode(StatusCodes.Status500InternalServerError, failure);
    }

    private static string BuildPathDiagnosticMessage(Aovias.Configuration.Deployment.DeploymentFailure failure)
    {
        var parts = new List<string> { failure.Message };
        if (!string.IsNullOrWhiteSpace(failure.DeploymentRoot))
        {
            parts.Add($"Deployment Root: {failure.DeploymentRoot}");
        }

        if (!string.IsNullOrWhiteSpace(failure.ExpectedPath))
        {
            parts.Add($"Expected: {failure.ExpectedPath}");
        }

        if (!string.IsNullOrWhiteSpace(failure.ResolvedPath))
        {
            parts.Add($"Resolved Upload Directory: {failure.ResolvedPath}");
            parts.Add("Status: Configuration Mismatch");
        }

        return string.Join("; ", parts);
    }

    private static string BuildLegacyMismatchMessage(string pathOrMessage)
    {
        var path = ExtractPathFromException(new Exception(pathOrMessage)) ?? pathOrMessage;
        return $"Deployment Root: /opt/aovias/deployments; Resolved Upload Directory: {path}; Status: Configuration Mismatch";
    }

    private static bool IsLegacyPathMismatch(string message)
        => message.Contains("/opt/aovias/releases", StringComparison.OrdinalIgnoreCase);

    private static string? ExtractPathFromException(Exception exception)
    {
        const string marker = "path '";
        var message = exception.Message;
        var start = message.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return null;
        }

        start += marker.Length;
        var end = message.IndexOf('\'', start);
        return end > start ? message[start..end] : null;
    }

    /// <summary>
    /// Validates a previously uploaded release package.
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateAsync([FromBody] ValidateReleaseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReleaseId))
        {
            return BadRequest("ReleaseId is required.");
        }

        try
        {
            var result = await mediator.Send(new ValidateReleaseCommand(request.ReleaseId), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (Exception exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// Generates a deployment plan for a release.
    /// </summary>
    [HttpPost("plan")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> PlanAsync([FromBody] PlanDeploymentRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ReleaseId))
        {
            return BadRequest("ReleaseId is required.");
        }

        try
        {
            var result = await mediator.Send(new PlanReleaseCommand(request), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (Exception exception)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, exception.Message);
        }
    }

    /// <summary>
    /// Queues a release deployment for execution.
    /// </summary>
    [HttpPost("deploy")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeployAsync([FromBody] DeployReleaseRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return DeploymentBoundaryDiagnostics.ValidationError(
                "Deploy release request body is required.",
                "Send a JSON DeployReleaseRequest payload.",
                endpoint: "/api/releases/deploy",
                expectedRequestType: nameof(DeployReleaseRequest));
        }

        if (string.IsNullOrWhiteSpace(request.ReleaseId))
        {
            return DeploymentBoundaryDiagnostics.ValidationError(
                "ReleaseId is required.",
                "Run Analyze to upload and register the release before deploying.",
                field: nameof(DeployReleaseRequest.ReleaseId),
                endpoint: "/api/releases/deploy",
                expectedRequestType: nameof(DeployReleaseRequest));
        }

        if (string.IsNullOrWhiteSpace(request.RequestedBy))
        {
            return DeploymentBoundaryDiagnostics.ValidationError(
                "RequestedBy is required.",
                "Provide the operator identity in the deploy request.",
                field: nameof(DeployReleaseRequest.RequestedBy),
                endpoint: "/api/releases/deploy",
                expectedRequestType: nameof(DeployReleaseRequest));
        }

        try
        {
            var result = await mediator.Send(new DeployReleaseCommand(request), cancellationToken);
            return Ok(result);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new DeploymentRequestValidationErrorDto
            {
                Reason = exception.Message,
                Recovery = "Re-upload and analyze the package, then deploy again.",
                Field = nameof(DeployReleaseRequest.ReleaseId),
                Endpoint = "/api/releases/deploy",
                ExpectedRequestType = nameof(DeployReleaseRequest)
            });
        }
        catch (InvalidOperationException exception)
        {
            return DeploymentBoundaryDiagnostics.ValidationError(
                exception.Message,
                "Complete release validation and planning before deploying.",
                endpoint: "/api/releases/deploy",
                expectedRequestType: nameof(DeployReleaseRequest));
        }
        catch (Exception exception)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new
                {
                    category = "Infrastructure",
                    reason = exception.Message,
                    recovery = "Review Runtime Agent logs for deployment service exceptions."
                });
        }
    }

    /// <summary>
    /// Queues a release rollback for execution.
    /// </summary>
    [HttpPost("rollback")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RollbackAsync([FromBody] RollbackReleaseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await mediator.Send(new RollbackReleaseCommand(request), cancellationToken);
            return Ok(result);
        }
        catch (Exception exception)
        {
            return BadRequest(exception.Message);
        }
    }

    /// <summary>
    /// Gets all releases tracked by the runtime agent.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReleasesAsync(CancellationToken cancellationToken)
    {
        var releases = await mediator.Send(new GetReleasesQuery(), cancellationToken);
        return Ok(releases);
    }

    /// <summary>
    /// Gets release history entries.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistoryAsync(
        [FromQuery] string? runtime,
        [FromQuery] ReleaseState? status,
        [FromQuery] string? operatorName,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken cancellationToken)
    {
        var history = await mediator.Send(
            new GetReleaseHistoryQuery(new ReleaseHistorySearchCriteria
            {
                Runtime = runtime,
                State = status,
                Operator = operatorName,
                From = from,
                To = to
            }),
            cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// Gets deployment history detail events.
    /// </summary>
    [HttpGet("history/{deploymentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistoryDetailAsync(string deploymentId, CancellationToken cancellationToken)
    {
        var detail = await mediator.Send(new GetReleaseHistoryDetailQuery(deploymentId), cancellationToken);
        return Ok(detail);
    }

    /// <summary>
    /// Gets a release by identifier.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReleaseAsync(string id, CancellationToken cancellationToken)
    {
        var release = await mediator.Send(new GetReleaseByIdQuery(id), cancellationToken);
        return release is null ? NotFound() : Ok(release);
    }
}

/// <summary>
/// Validate release request payload.
/// </summary>
public sealed class ValidateReleaseRequest
{
    /// <summary>
    /// Gets or sets the release identifier.
    /// </summary>
    public required string ReleaseId { get; init; }
}
