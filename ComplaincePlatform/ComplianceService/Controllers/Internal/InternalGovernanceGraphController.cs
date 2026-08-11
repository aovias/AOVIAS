using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.Internal;

[ApiController]
[Route("internal/v1/governance-graph")]
[Authorize(Policy = "InternalOnly")]
public sealed class InternalGovernanceGraphController(ComplianceDbContext db) : ControllerBase
{
    [HttpPost("trace-links")]
    public async Task<ActionResult<ApiResponse<object>>> UpsertLink(
        [FromBody] UpsertGovernanceTraceLinkRequest request,
        CancellationToken ct = default)
    {
        if (request.OrganizationId == Guid.Empty || request.SourceEntityId == Guid.Empty || request.TargetEntityId == Guid.Empty)
            return BadRequest(ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, HttpContext.TraceIdentifier));

        var exists = await db.GovernanceTraceLinks.AnyAsync(l =>
            l.OrganizationId == request.OrganizationId
            && l.SourceEntityId == request.SourceEntityId
            && l.TargetEntityId == request.TargetEntityId
            && l.RelationshipType == request.RelationshipType, ct);

        if (!exists)
        {
            db.GovernanceTraceLinks.Add(new GovernanceTraceLink
            {
                Id = Guid.NewGuid(),
                OrganizationId = request.OrganizationId,
                SourceEntityType = request.SourceEntityType.Trim(),
                SourceEntityId = request.SourceEntityId,
                SourceLabel = string.IsNullOrWhiteSpace(request.SourceLabel) ? request.SourceEntityType : request.SourceLabel.Trim(),
                TargetEntityType = request.TargetEntityType.Trim(),
                TargetEntityId = request.TargetEntityId,
                TargetLabel = string.IsNullOrWhiteSpace(request.TargetLabel) ? request.TargetEntityType : request.TargetLabel.Trim(),
                RelationshipType = string.IsNullOrWhiteSpace(request.RelationshipType) ? "LinkedTo" : request.RelationshipType.Trim(),
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);
        }

        return Ok(ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { }, HttpContext.TraceIdentifier));
    }
}
