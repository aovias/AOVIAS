using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WorkforceIntelligenceService.Application.Contracts;
using WorkforceIntelligenceService.Application.Intelligence.Platform;
using WorkforceIntelligenceService.Domain.ReadModels;
using WorkforceIntelligenceService.Infrastructure.Clients;
using WorkforceIntelligenceService.Infrastructure.Persistence;

namespace WorkforceIntelligenceService.Application.Intelligence.Forecasting;

public sealed class ForecastEngine(
    WorkforceIntelligenceDbContext db,
    IProfessionalSourceClient professional,
    IIntelligenceOutboxWriter outbox,
    IOptions<IntelligenceOptions> options) : IForecastEngine
{
    private static readonly string[] SupportedTypes =
    [
        "Retirement", "CertificationExpiry", "LicenseExpiry", "Attrition", "Hiring", "Training", "Competency"
    ];

    public async Task<ForecastSnapshotResult> GenerateAsync(Guid organizationId, string forecastType, string correlationId, CancellationToken ct = default)
    {
        if (!SupportedTypes.Contains(forecastType))
            forecastType = "CertificationExpiry";

        var lastVersion = await db.ForecastSnapshots
            .Where(f => f.OrganizationId == organizationId && f.ForecastType == forecastType)
            .MaxAsync(f => (int?)f.Version, ct) ?? 0;

        var snapshotIds = await db.WorkforceSnapshots
            .Where(s => s.OrganizationId == organizationId)
            .OrderByDescending(s => s.AsOfUtc)
            .Take(3)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var horizon = options.Value.ForecastHorizonDays;
        var warningDays = options.Value.CredentialExpiryWarningDays;
        var points = new List<ForecastDataPointDto>();

        switch (forecastType)
        {
            case "CertificationExpiry":
            case "LicenseExpiry":
                var expiring = await professional.GetExpiringAsync(organizationId, horizon, ct);
                var filtered = expiring.Where(e => forecastType == "LicenseExpiry"
                    ? e.CredentialType.Contains("License", StringComparison.OrdinalIgnoreCase)
                    : e.CredentialType.Contains("Certification", StringComparison.OrdinalIgnoreCase));
                points = filtered.GroupBy(e => e.ExpiryDate?.ToString("yyyy-MM") ?? "Unknown")
                    .Select(g => new ForecastDataPointDto { Label = g.Key, Count = g.Count(), TargetDate = g.First().ExpiryDate })
                    .ToList();
                break;
            case "Attrition":
                var snapshots = await db.WorkforceSnapshots.Where(s => s.OrganizationId == organizationId).OrderBy(s => s.AsOfUtc).Take(12).ToListAsync(ct);
                var rate = snapshots.LastOrDefault()?.AttritionRate ?? 0.05m;
                points.Add(new ForecastDataPointDto { Label = "Projected departures", Count = (int)Math.Ceiling((snapshots.LastOrDefault()?.TotalHeadcount ?? 0) * (double)rate), Category = "12-month" });
                break;
            case "Hiring":
                var latest = await db.ExecutiveMetrics.Where(m => m.OrganizationId == organizationId).OrderByDescending(m => m.AsOfUtc).FirstOrDefaultAsync(ct);
                points.Add(new ForecastDataPointDto { Label = "Vacancy fill", Count = latest?.OpenVacancies ?? 0, Category = "Open" });
                break;
            default:
                points.Add(new ForecastDataPointDto { Label = forecastType, Count = 0 });
                break;
        }

        var entity = new ForecastSnapshot
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ForecastType = forecastType,
            Version = lastVersion + 1,
            GeneratedAtUtc = DateTime.UtcNow,
            InputSnapshotIdsJson = JsonSerializer.Serialize(snapshotIds),
            ParametersJson = JsonSerializer.Serialize(new { horizon, warningDays }),
            ResultsJson = JsonSerializer.Serialize(points),
            CorrelationId = correlationId
        };
        db.ForecastSnapshots.Add(entity);
        await db.SaveChangesAsync(ct);
        await outbox.EnqueueAsync("ForecastGenerated", entity.Id, new { organizationId, forecastType, version = entity.Version }, ct);
        return new ForecastSnapshotResult(entity.Id, entity.Version, entity.ResultsJson);
    }
}
