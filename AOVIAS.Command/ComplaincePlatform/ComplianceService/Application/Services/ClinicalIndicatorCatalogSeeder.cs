using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>Seeds the default NABH-aligned clinical indicator catalog per organization.</summary>
public sealed class ClinicalIndicatorCatalogSeeder
{
    private readonly ComplianceDbContext _db;

    public ClinicalIndicatorCatalogSeeder(ComplianceDbContext db) => _db = db;

    public async Task EnsureSeededAsync(Guid organizationId, Guid? hospitalId, CancellationToken ct = default)
    {
        if (await _db.ClinicalIndicators.AnyAsync(i => i.TenantId == organizationId, ct))
            return;

        var now = DateTime.UtcNow;
        var catalog = BuildDefaultCatalog(organizationId, hospitalId, now);
        _db.ClinicalIndicators.AddRange(catalog);
        await _db.SaveChangesAsync(ct);
    }

    private static IEnumerable<ClinicalIndicator> BuildDefaultCatalog(Guid tenantId, Guid? hospitalId, DateTime now)
    {
        var defs = new (string Code, string Name, IndicatorCategory Cat, FindingDepartment Dept, MeasurementUnit Unit, decimal Target, decimal Green, decimal Yellow, decimal Red, decimal Critical, bool LowerIsBetter, string FrameworkRef)[]
        {
            ("QI-MED-ERR", "Medication Error Rate", IndicatorCategory.MedicationErrors, FindingDepartment.Clinical, MeasurementUnit.Rate, 2m, 2m, 4m, 6m, 10m, true, "NABH-PSQ"),
            ("QI-FALL", "Patient Fall Rate", IndicatorCategory.PatientFalls, FindingDepartment.Clinical, MeasurementUnit.Rate, 3m, 3m, 5m, 8m, 12m, true, "NABH-PSQ"),
            ("QI-HAI", "Hospital Acquired Infection Rate", IndicatorCategory.HospitalAcquiredInfection, FindingDepartment.Clinical, MeasurementUnit.Percent, 2m, 2m, 3.5m, 5m, 8m, true, "NABH-PSQ"),
            ("QI-SSI", "Surgical Site Infection Rate", IndicatorCategory.SurgicalSiteInfection, FindingDepartment.Clinical, MeasurementUnit.Percent, 1.5m, 1.5m, 2.5m, 4m, 6m, true, "NABH-PSQ"),
            ("QI-PI", "Pressure Injury Rate", IndicatorCategory.PressureInjury, FindingDepartment.Clinical, MeasurementUnit.Rate, 1m, 1m, 2m, 3m, 5m, true, "NABH-PSQ"),
            ("QI-READMIT", "30-Day Readmission Rate", IndicatorCategory.Readmissions, FindingDepartment.Clinical, MeasurementUnit.Percent, 8m, 8m, 10m, 12m, 15m, true, "NABH-PSQ"),
            ("QI-MORT", "Risk-Adjusted Mortality Index", IndicatorCategory.Mortality, FindingDepartment.Clinical, MeasurementUnit.Ratio, 1m, 1m, 1.1m, 1.2m, 1.3m, true, "NABH-PSQ"),
            ("QI-NSI", "Needle Stick Injury Rate", IndicatorCategory.NeedleStickInjuries, FindingDepartment.Clinical, MeasurementUnit.Rate, 1m, 1m, 2m, 3m, 5m, true, "NABH-PSQ"),
            ("QI-BMW", "Biomedical Waste Compliance", IndicatorCategory.BiomedicalWaste, FindingDepartment.Admin, MeasurementUnit.Percent, 98m, 98m, 95m, 90m, 85m, false, "NABH-FMS"),
            ("QI-EQUIP", "Critical Equipment Uptime", IndicatorCategory.EquipmentDowntime, FindingDepartment.Clinical, MeasurementUnit.Percent, 97m, 97m, 94m, 90m, 85m, false, "NABH-FMS"),
            ("QI-EMR", "Emergency Response Time Compliance", IndicatorCategory.EmergencyResponse, FindingDepartment.Clinical, MeasurementUnit.Percent, 95m, 95m, 90m, 85m, 80m, false, "NABH-PSQ"),
            ("QI-PSAT", "Patient Satisfaction Score", IndicatorCategory.PatientSatisfaction, FindingDepartment.Admin, MeasurementUnit.Score, 85m, 85m, 80m, 75m, 70m, false, "NABH-PSQ"),
            ("QI-COMP", "Complaint Resolution Within SLA", IndicatorCategory.ComplaintResolution, FindingDepartment.Admin, MeasurementUnit.Percent, 92m, 92m, 85m, 75m, 65m, false, "NABH-PSQ"),
            ("QI-LAB", "Laboratory Error Rate", IndicatorCategory.LaboratoryErrors, FindingDepartment.Clinical, MeasurementUnit.Rate, 0.5m, 0.5m, 1m, 2m, 3m, true, "NABH-PSQ"),
            ("QI-RAD", "Radiology Report Turnaround Compliance", IndicatorCategory.RadiologyErrors, FindingDepartment.Clinical, MeasurementUnit.Percent, 95m, 95m, 90m, 85m, 80m, false, "NABH-PSQ"),
            ("QI-HH", "Hand Hygiene Compliance", IndicatorCategory.HandHygieneCompliance, FindingDepartment.Clinical, MeasurementUnit.Percent, 90m, 90m, 85m, 80m, 75m, false, "NABH-PSQ"),
            ("QI-BTR", "Blood Transfusion Reaction Rate", IndicatorCategory.BloodTransfusionReactions, FindingDepartment.Clinical, MeasurementUnit.Rate, 0.2m, 0.2m, 0.5m, 1m, 2m, true, "NABH-PSQ"),
            ("QI-MED-SAF", "Medication Safety Composite Score", IndicatorCategory.MedicationSafety, FindingDepartment.Clinical, MeasurementUnit.Score, 88m, 88m, 82m, 75m, 68m, false, "NABH-PSQ"),
        };

        foreach (var d in defs)
        {
            var id = Guid.NewGuid();
            var measurements = BuildSampleMeasurements(id, d.Target, d.LowerIsBetter, now);
            var current = measurements.OrderByDescending(m => m.PeriodEnd).First();

            yield return new ClinicalIndicator
            {
                Id = id,
                TenantId = tenantId,
                HospitalId = hospitalId,
                IndicatorCode = d.Code,
                Name = d.Name,
                Description = $"{d.Name} — continuous quality measurement aligned to {d.FrameworkRef}.",
                Category = d.Cat,
                Department = d.Dept,
                MeasurementUnit = d.Unit,
                CalculationMethod = CalculationMethod.Formula,
                NumeratorDefinition = "Events in period",
                DenominatorDefinition = "Eligible population in period",
                FormulaExpression = "(Numerator / Denominator) * scaling factor",
                TargetValue = d.Target,
                GreenThreshold = d.Green,
                YellowThreshold = d.Yellow,
                RedThreshold = d.Red,
                CriticalThreshold = d.Critical,
                LowerIsBetter = d.LowerIsBetter,
                CurrentValue = current.Value,
                CurrentNumerator = current.Numerator,
                CurrentDenominator = current.Denominator,
                Trend = TrendDirection.Stable,
                Frequency = MeasurementFrequency.Monthly,
                FrameworkMapping = QualityFrameworkMapping.NABH,
                FrameworkReference = d.FrameworkRef,
                Status = IndicatorStatus.Active,
                RiskLevel = IndicatorRiskLevel.Medium,
                ThresholdStatus = ClinicalIndicatorTrendEngine.EvaluateThreshold(new ClinicalIndicator
                {
                    CurrentValue = current.Value, TargetValue = d.Target,
                    GreenThreshold = d.Green, YellowThreshold = d.Yellow,
                    RedThreshold = d.Red, CriticalThreshold = d.Critical, LowerIsBetter = d.LowerIsBetter,
                }),
                DataSource = "PRAMAN Quality Intelligence",
                LastCalculatedAt = now,
                CreatedAt = now,
                LastUpdatedAt = now,
                Measurements = measurements,
            };
        }
    }

    private static List<IndicatorMeasurement> BuildSampleMeasurements(Guid indicatorId, decimal target, bool lowerIsBetter, DateTime now)
    {
        var list = new List<IndicatorMeasurement>();
        var random = new Random(indicatorId.GetHashCode());
        for (var i = 5; i >= 0; i--)
        {
            var periodEnd = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-i);
            var periodStart = periodEnd.AddMonths(-1);
            var variance = (decimal)(random.NextDouble() * 0.15 - 0.075);
            var value = lowerIsBetter
                ? Math.Max(0, target * (1 + variance))
                : Math.Min(100, target * (1 - variance / 2));

            list.Add(new IndicatorMeasurement
            {
                Id = Guid.NewGuid(),
                IndicatorId = indicatorId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                Frequency = MeasurementFrequency.Monthly,
                Numerator = Math.Round(value * 10, 2),
                Denominator = 1000,
                Value = Math.Round(value, 2),
                CalculatedAt = periodEnd,
                Source = "Seed",
                CreatedAt = periodEnd,
            });
        }
        return list;
    }
}
