using AuditService.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuditService.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit")]
[Authorize(Policy = "AuditScope")]
public class AuditController : ControllerBase
{
    private readonly AuditAppService _service;
    private readonly ILogger<AuditController> _logger;

    public AuditController(AuditAppService service, ILogger<AuditController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateAuditRequest request, CancellationToken ct)
    {
        if (request.OrganizationId == Guid.Empty || request.ComplianceProgramId == Guid.Empty)
        {
            _logger.LogWarning("Invalid audit generation request: OrganizationId={OrganizationId}, ComplianceProgramId={ComplianceProgramId}",
                request.OrganizationId, request.ComplianceProgramId);
            return BadRequest("OrganizationId and ComplianceProgramId are required.");
        }

        _logger.LogInformation("Generating audit session for OrganizationId={OrganizationId}, ComplianceProgramId={ComplianceProgramId}",
            request.OrganizationId, request.ComplianceProgramId);

        var session = await _service.GenerateAuditAsync(
            request.OrganizationId,
            request.ComplianceProgramId,
            ct);

        _logger.LogInformation("Successfully generated audit session: SessionId={SessionId}, PlatformId={PlatformId}, OrganizationId={OrganizationId}",
            session.Id, session.PlatformId, request.OrganizationId);

        return Ok(session);
    }

    [HttpGet("{sessionId}")]
    public async Task<IActionResult> Get(Guid sessionId, CancellationToken ct)
    {
        if (sessionId == Guid.Empty)
        {
            return BadRequest("SessionId is required.");
        }

        _logger.LogInformation("Retrieving audit session: SessionId={SessionId}", sessionId);

        var session = await _service.GetAuditSessionAsync(sessionId, ct);
        
        if (session == null)
        {
            _logger.LogWarning("Audit session not found: SessionId={SessionId}", sessionId);
            return NotFound($"Audit session with ID {sessionId} not found.");
        }

        _logger.LogInformation("Retrieved audit session: SessionId={SessionId}, PlatformId={PlatformId}, Status={Status}",
            session.Id, session.PlatformId, session.Status);

        return Ok(session);
    }

    [HttpGet("organizations/{organizationId}")]
    public async Task<IActionResult> GetByOrganization(Guid organizationId, CancellationToken ct)
    {
        if (organizationId == Guid.Empty)
        {
            return BadRequest("OrganizationId is required.");
        }

        _logger.LogInformation("Retrieving audit sessions for OrganizationId={OrganizationId}", organizationId);

        var sessions = await _service.GetAuditSessionsByOrganizationAsync(organizationId, ct);

        _logger.LogInformation("Retrieved {Count} audit sessions for OrganizationId={OrganizationId}",
            sessions.Count, organizationId);

        return Ok(sessions);
    }
}

public record GenerateAuditRequest
{
    public Guid OrganizationId { get; init; }
    public Guid ComplianceProgramId { get; init; }
}

