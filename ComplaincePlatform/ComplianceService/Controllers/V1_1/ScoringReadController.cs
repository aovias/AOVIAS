using ComplianceService.Application.Aggregation;
using ComplianceService.Application.Decision;
using ComplianceService.Domain;
using ComplianceService.Domain.Policies;
using ComplianceService.Infrastructure.Filters;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.Domain.Enums;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Controllers.V1_1
{
    [ApiController]
    [ApiVersion("1.1")]
    [Route("api/v{version:apiVersion}/scoring")]
    [Authorize(Policy = Shared.Infrastructure.Auth.AuthConstants.Policies.ComplianceScope)]
    [ValidateHospitalId]
    public class ScoringReadController : ControllerBase
    {
        private readonly IAggregationService _aggregation;
        private readonly DecisionService _decision;
        private readonly ComplianceDbContext _db;
        private readonly Application.Contracts.IComplianceScoringService _appScoring;
        private readonly IOperationalModeClassificationPolicy _operationalModeClassification;

        public ScoringReadController(
            IAggregationService aggregation,
            DecisionService decision,
            ComplianceDbContext db,
            Application.Contracts.IComplianceScoringService appScoring,
            IOperationalModeClassificationPolicy operationalModeClassification)
        {
            _aggregation = aggregation;
            _decision = decision;
            _db = db;
            _appScoring = appScoring;
            _operationalModeClassification = operationalModeClassification;
        }

        [HttpGet("hospitals/{hospitalId}/frameworks/{frameworkId}/journeys/{journeyId}/aggregate")]
        public async Task<ActionResult<ApiResponse<object>>> GetAggregate(
            Guid hospitalId,
            Guid frameworkId,
            Guid journeyId,
            [FromQuery] OperationalMode operationalMode = OperationalMode.FinalAudit,
            CancellationToken ct = default)
        {
            var traceId = HttpContext.TraceIdentifier;

            // Validate journey exists and belongs to hospital
            var journey = await _db.ComplianceJourneys.FindAsync(new object[] { journeyId }, ct);
            if (journey == null || journey.HospitalId != hospitalId || journey.FrameworkId != frameworkId)
            {
                var notFound = ApiResponseFactory.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
                var meta404 = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(meta404.HttpStatus, notFound);
            }

            var included = _operationalModeClassification.GetClassificationsForWorkspace(operationalMode);

            var agg = await _aggregation.ComputeFrameworkAsync(frameworkId, journeyId, included, ct);
            var decision = await _decision.EvaluateAsync(frameworkId, journeyId, operationalMode, ct);

            var payload = new
            {
                Aggregate = agg,
                Decision = decision
            };

            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, (object)payload, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }

        [HttpGet("hospitals/{hospitalId}/frameworks/{frameworkId}/score-enriched")]
        public async Task<ActionResult<ApiResponse<ComplianceService.Application.ComplianceScoring.ComplianceScoreResult>>> GetEnrichedScore(
            Guid hospitalId,
            Guid frameworkId,
            CancellationToken ct = default)
        {
            var traceId = HttpContext.TraceIdentifier;

            var framework = await _db.ComplianceFrameworks.FindAsync(new object[] { frameworkId }, ct);
            if (framework == null)
            {
                var notFound = ApiResponseFactory.Failure<ComplianceService.Application.ComplianceScoring.ComplianceScoreResult>(ApiResponseCode.RESOURCE_NOT_FOUND, traceId);
                var meta404 = ApiResponseCodes.Registry[ApiResponseCode.RESOURCE_NOT_FOUND];
                return StatusCode(meta404.HttpStatus, notFound);
            }

            var result = await _appScoring.CalculateFrameworkScoreAsync(hospitalId, frameworkId);
            var response = ApiResponseFactory.Success(ApiResponseCode.SUCCESS, result, traceId);
            var meta = ApiResponseCodes.Registry[ApiResponseCode.SUCCESS];
            return StatusCode(meta.HttpStatus, response);
        }
    }
}

