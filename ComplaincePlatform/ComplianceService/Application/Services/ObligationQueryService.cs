using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Read-side query service for OrganizationComplianceObligation projections.
/// All queries use IgnoreQueryFilters where the caller supplies an explicit organizationId
/// to support both tenant-scoped and internal/admin queries safely.
/// </summary>
public class ObligationQueryService : IObligationQueryService
{
    private readonly ComplianceDbContext _db;
    private readonly ILogger<ObligationQueryService> _logger;

    public ObligationQueryService(
        ComplianceDbContext db,
        ILogger<ObligationQueryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OrganizationObligationDto>> GetObligationsAsync(
        Guid organizationId,
        Guid frameworkId,
        CancellationToken ct = default)
    {
        var obligations = await _db.OrganizationComplianceObligations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.OrganizationId == organizationId && o.FrameworkId == frameworkId)
            .OrderBy(o => o.MaturityLevel)
            .ThenBy(o => o.Code)
            .ToListAsync(ct);

        return obligations.Select(MapToDto).ToList();
    }

    public async Task<OrganizationObligationSummaryDto> GetSummaryAsync(
        Guid organizationId,
        Guid frameworkId,
        CancellationToken ct = default)
    {
        var obligations = await _db.OrganizationComplianceObligations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.OrganizationId == organizationId && o.FrameworkId == frameworkId)
            .ToListAsync(ct);

        if (obligations.Count == 0)
        {
            return new OrganizationObligationSummaryDto
            {
                OrganizationId = organizationId,
                FrameworkId = frameworkId
            };
        }

        var breakdown = BuildMaturityBreakdown(obligations);
        var overallScore = obligations.Count > 0
            ? obligations.Average(o => (double)o.ReadinessScore)
            : 0d;

        return new OrganizationObligationSummaryDto
        {
            OrganizationId = organizationId,
            FrameworkId = frameworkId,
            TotalObligations = obligations.Count,
            Implemented = obligations.Count(o => o.ImplementationStatus == ImplementationStatus.Implemented),
            PartiallyImplemented = obligations.Count(o => o.ImplementationStatus == ImplementationStatus.PartiallyImplemented),
            NotImplemented = obligations.Count(o => o.ImplementationStatus == ImplementationStatus.NotImplemented),
            NotStarted = obligations.Count(o => o.ImplementationStatus == ImplementationStatus.NotStarted
                                              || o.ImplementationStatus == ImplementationStatus.Unknown),
            Verified = obligations.Count(o => o.VerificationStatus == VerificationStatus.Verified),
            Passed = obligations.Count(o => o.AssessmentStatus == ObligationAssessmentStatus.Passed),
            Failed = obligations.Count(o => o.AssessmentStatus == ObligationAssessmentStatus.Failed),
            OverallReadinessScore = (decimal)overallScore,
            MaturityBreakdown = breakdown
        };
    }

    public async Task<ObligationMaturityBreakdownDto> GetMaturityBreakdownAsync(
        Guid organizationId,
        Guid frameworkId,
        CancellationToken ct = default)
    {
        var obligations = await _db.OrganizationComplianceObligations
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(o => o.OrganizationId == organizationId && o.FrameworkId == frameworkId)
            .ToListAsync(ct);

        return BuildMaturityBreakdown(obligations);
    }

    public async Task<IReadOnlyList<ObligationReadinessProjectionDto>> GetReadinessProjectionAsync(
        Guid organizationId,
        Guid frameworkId,
        CancellationToken ct = default)
    {
        // Join obligations with chapter data for readable chapter codes/titles.
        var result = await (
            from o in _db.OrganizationComplianceObligations
                .IgnoreQueryFilters()
                .AsNoTracking()
            where o.OrganizationId == organizationId && o.FrameworkId == frameworkId
            join ch in _db.ComplianceChapters.AsNoTracking()
                on o.ChapterId equals ch.Id into chapters
            from chapter in chapters.DefaultIfEmpty()
            group new { o, chapter } by new { o.ChapterId, chapter.PlatformId, chapter.Name } into grp
            select new ObligationReadinessProjectionDto
            {
                ChapterId = grp.Key.ChapterId,
                ChapterCode = grp.Key.PlatformId ?? string.Empty,
                ChapterTitle = grp.Key.Name ?? string.Empty,
                TotalObligations = grp.Count(),
                CompletedObligations = grp.Count(x => x.o.JourneyStatus == ObligationJourneyStatus.Completed),
                ReadinessScore = grp.Any()
                    ? grp.Average(x => x.o.ReadinessScore)
                    : 0m
            }
        ).ToListAsync(ct);

        return result.OrderBy(r => r.ChapterCode).ToList();
    }

    private static ObligationMaturityBreakdownDto BuildMaturityBreakdown(
        IReadOnlyList<OrganizationComplianceObligation> obligations)
    {
        return new ObligationMaturityBreakdownDto
        {
            Core       = BuildBand(obligations, ObjectiveElementClassification.Core),
            Commitment = BuildBand(obligations, ObjectiveElementClassification.Commitment),
            Achievement = BuildBand(obligations, ObjectiveElementClassification.Achievement),
            Excellence  = BuildBand(obligations, ObjectiveElementClassification.Excellence)
        };
    }

    private static ObligationBandSummaryDto BuildBand(
        IReadOnlyList<OrganizationComplianceObligation> obligations,
        ObjectiveElementClassification band)
        => ObligationBandSummaryCalculator.BuildBand(obligations, band);

    private static OrganizationObligationDto MapToDto(OrganizationComplianceObligation o) =>
        new()
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
            UpdatedBy = o.UpdatedBy
        };
}
