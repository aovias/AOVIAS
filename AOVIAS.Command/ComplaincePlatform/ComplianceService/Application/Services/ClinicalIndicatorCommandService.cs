using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public sealed class ClinicalIndicatorCommandService : IClinicalIndicatorCommandService
{
    private readonly ComplianceDbContext _db;
    private readonly ClinicalIndicatorCatalogSeeder _seeder;
    private readonly IClinicalIndicatorQueryService _query;
    private readonly ClinicalIndicatorMetricsService _metrics;

    public ClinicalIndicatorCommandService(
        ComplianceDbContext db,
        ClinicalIndicatorCatalogSeeder seeder,
        IClinicalIndicatorQueryService query,
        ClinicalIndicatorMetricsService metrics)
    {
        _db = db;
        _seeder = seeder;
        _query = query;
        _metrics = metrics;
    }

    public async Task EnsureCatalogSeededAsync(Guid organizationId, Guid? hospitalId, CancellationToken ct = default)
        => await _seeder.EnsureSeededAsync(organizationId, hospitalId, ct);

    public async Task<ClinicalIndicatorDetailDto> PatchThresholdsAsync(
        Guid organizationId, Guid userId, PatchIndicatorThresholdsRequestDto request, CancellationToken ct = default)
    {
        var indicator = await _db.ClinicalIndicators
            .FirstOrDefaultAsync(i => i.TenantId == organizationId && i.Id == request.IndicatorId, ct)
            ?? throw new InvalidOperationException("Clinical indicator not found.");

        var now = DateTime.UtcNow;

        _db.IndicatorThresholdHistories.Add(new IndicatorThresholdHistory
        {
            Id = Guid.NewGuid(),
            IndicatorId = indicator.Id,
            TargetValue = request.TargetValue ?? indicator.TargetValue,
            GreenThreshold = request.GreenThreshold ?? indicator.GreenThreshold,
            YellowThreshold = request.YellowThreshold ?? indicator.YellowThreshold,
            RedThreshold = request.RedThreshold ?? indicator.RedThreshold,
            CriticalThreshold = request.CriticalThreshold ?? indicator.CriticalThreshold,
            LowerIsBetter = request.LowerIsBetter ?? indicator.LowerIsBetter,
            Reason = request.Reason,
            ChangedByUserId = userId,
            EffectiveFrom = now,
            CreatedAt = now,
        });

        if (request.TargetValue.HasValue) indicator.TargetValue = request.TargetValue;
        if (request.GreenThreshold.HasValue) indicator.GreenThreshold = request.GreenThreshold;
        if (request.YellowThreshold.HasValue) indicator.YellowThreshold = request.YellowThreshold;
        if (request.RedThreshold.HasValue) indicator.RedThreshold = request.RedThreshold;
        if (request.CriticalThreshold.HasValue) indicator.CriticalThreshold = request.CriticalThreshold;
        if (request.LowerIsBetter.HasValue) indicator.LowerIsBetter = request.LowerIsBetter.Value;

        indicator.ThresholdStatus = ClinicalIndicatorTrendEngine.EvaluateThreshold(indicator);
        indicator.LastUpdatedAt = now;
        indicator.LastUpdatedByUserId = userId;

        _db.IndicatorAuditTrails.Add(new IndicatorAuditTrail
        {
            Id = Guid.NewGuid(),
            IndicatorId = indicator.Id,
            Action = "ThresholdUpdated",
            Detail = request.Reason ?? "Threshold configuration updated",
            ActorUserId = userId,
            OccurredAt = now,
            CreatedAt = now,
        });

        await _db.SaveChangesAsync(ct);
        await _metrics.RefreshIndicatorComputedFieldsAsync(organizationId, ct);

        return (await _query.GetByIdAsync(organizationId, indicator.Id, ct))!;
    }
}
