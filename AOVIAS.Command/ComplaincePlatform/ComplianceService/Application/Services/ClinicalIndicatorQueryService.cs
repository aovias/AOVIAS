using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

public sealed class ClinicalIndicatorQueryService : IClinicalIndicatorQueryService
{
    private readonly ComplianceDbContext _db;
    private readonly ClinicalIndicatorMetricsService _metrics;
    private readonly ClinicalIndicatorCatalogSeeder _seeder;
    private readonly IAnalyticsAggregationService _analytics;

    public ClinicalIndicatorQueryService(
        ComplianceDbContext db,
        ClinicalIndicatorMetricsService metrics,
        ClinicalIndicatorCatalogSeeder seeder,
        IAnalyticsAggregationService analytics)
    {
        _db = db;
        _metrics = metrics;
        _seeder = seeder;
        _analytics = analytics;
    }

    public async Task<QualityDashboardDto> GetDashboardAsync(Guid organizationId, Guid? hospitalId, CancellationToken ct = default)
    {
        await _seeder.EnsureSeededAsync(organizationId, hospitalId, ct);
        await _metrics.RefreshIndicatorComputedFieldsAsync(organizationId, ct);

        var rows = await _db.ClinicalIndicators.AsNoTracking()
            .Where(i => i.TenantId == organizationId && i.Status == IndicatorStatus.Active)
            .ToListAsync(ct);

        var dto = new QualityDashboardDto
        {
            ActiveIndicators = rows.Count,
            IndicatorsWithinTarget = rows.Count(i => i.ThresholdStatus == ThresholdBand.Green),
            WarningIndicators = rows.Count(i => i.ThresholdStatus == ThresholdBand.Yellow),
            CriticalIndicators = rows.Count(i => i.ThresholdStatus is ThresholdBand.Red or ThresholdBand.Critical),
            ImprovingTrends = rows.Count(i => i.Trend == TrendDirection.Improving),
            DecliningTrends = rows.Count(i => i.Trend == TrendDirection.Declining),
            OverallQualityScore = await _metrics.ComputeOverallQualityScoreAsync(organizationId, ct),
            PatientSafetyScore = await _metrics.ComputePatientSafetyScoreAsync(organizationId, ct),
            AccreditationRiskScore = await _metrics.ComputeAccreditationRiskScoreAsync(organizationId, hospitalId, ct),
            CapaEffectivenessScore = await _metrics.ComputeCapaEffectivenessAsync(organizationId, hospitalId, ct),
        };

        dto.ByCategory = rows.GroupBy(r => r.Category.ToString())
            .Select(g => new QualityCountByLabelDto { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ToList();
        dto.ByDepartment = rows.GroupBy(r => r.Department.ToString())
            .Select(g => new QualityCountByLabelDto { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count).ToList();
        dto.ByThresholdStatus = rows.GroupBy(r => r.ThresholdStatus.ToString())
            .Select(g => new QualityCountByLabelDto { Label = g.Key, Count = g.Count() })
            .ToList();

        return dto;
    }

    public async Task<ClinicalIndicatorListDto> GetIndicatorsAsync(
        Guid organizationId, string? category, string? department, string? thresholdStatus,
        string? trend, string? search, string? sortBy, bool sortDesc,
        int page, int pageSize, CancellationToken ct = default)
    {
        await _seeder.EnsureSeededAsync(organizationId, null, ct);

        var q = _db.ClinicalIndicators.AsNoTracking()
            .Where(i => i.TenantId == organizationId && i.Status != IndicatorStatus.Archived);

        if (!string.IsNullOrWhiteSpace(category) && Enum.TryParse<IndicatorCategory>(category, true, out var cat))
            q = q.Where(i => i.Category == cat);
        if (!string.IsNullOrWhiteSpace(department) && Enum.TryParse<FindingDepartment>(department, true, out var dept))
            q = q.Where(i => i.Department == dept);
        if (!string.IsNullOrWhiteSpace(thresholdStatus) && Enum.TryParse<ThresholdBand>(thresholdStatus, true, out var band))
            q = q.Where(i => i.ThresholdStatus == band);
        if (!string.IsNullOrWhiteSpace(trend) && Enum.TryParse<TrendDirection>(trend, true, out var tr))
            q = q.Where(i => i.Trend == tr);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            q = q.Where(i => i.Name.ToLower().Contains(term) || i.IndicatorCode.ToLower().Contains(term));
        }

        var total = await q.CountAsync(ct);
        q = ApplySort(q, sortBy, sortDesc);

        var items = await q.Skip((page - 1) * pageSize).Take(pageSize)
            .Select(i => MapListItem(i))
            .ToListAsync(ct);

        return new ClinicalIndicatorListDto { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<ClinicalIndicatorDetailDto?> GetByIdAsync(Guid organizationId, Guid id, CancellationToken ct = default)
    {
        var entity = await _db.ClinicalIndicators.AsNoTracking()
            .Include(i => i.Measurements.OrderByDescending(m => m.PeriodEnd).Take(24))
            .Include(i => i.ThresholdHistory.OrderByDescending(h => h.EffectiveFrom).Take(20))
            .Include(i => i.AuditTrail.OrderByDescending(a => a.OccurredAt).Take(30))
            .FirstOrDefaultAsync(i => i.TenantId == organizationId && i.Id == id, ct);
        if (entity == null) return null;

        var dto = MapDetail(entity);
        dto.RelatedIncidents = await GetRelatedIncidentsAsync(organizationId, entity, ct);
        dto.RelatedFindings = await GetRelatedFindingsAsync(organizationId, entity, ct);
        dto.RelatedDeficiencies = await GetRelatedDeficienciesAsync(organizationId, entity, ct);
        dto.RelatedCapas = await GetRelatedCapasAsync(organizationId, entity, ct);
        dto.RelatedDocuments = ParseDocumentLinks(entity.RelatedDocumentationJson, entity.Id);
        dto.InvestigationRecommendations = BuildInvestigationRecommendations(entity);
        dto.ComplianceContext = await GetComplianceContextAsync(entity, ct);
        return dto;
    }

    public async Task<IReadOnlyList<DepartmentScorecardDto>> GetScorecardsAsync(Guid organizationId, CancellationToken ct = default)
    {
        await _seeder.EnsureSeededAsync(organizationId, null, ct);
        await _metrics.RefreshIndicatorComputedFieldsAsync(organizationId, ct);

        var scope = new AnalyticsScope(organizationId, Guid.Empty, Guid.Empty);
        var capa = await _analytics.GetCapaMetricsAsync(scope, ct);
        var sla = await _analytics.GetSlaMetricsAsync(scope, ct);

        var indicators = await _db.ClinicalIndicators.AsNoTracking()
            .Where(i => i.TenantId == organizationId && i.Status == IndicatorStatus.Active)
            .ToListAsync(ct);

        var incidentCounts = await _db.Incidents.AsNoTracking()
            .Where(i => i.TenantId == organizationId)
            .GroupBy(i => i.Department)
            .Select(g => new { Dept = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return indicators.GroupBy(i => i.Department).Select(g =>
        {
            var deptIndicators = g.ToList();
            var green = deptIndicators.Count(i => i.ThresholdStatus == ThresholdBand.Green);
            var score = deptIndicators.Count == 0 ? 0 : Math.Round(green * 100m / deptIndicators.Count, 1);
            var incCount = incidentCounts.FirstOrDefault(x => x.Dept == g.Key)?.Count ?? 0;
            return new DepartmentScorecardDto
            {
                Department = g.Key.ToString(),
                QualityScore = score,
                ClinicalIndicatorScore = score,
                IncidentRate = incCount,
                CapaCompletionPercent = Math.Round((decimal)capa.CapaEffectivenessRate * 100, 1),
                SlaHealthPercent = sla.TotalOpenWithSla > 0
                    ? Math.Round((sla.TotalOpenWithSla - sla.AtRiskCount - sla.BreachedCount) * 100m / sla.TotalOpenWithSla, 1)
                    : 100,
                EvidenceCompletenessPercent = 85,
                DocumentationHealthPercent = 88,
                IndicatorsWithinTarget = green,
                WarningIndicators = deptIndicators.Count(i => i.ThresholdStatus == ThresholdBand.Yellow),
                CriticalIndicators = deptIndicators.Count(i => i.ThresholdStatus is ThresholdBand.Red or ThresholdBand.Critical),
                ExecutiveSummary = BuildDeptSummary(g.Key.ToString(), score, incCount),
            };
        }).OrderByDescending(s => s.QualityScore).ToList();
    }

    public async Task<QualityTrendsDto> GetTrendsAsync(Guid organizationId, string? period, Guid? indicatorId, CancellationToken ct = default)
    {
        await _seeder.EnsureSeededAsync(organizationId, null, ct);
        var freq = ParsePeriod(period);

        var q = _db.ClinicalIndicators.AsNoTracking()
            .Include(i => i.Measurements)
            .Where(i => i.TenantId == organizationId && i.Status == IndicatorStatus.Active);
        if (indicatorId.HasValue) q = q.Where(i => i.Id == indicatorId.Value);

        var indicators = await q.ToListAsync(ct);
        var series = indicators.Select(i =>
        {
            var points = i.Measurements
                .Where(m => m.Frequency == freq)
                .OrderBy(m => m.PeriodEnd)
                .Take(24)
                .Select(MapMeasurement)
                .ToList();
            var values = points.Select(p => p.Value).ToList();
            return new QualityTrendSeriesDto
            {
                IndicatorId = i.Id,
                IndicatorCode = i.IndicatorCode,
                Name = i.Name,
                Trend = i.Trend.ToString(),
                Points = points,
                MovingAverage = ClinicalIndicatorTrendEngine.MovingAverage(values),
            };
        }).ToList();

        return new QualityTrendsDto { Period = freq.ToString(), Series = series };
    }

    public async Task<QualityBenchmarkingDto> GetBenchmarkingAsync(Guid organizationId, string? comparisonPeriod, CancellationToken ct = default)
    {
        await _seeder.EnsureSeededAsync(organizationId, null, ct);
        var period = string.IsNullOrWhiteSpace(comparisonPeriod) ? "PreviousMonth" : comparisonPeriod;

        var indicators = await _db.ClinicalIndicators.AsNoTracking()
            .Include(i => i.Measurements)
            .Where(i => i.TenantId == organizationId && i.Status == IndicatorStatus.Active)
            .ToListAsync(ct);

        var items = indicators.Select(i =>
        {
            var ordered = i.Measurements.OrderByDescending(m => m.PeriodEnd).ToList();
            var current = ordered.FirstOrDefault()?.Value;
            var previous = ordered.Skip(1).FirstOrDefault()?.Value;
            decimal? change = current.HasValue && previous.HasValue && previous != 0
                ? Math.Round((current.Value - previous.Value) / previous.Value * 100, 1)
                : null;
            return new QualityBenchmarkItemDto
            {
                IndicatorId = i.Id,
                IndicatorCode = i.IndicatorCode,
                Name = i.Name,
                CurrentValue = current,
                PreviousValue = previous,
                ChangePercent = change,
                TargetValue = i.TargetValue,
                Trend = i.Trend.ToString(),
            };
        }).ToList();

        var deptComparison = indicators.GroupBy(i => i.Department).Select(g => new QualityDepartmentComparisonDto
        {
            Department = g.Key.ToString(),
            QualityScore = g.Count(x => x.ThresholdStatus == ThresholdBand.Green) * 100m / Math.Max(1, g.Count()),
            AverageIndicatorValue = g.Where(x => x.CurrentValue.HasValue).Select(x => x.CurrentValue!.Value).DefaultIfEmpty(0).Average(),
            IndicatorCount = g.Count(),
        }).ToList();

        return new QualityBenchmarkingDto { ComparisonPeriod = period, Items = items, DepartmentComparison = deptComparison };
    }

    public async Task<QualityExecutiveDto> GetExecutiveAsync(Guid organizationId, CancellationToken ct = default)
    {
        var dashboard = await GetDashboardAsync(organizationId, null, ct);
        var indicators = await _db.ClinicalIndicators.AsNoTracking()
            .Where(i => i.TenantId == organizationId && i.Status == IndicatorStatus.Active)
            .ToListAsync(ct);

        var atRisk = indicators
            .Where(i => i.ThresholdStatus is ThresholdBand.Red or ThresholdBand.Critical || i.Trend == TrendDirection.Declining)
            .OrderByDescending(i => i.RiskLevel)
            .Take(8)
            .Select(i => new QualityExecutiveRiskDto
            {
                Title = i.Name,
                Department = i.Department.ToString(),
                Severity = i.ThresholdStatus.ToString(),
                DeepLink = $"/app/quality-intelligence/indicators?selected={i.Id}",
            }).ToList();

        var improving = indicators
            .Where(i => i.Trend == TrendDirection.Improving)
            .Take(5)
            .Select(i => new QualityExecutiveImprovementDto
            {
                Title = i.Name,
                Department = i.Department.ToString(),
                ChangePercent = 5,
                DeepLink = $"/app/quality-intelligence/indicators?selected={i.Id}",
            }).ToList();

        return new QualityExecutiveDto
        {
            OverallQualityScore = dashboard.OverallQualityScore,
            AccreditationRiskScore = dashboard.AccreditationRiskScore,
            TopRisks = atRisk,
            TopImprovements = improving,
            DepartmentsNeedingAttention = indicators
                .Where(i => i.ThresholdStatus is ThresholdBand.Red or ThresholdBand.Critical)
                .Select(i => i.Department.ToString()).Distinct().ToList(),
            IndicatorsThreateningAccreditation = indicators
                .Where(i => i.ThresholdStatus is ThresholdBand.Red or ThresholdBand.Critical)
                .Select(i => i.Name).ToList(),
            RecurringProblems = indicators
                .Where(i => i.Trend == TrendDirection.Declining)
                .Select(i => i.Name).Take(5).ToList(),
            ImprovingDepartments = indicators
                .Where(i => i.Trend == TrendDirection.Improving)
                .Select(i => i.Department.ToString()).Distinct().ToList(),
            MonthlyQualitySummary = $"Overall quality score {dashboard.OverallQualityScore}%. " +
                $"{dashboard.IndicatorsWithinTarget} indicators within target, {dashboard.CriticalIndicators} critical. " +
                $"Accreditation risk score {dashboard.AccreditationRiskScore}.",
        };
    }

    public async Task<QualityAnalyticsDto> GetAnalyticsAsync(Guid organizationId, Guid? hospitalId, CancellationToken ct = default)
    {
        var dashboard = await GetDashboardAsync(organizationId, hospitalId, ct);
        var scope = new AnalyticsScope(organizationId, hospitalId ?? Guid.Empty, Guid.Empty);
        var readiness = await _analytics.GetReadinessAsync(scope, ct);
        var trends = await GetTrendsAsync(organizationId, "Monthly", null, ct);

        return new QualityAnalyticsDto
        {
            OverallQualityScore = dashboard.OverallQualityScore,
            AccreditationRiskScore = dashboard.AccreditationRiskScore,
            JourneyReadiness = readiness.JourneyAccreditationReadiness,
            CapaEffectiveness = dashboard.CapaEffectivenessScore,
            IndicatorHealth = dashboard.ByThresholdStatus,
            DepartmentHeatmap = dashboard.ByDepartment,
            QualityTrends = trends.Series,
        };
    }

    private static IQueryable<ClinicalIndicator> ApplySort(IQueryable<ClinicalIndicator> q, string? sortBy, bool sortDesc)
    {
        return (sortBy?.ToLowerInvariant()) switch
        {
            "code" => sortDesc ? q.OrderByDescending(i => i.IndicatorCode) : q.OrderBy(i => i.IndicatorCode),
            "category" => sortDesc ? q.OrderByDescending(i => i.Category) : q.OrderBy(i => i.Category),
            "department" => sortDesc ? q.OrderByDescending(i => i.Department) : q.OrderBy(i => i.Department),
            "value" => sortDesc ? q.OrderByDescending(i => i.CurrentValue) : q.OrderBy(i => i.CurrentValue),
            "threshold" => sortDesc ? q.OrderByDescending(i => i.ThresholdStatus) : q.OrderBy(i => i.ThresholdStatus),
            "trend" => sortDesc ? q.OrderByDescending(i => i.Trend) : q.OrderBy(i => i.Trend),
            _ => sortDesc ? q.OrderByDescending(i => i.Name) : q.OrderBy(i => i.Name),
        };
    }

    private static ClinicalIndicatorListItemDto MapListItem(ClinicalIndicator i) => new()
    {
        Id = i.Id,
        IndicatorCode = i.IndicatorCode,
        Name = i.Name,
        Category = i.Category.ToString(),
        Department = i.Department.ToString(),
        MeasurementUnit = i.MeasurementUnit.ToString(),
        Frequency = i.Frequency.ToString(),
        CurrentValue = i.CurrentValue,
        TargetValue = i.TargetValue,
        ThresholdStatus = i.ThresholdStatus.ToString(),
        Trend = i.Trend.ToString(),
        RiskLevel = i.RiskLevel.ToString(),
        Status = i.Status.ToString(),
        OwnerName = i.OwnerName,
        LastCalculatedAt = i.LastCalculatedAt,
    };

    private static ClinicalIndicatorDetailDto MapDetail(ClinicalIndicator i)
    {
        var dto = new ClinicalIndicatorDetailDto
        {
            Description = i.Description,
            CalculationMethod = i.CalculationMethod.ToString(),
            NumeratorDefinition = i.NumeratorDefinition,
            DenominatorDefinition = i.DenominatorDefinition,
            FormulaExpression = i.FormulaExpression,
            CurrentNumerator = i.CurrentNumerator,
            CurrentDenominator = i.CurrentDenominator,
            GreenThreshold = i.GreenThreshold,
            YellowThreshold = i.YellowThreshold,
            RedThreshold = i.RedThreshold,
            CriticalThreshold = i.CriticalThreshold,
            LowerIsBetter = i.LowerIsBetter,
            FrameworkMapping = i.FrameworkMapping.ToString(),
            FrameworkReference = i.FrameworkReference,
            ObjectiveElementId = i.ObjectiveElementId,
            DataSource = i.DataSource,
            Comments = i.Comments,
            TrendHistory = i.Measurements.OrderByDescending(m => m.PeriodEnd).Select(MapMeasurement).ToList(),
            ThresholdHistory = i.ThresholdHistory.Select(h => new IndicatorThresholdHistoryDto
            {
                Id = h.Id, TargetValue = h.TargetValue, GreenThreshold = h.GreenThreshold,
                YellowThreshold = h.YellowThreshold, RedThreshold = h.RedThreshold,
                CriticalThreshold = h.CriticalThreshold, LowerIsBetter = h.LowerIsBetter,
                Reason = h.Reason, ChangedByName = h.ChangedByName, EffectiveFrom = h.EffectiveFrom,
            }).ToList(),
            LifecycleHistory = i.AuditTrail.Select(a => new IndicatorAuditTrailDto
            {
                Action = a.Action, Detail = a.Detail, ActorName = a.ActorName, OccurredAt = a.OccurredAt,
            }).ToList(),
        };
        var list = MapListItem(i);
        CopyListFields(dto, list);
        return dto;
    }

    private static void CopyListFields(ClinicalIndicatorDetailDto dto, ClinicalIndicatorListItemDto list)
    {
        dto.Id = list.Id; dto.IndicatorCode = list.IndicatorCode; dto.Name = list.Name;
        dto.Category = list.Category; dto.Department = list.Department;
        dto.MeasurementUnit = list.MeasurementUnit; dto.Frequency = list.Frequency;
        dto.CurrentValue = list.CurrentValue; dto.TargetValue = list.TargetValue;
        dto.ThresholdStatus = list.ThresholdStatus; dto.Trend = list.Trend;
        dto.RiskLevel = list.RiskLevel; dto.Status = list.Status;
        dto.OwnerName = list.OwnerName; dto.LastCalculatedAt = list.LastCalculatedAt;
    }

    private static IndicatorMeasurementDto MapMeasurement(IndicatorMeasurement m) => new()
    {
        Id = m.Id, PeriodStart = m.PeriodStart, PeriodEnd = m.PeriodEnd,
        Frequency = m.Frequency.ToString(), Numerator = m.Numerator,
        Denominator = m.Denominator, Value = m.Value, CalculatedAt = m.CalculatedAt,
    };

    private static MeasurementFrequency ParsePeriod(string? period) => period?.ToLowerInvariant() switch
    {
        "daily" => MeasurementFrequency.Daily,
        "weekly" => MeasurementFrequency.Weekly,
        "quarterly" => MeasurementFrequency.Quarterly,
        "yearly" => MeasurementFrequency.Yearly,
        _ => MeasurementFrequency.Monthly,
    };

    private static string BuildDeptSummary(string dept, decimal score, int incidents) =>
        $"{dept}: quality score {score}% with {incidents} related incidents. Review declining indicators and CAPA completion.";

    private async Task<IReadOnlyList<QualityLinkedEntityDto>> GetRelatedIncidentsAsync(
        Guid orgId, ClinicalIndicator indicator, CancellationToken ct)
    {
        var incidents = await _db.Incidents.AsNoTracking()
            .Where(i => i.TenantId == orgId && i.Department == indicator.Department)
            .OrderByDescending(i => i.IncidentDateTime)
            .Take(10)
            .Select(i => new QualityLinkedEntityDto
            {
                Id = i.Id,
                Label = i.IncidentNumber + " — " + i.Title,
                SubLabel = i.Severity.ToString(),
                DeepLink = $"/app/patient-safety/command-center?selected={i.Id}",
            })
            .ToListAsync(ct);
        return incidents;
    }

    private async Task<IReadOnlyList<QualityLinkedEntityDto>> GetRelatedFindingsAsync(
        Guid orgId, ClinicalIndicator indicator, CancellationToken ct)
    {
        return await _db.Findings.AsNoTracking()
            .Where(f => f.TenantId == orgId && f.Department == indicator.Department && f.Status != FindingStatus.Closed)
            .OrderByDescending(f => f.DateRaised)
            .Take(8)
            .Select(f => new QualityLinkedEntityDto
            {
                Id = f.Id,
                Label = (string.IsNullOrEmpty(f.FindingNumber) ? f.Id.ToString() : f.FindingNumber) + " — " + f.Title,
                SubLabel = f.Severity.ToString(),
                DeepLink = $"/app/audit/findings?selected={f.Id}",
            })
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<QualityLinkedEntityDto>> GetRelatedDeficienciesAsync(
        Guid orgId, ClinicalIndicator indicator, CancellationToken ct)
    {
        return await _db.ComplianceDeficiencies.AsNoTracking()
            .Where(d => d.TenantId == orgId && d.Department == indicator.Department.ToString())
            .OrderByDescending(d => d.CreatedAt)
            .Take(8)
            .Select(d => new QualityLinkedEntityDto
            {
                Id = d.Id,
                Label = d.DeficiencyCode + " — " + d.Title,
                SubLabel = d.Severity.ToString(),
                DeepLink = $"/app/audit/deficiency-command-center?selected={d.Id}",
            })
            .ToListAsync(ct);
    }

    private async Task<IReadOnlyList<QualityLinkedEntityDto>> GetRelatedCapasAsync(
        Guid orgId, ClinicalIndicator indicator, CancellationToken ct)
    {
        return await _db.Capas.AsNoTracking()
            .Where(c => c.TenantId == orgId)
            .OrderByDescending(c => c.CreatedAt)
            .Take(6)
            .Select(c => new QualityLinkedEntityDto
            {
                Id = c.Id,
                Label = c.Title,
                SubLabel = c.Status.ToString(),
                DeepLink = $"/app/audit/capa/{c.Id}",
            })
            .ToListAsync(ct);
    }

    private static IReadOnlyList<QualityLinkedEntityDto> ParseDocumentLinks(string? json, Guid indicatorId)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<QualityLinkedEntityDto>();
        try
        {
            var list = System.Text.Json.JsonSerializer.Deserialize<List<QualityLinkedEntityDto>>(json);
            return list ?? (IReadOnlyList<QualityLinkedEntityDto>)Array.Empty<QualityLinkedEntityDto>();
        }
        catch
        {
            return Array.Empty<QualityLinkedEntityDto>();
        }
    }

    private static IReadOnlyList<QualityInvestigationRecommendationDto> BuildInvestigationRecommendations(ClinicalIndicator i)
    {
        var list = new List<QualityInvestigationRecommendationDto>();
        if (i.ThresholdStatus is ThresholdBand.Red or ThresholdBand.Critical)
        {
            list.Add(new() { RecommendationType = "Investigate", Title = "Investigate threshold breach", Rationale = $"{i.Name} is in {i.ThresholdStatus} band.", DeepLink = $"/app/patient-safety/investigations", IsAdvisory = true });
            list.Add(new() { RecommendationType = "ReviewIncidents", Title = "Review related incidents", Rationale = "Correlate operational events with indicator deterioration.", DeepLink = "/app/patient-safety/command-center", IsAdvisory = true });
        }
        if (i.Trend == TrendDirection.Declining)
            list.Add(new() { RecommendationType = "ReviewSOP", Title = "Review related SOPs", Rationale = "Sustained decline may indicate process drift.", DeepLink = "/app/documentation/register", IsAdvisory = true });
        if (i.Trend == TrendDirection.Volatile)
            list.Add(new() { RecommendationType = "ReviewCAPA", Title = "Review CAPA effectiveness", Rationale = "Volatile performance suggests inconsistent remediation.", DeepLink = "/app/audit/capa", IsAdvisory = true });
        return list;
    }

    private async Task<QualityComplianceContextDto?> GetComplianceContextAsync(ClinicalIndicator i, CancellationToken ct)
    {
        if (!i.ObjectiveElementId.HasValue) return null;
        var oe = await _db.ObjectiveElements.AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == i.ObjectiveElementId, ct);
        if (oe == null) return null;
        return new QualityComplianceContextDto
        {
            ObjectiveElementId = oe.Id,
            ObjectiveElementName = oe.Title,
            ClauseReference = oe.Code,
            ReadinessImpact = i.ThresholdStatus is ThresholdBand.Red or ThresholdBand.Critical ? 15 : 5,
            VerificationStatus = "Linked",
        };
    }
}
