using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Controllers.Internal;

/// <summary>
/// Internal endpoints exposing obligation data scoped to a journey context (hospitalId + frameworkId).
/// Used by the Gateway to proxy Phase 11 obligation endpoints to Angular.
/// </summary>
[ApiController]
[Route("internal/v1/journey-obligations")]
[Authorize(Policy = "InternalOnly")]
public class InternalJourneyObligationsController : ControllerBase
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<InternalJourneyObligationsController> _logger;

    public InternalJourneyObligationsController(
        ComplianceDbContext db,
        ILogger<InternalJourneyObligationsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Returns overview summary for obligations scoped to a hospital × framework.
    /// GET /internal/v1/journey-obligations/overview?hospitalId=&amp;frameworkId=
    /// </summary>
    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        if (hospitalId == Guid.Empty || frameworkId == Guid.Empty)
            return BadRequest(new { error = "hospitalId and frameworkId are required." });

        var allObligations = await _db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.HospitalId == hospitalId && o.FrameworkId == frameworkId)
            .ToListAsync(cancellationToken);

        var journeyReadiness = ObligationReadinessCalculator.CalculateJourneyReadiness(allObligations);

        var summary = new OrganizationObligationSummaryDto
        {
            OrganizationId = hospitalId,
            FrameworkId = frameworkId,
            TotalObligations = allObligations.Count,
            Implemented = allObligations.Count(o => o.ImplementationStatus == Domain.Enums.ImplementationStatus.Implemented),
            PartiallyImplemented = allObligations.Count(o => o.ImplementationStatus == Domain.Enums.ImplementationStatus.PartiallyImplemented),
            NotImplemented = allObligations.Count(o => o.ImplementationStatus == Domain.Enums.ImplementationStatus.NotImplemented),
            NotStarted = allObligations.Count(o => o.ImplementationStatus == Domain.Enums.ImplementationStatus.NotStarted),
            Verified = allObligations.Count(o => o.VerificationStatus == Domain.Enums.VerificationStatus.Verified),
            Passed = allObligations.Count(o => o.AssessmentStatus == Domain.Enums.ObligationAssessmentStatus.Passed),
            Failed = allObligations.Count(o => o.AssessmentStatus == Domain.Enums.ObligationAssessmentStatus.Failed),
            OverallReadinessScore = journeyReadiness,
        };

        var overview = new JourneyObligationOverviewDto
        {
            HospitalId = hospitalId,
            FrameworkId = frameworkId,
            Summary = summary,
            JourneyReadinessScore = journeyReadiness,
            TotalObligations = allObligations.Count,
        };

        return Ok(overview);
    }

    /// <summary>
    /// Returns the full obligation list for a hospital × framework.
    /// GET /internal/v1/journey-obligations?hospitalId=&amp;frameworkId=
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetObligations(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        if (hospitalId == Guid.Empty || frameworkId == Guid.Empty)
            return BadRequest(new { error = "hospitalId and frameworkId are required." });

        var rawObligations = await _db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.HospitalId == hospitalId && o.FrameworkId == frameworkId)
            .ToListAsync(cancellationToken);

        var items = rawObligations.Select(o => new OrganizationObligationDto
        {
            Id = o.Id,
            OrganizationId = o.OrganizationId,
            HospitalId = o.HospitalId,
            FrameworkId = o.FrameworkId,
            ChapterId = o.ChapterId,
            StandardId = o.StandardId,
            ObjectiveElementId = o.ObjectiveElementId,
            ComplianceNodeId = o.ComplianceNodeId,
            Code = o.Code,
            Title = o.Title,
            Description = o.Description,
            MaturityLevel = o.MaturityLevel.ToString(),
            RiskCategory = o.RiskCategory,
            Weight = o.Weight,
            ImplementationStatus = o.ImplementationStatus.ToString(),
            AssessmentStatus = o.AssessmentStatus.ToString(),
            JourneyStatus = o.JourneyStatus.ToString(),
            EvidenceStatus = o.EvidenceStatus.ToString(),
            VerificationStatus = o.VerificationStatus.ToString(),
            ReadinessScore = o.ReadinessScore,
            CompletionPercentage = o.CompletionPercentage,
            LastAssessmentDate = o.LastAssessmentDate,
            LastEvidenceDate = o.LastEvidenceDate,
            LastVerificationDate = o.LastVerificationDate,
            LastSnapshotRefreshDate = o.LastSnapshotRefreshDate,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            CreatedBy = o.CreatedBy,
            UpdatedBy = o.UpdatedBy,
        }).ToList();

        return Ok(new JourneyObligationsListDto
        {
            Items = items,
            TotalCount = items.Count,
            Page = 1,
            PageSize = items.Count,
        });
    }

    /// <summary>
    /// Returns a single obligation by its ID.
    /// GET /internal/v1/journey-obligations/{id}
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetObligationById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var obligation = await _db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new ObligationStatusDto
            {
                Id = o.Id,
                ObjectiveElementId = o.ObjectiveElementId,
                Code = o.Code,
                ImplementationStatus = o.ImplementationStatus.ToString(),
                AssessmentStatus = o.AssessmentStatus.ToString(),
                JourneyStatus = o.JourneyStatus.ToString(),
                EvidenceStatus = o.EvidenceStatus.ToString(),
                VerificationStatus = o.VerificationStatus.ToString(),
                ReadinessScore = o.ReadinessScore,
                CompletionPercentage = o.CompletionPercentage,
                UpdatedAt = o.UpdatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (obligation == null)
            return NotFound(new { error = $"Obligation {id} not found." });

        return Ok(obligation);
    }

    /// <summary>
    /// Returns per-level readiness breakdown for a hospital × framework.
    /// GET /internal/v1/journey-obligations/readiness?hospitalId=&amp;frameworkId=
    /// </summary>
    [HttpGet("readiness")]
    public async Task<IActionResult> GetReadiness(
        [FromQuery] Guid hospitalId,
        [FromQuery] Guid frameworkId,
        CancellationToken cancellationToken)
    {
        if (hospitalId == Guid.Empty || frameworkId == Guid.Empty)
            return BadRequest(new { error = "hospitalId and frameworkId are required." });

        var obligations = await _db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.HospitalId == hospitalId && o.FrameworkId == frameworkId)
            .ToListAsync(cancellationToken);

        var journeyReadiness = ObligationReadinessCalculator.CalculateJourneyReadiness(obligations);

        var byStage = new List<StageReadinessDto>
        {
            BuildStageReadiness(obligations, ObjectiveElementClassification.Core, "Core"),
            BuildStageReadiness(obligations, ObjectiveElementClassification.Commitment, "Commitment"),
            BuildStageReadiness(obligations, ObjectiveElementClassification.Achievement, "Achievement"),
            BuildStageReadiness(obligations, ObjectiveElementClassification.Excellence, "Excellence"),
        };

        var chapterProjections = await _db.OrganizationComplianceObligations
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.HospitalId == hospitalId && o.FrameworkId == frameworkId)
            .GroupBy(o => new { o.ChapterId })
            .Select(g => new ObligationReadinessProjectionDto
            {
                ChapterId = g.Key.ChapterId,
                TotalObligations = g.Count(),
                CompletedObligations = g.Count(o => o.VerificationStatus == Domain.Enums.VerificationStatus.Verified),
            })
            .ToListAsync(cancellationToken);

        return Ok(new JourneyReadinessDto
        {
            HospitalId = hospitalId,
            FrameworkId = frameworkId,
            JourneyReadinessScore = journeyReadiness,
            ByStage = byStage,
            ByChapter = chapterProjections.ToList(),
        });
    }

    private static StageReadinessDto BuildStageReadiness(
        List<Domain.OrganizationComplianceObligation> obligations,
        ObjectiveElementClassification stage,
        string label)
    {
        var stageObs = obligations.Where(o => o.MaturityLevel == stage).ToList();
        return new StageReadinessDto
        {
            Stage = stage.ToString().ToLowerInvariant(),
            Label = label,
            ReadinessScore = ObligationReadinessCalculator.CalculateStageReadiness(obligations, stage),
            TotalObligations = stageObs.Count,
        };
    }
}
