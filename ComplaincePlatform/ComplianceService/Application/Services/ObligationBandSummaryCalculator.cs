using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// Builds mutually exclusive obligation status buckets per maturity band.
/// Verified + Implemented + Partial + Deficient + NotStarted + Unknown = Total.
/// </summary>
public static class ObligationBandSummaryCalculator
{
    private enum PrimaryBucket
    {
        Verified,
        Deficient,
        Implemented,
        Partial,
        NotStarted,
        Unknown
    }

    public static ObligationBandSummaryDto BuildBand(
        IReadOnlyList<OrganizationComplianceObligation> obligations,
        ObjectiveElementClassification band)
    {
        var bandObs = obligations.Where(o => o.MaturityLevel == band).ToList();
        if (bandObs.Count == 0)
            return new ObligationBandSummaryDto();

        var buckets = new Dictionary<PrimaryBucket, int>();
        foreach (PrimaryBucket b in Enum.GetValues<PrimaryBucket>())
            buckets[b] = 0;

        foreach (var o in bandObs)
            buckets[ClassifyPrimary(o)]++;

        return new ObligationBandSummaryDto
        {
            Total = bandObs.Count,
            Verified = buckets[PrimaryBucket.Verified],
            Implemented = buckets[PrimaryBucket.Implemented],
            Partial = buckets[PrimaryBucket.Partial],
            Deficient = buckets[PrimaryBucket.Deficient],
            NotStarted = buckets[PrimaryBucket.NotStarted],
            Unknown = buckets[PrimaryBucket.Unknown],
            ReadinessScore = Math.Round(bandObs.Average(x => x.ReadinessScore), 2),
        };
    }

    /// <summary>
    /// Maps stage-level obligation counts using the same primary bucket rules as band summaries.
    /// </summary>
    public static (int Verified, int Implemented, int Partial, int Deficient) MapStageCounts(
        IReadOnlyList<OrganizationComplianceObligation> stageObligations)
    {
        var verified = 0;
        var implemented = 0;
        var partial = 0;
        var deficient = 0;

        foreach (var o in stageObligations)
        {
            switch (ClassifyPrimary(o))
            {
                case PrimaryBucket.Verified: verified++; break;
                case PrimaryBucket.Implemented: implemented++; break;
                case PrimaryBucket.Partial: partial++; break;
                case PrimaryBucket.Deficient: deficient++; break;
            }
        }

        return (verified, implemented, partial, deficient);
    }

    private static PrimaryBucket ClassifyPrimary(OrganizationComplianceObligation o)
    {
        if (o.VerificationStatus == VerificationStatus.Verified)
            return PrimaryBucket.Verified;

        if (o.ImplementationStatus == ImplementationStatus.NotImplemented
            && o.AssessmentStatus == ObligationAssessmentStatus.Failed)
            return PrimaryBucket.Deficient;

        if (o.ImplementationStatus == ImplementationStatus.Implemented)
            return PrimaryBucket.Implemented;

        if (o.ImplementationStatus == ImplementationStatus.PartiallyImplemented
            || o.EvidenceStatus == EvidenceStatus.Partial)
            return PrimaryBucket.Partial;

        if (o.ImplementationStatus == ImplementationStatus.NotStarted)
            return PrimaryBucket.NotStarted;

        return PrimaryBucket.Unknown;
    }
}
