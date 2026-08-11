using ComplianceService.Application.Analytics;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>Sprint 17 — SSOT for quality intelligence KPI formulas. Delegates CAPA/SLA/readiness to Analytics Hub.</summary>
public sealed class ClinicalIndicatorMetricsService
{
    private readonly ComplianceDbContext _db;
    private readonly IAnalyticsAggregationService _analytics;

    public ClinicalIndicatorMetricsService(ComplianceDbContext db, IAnalyticsAggregationService analytics)
    {
        _db = db;
        _analytics = analytics;
    }

    public async Task<decimal> ComputeOverallQualityScoreAsync(Guid organizationId, CancellationToken ct)
    {
        var indicators = await _db.ClinicalIndicators.AsNoTracking()
            .Where(i => i.TenantId == organizationId && i.Status == IndicatorStatus.Active)
            .ToListAsync(ct);
        if (indicators.Count == 0) return 0;

        var green = indicators.Count(i => i.ThresholdStatus == ThresholdBand.Green);
        var yellow = indicators.Count(i => i.ThresholdStatus == ThresholdBand.Yellow);
        var score = (green * 100m + yellow * 60m) / indicators.Count;
        return Math.Round(score, 1);
    }

    public async Task<decimal> ComputeAccreditationRiskScoreAsync(Guid organizationId, Guid? hospitalId, CancellationToken ct)
    {
        var scope = new AnalyticsScope(organizationId, hospitalId ?? Guid.Empty, Guid.Empty);
        var readiness = await _analytics.GetReadinessAsync(scope, ct);
        var indicators = await _db.ClinicalIndicators.AsNoTracking()
            .Where(i => i.TenantId == organizationId && i.Status == IndicatorStatus.Active)
            .ToListAsync(ct);

        var criticalCount = indicators.Count(i => i.ThresholdStatus is ThresholdBand.Red or ThresholdBand.Critical);
        var declining = indicators.Count(i => i.Trend == TrendDirection.Declining);
        var readinessGap = Math.Max(0, 100 - readiness.JourneyAccreditationReadiness);
        var risk = (criticalCount * 12m) + (declining * 5m) + (readinessGap * 0.4m);
        return Math.Round(Math.Min(100, risk), 1);
    }

    public async Task<decimal> ComputeCapaEffectivenessAsync(Guid organizationId, Guid? hospitalId, CancellationToken ct)
    {
        var scope = new AnalyticsScope(organizationId, hospitalId ?? Guid.Empty, Guid.Empty);
        var capa = await _analytics.GetCapaMetricsAsync(scope, ct);
        return Math.Round((decimal)capa.CapaEffectivenessRate * 100m, 1);
    }

    public async Task<decimal> ComputePatientSafetyScoreAsync(Guid organizationId, CancellationToken ct)
    {
        var safetyCategories = new[]
        {
            IndicatorCategory.MedicationSafety, IndicatorCategory.PatientFalls,
            IndicatorCategory.MedicationErrors, IndicatorCategory.NeedleStickInjuries,
        };
        var indicators = await _db.ClinicalIndicators.AsNoTracking()
            .Where(i => i.TenantId == organizationId && i.Status == IndicatorStatus.Active
                && safetyCategories.Contains(i.Category))
            .ToListAsync(ct);
        if (indicators.Count == 0) return 100;

        var green = indicators.Count(i => i.ThresholdStatus == ThresholdBand.Green);
        return Math.Round(green * 100m / indicators.Count, 1);
    }

    public async Task RefreshIndicatorComputedFieldsAsync(Guid organizationId, CancellationToken ct)
    {
        var indicators = await _db.ClinicalIndicators
            .Include(i => i.Measurements)
            .Where(i => i.TenantId == organizationId && i.Status == IndicatorStatus.Active)
            .ToListAsync(ct);

        foreach (var indicator in indicators)
        {
            var recent = indicator.Measurements.OrderByDescending(m => m.PeriodEnd).Take(12).ToList();
            if (recent.Count > 0)
            {
                var latest = recent.OrderByDescending(m => m.PeriodEnd).First();
                indicator.CurrentValue = latest.Value;
                indicator.CurrentNumerator = latest.Numerator;
                indicator.CurrentDenominator = latest.Denominator;
                indicator.LastCalculatedAt = latest.CalculatedAt;
            }

            indicator.Trend = ClinicalIndicatorTrendEngine.ComputeTrendForIndicator(
                indicator, recent.OrderBy(m => m.PeriodEnd), indicator.LowerIsBetter);
            indicator.ThresholdStatus = ClinicalIndicatorTrendEngine.EvaluateThreshold(indicator);
            indicator.RiskLevel = indicator.ThresholdStatus switch
            {
                ThresholdBand.Critical => IndicatorRiskLevel.Critical,
                ThresholdBand.Red => IndicatorRiskLevel.High,
                ThresholdBand.Yellow => IndicatorRiskLevel.Medium,
                ThresholdBand.Green => IndicatorRiskLevel.Low,
                _ => IndicatorRiskLevel.Medium,
            };
        }

        await _db.SaveChangesAsync(ct);
    }
}
