using ComplianceService.Application.Authorization;
using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Authorization;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1_1
{
	[ApiController]
	[ApiVersion("1.1")]
	[Route("api/v{version:apiVersion}/evidence")]
	[Authorize(Policy = Shared.Infrastructure.Auth.AuthConstants.Policies.ComplianceScope)]
	[ValidateHospitalId]
	public class EvidenceController : ControllerBase
	{
		private readonly IEvidenceService _evidenceService;

		public EvidenceController(IEvidenceService evidenceService)
		{
			_evidenceService = evidenceService;
		}

		[HttpGet("objective-elements/{objectiveElementId}")]
		public async Task<ActionResult<ApiResponse<object>>> GetForObjectiveElement(
			Guid objectiveElementId,
			[FromQuery] Evidence.EvidenceArtifactType? artifactType = null,
			[FromQuery] string? department = null,
			[FromQuery] Evidence.EvidenceLifecycleStatus? status = null,
			CancellationToken ct = default)
		{
			var traceId = HttpContext.TraceIdentifier;

			var items = await _evidenceService.GetEvidenceForObjectiveElementAsync(objectiveElementId, artifactType, department, status, ct);
			var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)items, traceId);
			var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
			return StatusCode(meta.HttpStatus, response);
		}

		public record VerifyRequest(Guid AuditorId, bool Accepted, string? Remarks);

		[Obsolete("Use POST /api/v1.0/evidence/{evidenceId}/approval via EvidenceReviewService. Retained for backward compatibility.")]
		[RequirePermission(CompliancePermissionCodes.EvidenceVerify)]
		[HttpPost("objective-elements/{objectiveElementId}/verify/{evidenceId}")]
		public async Task<ActionResult<ApiResponse<object>>> Verify(
			Guid objectiveElementId,
			Guid evidenceId,
			[FromBody] VerifyRequest body,
			CancellationToken ct = default)
		{
			var traceId = HttpContext.TraceIdentifier;

			await _evidenceService.SetVerificationAsync(objectiveElementId, evidenceId, body.AuditorId, body.Accepted, body.Remarks, ct);

			var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, new { objectiveElementId, evidenceId }, traceId);
			var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
			return StatusCode(meta.HttpStatus, response);
		}
	}
}
