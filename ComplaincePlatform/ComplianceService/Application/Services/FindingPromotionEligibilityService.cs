using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// Sprint 7B Phase 1 / Phase 2 — classification and structural eligibility for finding promotion.
/// Multiple CAPAs per finding are allowed for Accepted findings.
/// </summary>
public sealed class FindingPromotionEligibilityService : IFindingPromotionEligibilityService
{
    private readonly ComplianceDbContext _db;

    public FindingPromotionEligibilityService(ComplianceDbContext db)
    {
        _db = db;
    }

    public async Task<FindingPromotionEligibilityDto> EvaluateAsync(
        Finding finding,
        CancellationToken ct = default)
    {
        var reasons = new List<string>();
        var classification = finding.Classification
            ?? FindingsWorkspaceQueryService.MapLegacyType(finding.Type);

        var classificationOk = IsClassificationEligible(classification, reasons);

        // Phase 2: already-promoted findings may receive additional CAPAs when Accepted.

        if (finding.Status != FindingStatus.Accepted)
        {
            reasons.Add($"Finding status must be Accepted (current: {finding.Status}).");
        }

        if (!finding.ObjectiveElementId.HasValue || finding.ObjectiveElementId == Guid.Empty)
        {
            reasons.Add("Objective Element must be mapped before promotion.");
        }

        var context = finding.ObjectiveElementId.HasValue
            ? await ResolveComplianceContextAsync(finding.ObjectiveElementId.Value, ct)
            : null;

        var chapterCode = !string.IsNullOrWhiteSpace(context?.ChapterCode)
            ? context!.ChapterCode
            : finding.ClauseReference;

        if (string.IsNullOrWhiteSpace(chapterCode))
        {
            reasons.Add("Chapter must be mapped before promotion.");
        }

        if (string.IsNullOrWhiteSpace(context?.StandardCode))
        {
            reasons.Add("Standard must be mapped before promotion.");
        }

        if (finding.Department == default)
        {
            reasons.Add("Department must be assigned before promotion.");
        }

        if (string.IsNullOrWhiteSpace(finding.Title))
        {
            reasons.Add("Finding title is required.");
        }

        var isEligible = classificationOk
            && finding.Status == FindingStatus.Accepted
            && finding.ObjectiveElementId.HasValue
            && finding.ObjectiveElementId != Guid.Empty
            && !string.IsNullOrWhiteSpace(chapterCode)
            && !string.IsNullOrWhiteSpace(context?.StandardCode)
            && finding.Department != default
            && !string.IsNullOrWhiteSpace(finding.Title);

        return new FindingPromotionEligibilityDto
        {
            FindingId = finding.Id,
            EligibilityStatus = isEligible ? "Eligible" : "NotEligible",
            IsEligible = isEligible,
            ValidationReasons = reasons,
        };
    }

    internal static bool IsClassificationEligible(FindingClassification classification, IList<string>? reasons = null)
    {
        switch (classification)
        {
            case FindingClassification.AuditFinding:
            case FindingClassification.Observation:
            case FindingClassification.NonConformity:
            case FindingClassification.OpportunityForImprovement:
                return true;
            case FindingClassification.GoodPractice:
                reasons?.Add("Good Practice findings cannot be promoted to deficiencies.");
                return false;
            default:
                reasons?.Add($"Classification {classification} is not eligible for promotion.");
                return false;
        }
    }

    internal static DeficiencyType MapDeficiencyType(FindingClassification classification) =>
        classification switch
        {
            FindingClassification.OpportunityForImprovement => DeficiencyType.Maturity,
            _ => DeficiencyType.Assessment,
        };

    internal static DeficiencySeverity MapSeverity(FindingSeverity severity) =>
        severity switch
        {
            FindingSeverity.Critical => DeficiencySeverity.Critical,
            FindingSeverity.Major => DeficiencySeverity.Major,
            FindingSeverity.Moderate => DeficiencySeverity.Moderate,
            FindingSeverity.Informational => DeficiencySeverity.Minor,
            _ => DeficiencySeverity.Minor,
        };

    private async Task<ComplianceContextRow?> ResolveComplianceContextAsync(Guid objectiveElementId, CancellationToken ct)
    {
        return await (
                from oe in _db.ObjectiveElements.AsNoTracking()
                where oe.Id == objectiveElementId
                join std in _db.ComplianceStandards.AsNoTracking()
                    on oe.ComplianceStandardId equals std.Id into stdJoin
                from std in stdJoin.DefaultIfEmpty()
                join ch in _db.ComplianceChapters.AsNoTracking()
                    on std.ChapterId equals ch.Id into chJoin
                from ch in chJoin.DefaultIfEmpty()
                select new ComplianceContextRow
                {
                    ChapterCode = ch != null ? ch.Code : string.Empty,
                    StandardCode = std != null ? std.Code : string.Empty,
                    ChapterId = ch != null ? ch.Id : null,
                    StandardId = std != null ? std.Id : null,
                })
            .FirstOrDefaultAsync(ct);
    }

    private sealed class ComplianceContextRow
    {
        public string ChapterCode { get; init; } = string.Empty;
        public string StandardCode { get; init; } = string.Empty;
        public Guid? ChapterId { get; init; }
        public Guid? StandardId { get; init; }
    }
}
