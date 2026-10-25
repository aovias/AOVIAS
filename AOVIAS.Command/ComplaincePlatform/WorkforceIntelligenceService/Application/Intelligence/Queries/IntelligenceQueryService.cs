using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WorkforceIntelligenceService.Application.Contracts;
using WorkforceIntelligenceService.Application.Intelligence.Platform;
using WorkforceIntelligenceService.Infrastructure.Persistence;

namespace WorkforceIntelligenceService.Application.Intelligence.Queries;

public sealed class IntelligenceQueryService(
    WorkforceIntelligenceDbContext db,
    IAnalyticsCacheService cache,
    IOptions<IntelligenceOptions> options) : IIntelligenceQueryService
{
    public async Task<ExecutiveDashboardDto> GetDashboardAsync(IntelligenceScope scope, CancellationToken ct = default)
    {
        var cacheKey = $"dashboard:{scope.OrganizationId}:{scope.HospitalId}:{scope.AsOf:O}";
        var cached = await cache.GetAsync<ExecutiveDashboardDto>(cacheKey, ct);
        if (cached != null) return cached;

        var metrics = await GetLatestExecutiveMetricsAsync(scope, ct);
        var snapshot = metrics?.SnapshotId != null
            ? await db.WorkforceSnapshots.FindAsync([metrics.SnapshotId], ct)
            : await GetLatestSnapshotAsync(scope, ct);

        var dto = new ExecutiveDashboardDto
        {
            OrganizationId = scope.OrganizationId,
            AsOfUtc = metrics?.AsOfUtc ?? snapshot?.AsOfUtc ?? DateTime.UtcNow,
            Headcount = metrics?.Headcount ?? snapshot?.TotalHeadcount ?? 0,
            OpenVacancies = metrics?.OpenVacancies ?? 0,
            TrainingCompliancePercent = metrics?.TrainingCompliancePercent ?? 0,
            CredentialRiskCount = metrics?.CredentialRiskCount ?? 0,
            CriticalSkillGaps = metrics?.CriticalSkillGaps ?? 0,
            CommitteeActionBacklog = metrics?.CommitteeActionBacklog ?? 0,
            AvailabilityCoveragePercent = metrics?.AvailabilityCoveragePercent ?? 0,
            GrowthRate = snapshot?.GrowthRate,
            AttritionRate = snapshot?.AttritionRate,
            RetentionRate = snapshot?.RetentionRate,
            ByDepartment = DeserializeDict(snapshot?.ByDepartmentJson),
            ByProfession = DeserializeDict(snapshot?.ByProfessionJson),
            ByEmploymentType = DeserializeDict(snapshot?.ByEmploymentTypeJson)
        };

        await cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(options.Value.CacheTtlMinutes), ct);
        return dto;
    }

    public async Task<HeadcountAnalyticsDto> GetHeadcountAsync(IntelligenceScope scope, CancellationToken ct = default)
    {
        var snapshot = await GetLatestSnapshotAsync(scope, ct);
        var trend = await db.WorkforceSnapshots
            .Where(s => s.OrganizationId == scope.OrganizationId)
            .OrderByDescending(s => s.AsOfUtc)
            .Take(12)
            .Select(s => new HeadcountTrendPointDto { AsOfUtc = s.AsOfUtc, Headcount = s.TotalHeadcount })
            .ToListAsync(ct);

        return new HeadcountAnalyticsDto
        {
            OrganizationId = scope.OrganizationId,
            AsOfUtc = snapshot?.AsOfUtc ?? DateTime.UtcNow,
            TotalHeadcount = snapshot?.TotalHeadcount ?? 0,
            TotalFte = snapshot?.TotalFte ?? 0,
            ByHospital = DeserializeDict(snapshot?.ByHospitalJson),
            ByDepartment = DeserializeDict(snapshot?.ByDepartmentJson),
            ByProfession = DeserializeDict(snapshot?.ByProfessionJson),
            ByEmploymentType = DeserializeDict(snapshot?.ByEmploymentTypeJson),
            AgeDistribution = DeserializeDict(snapshot?.AgeDistributionJson),
            ExperienceDistribution = DeserializeDict(snapshot?.ExperienceDistributionJson),
            QualificationDistribution = DeserializeDict(snapshot?.QualificationDistributionJson),
            GrowthRate = snapshot?.GrowthRate,
            AttritionRate = snapshot?.AttritionRate,
            RetentionRate = snapshot?.RetentionRate,
            Trend = trend.OrderBy(t => t.AsOfUtc).ToList()
        };
    }

    public async Task<SkillMatrixDto> GetSkillsAsync(IntelligenceScope scope, CancellationToken ct = default)
    {
        var asOf = await GetMatrixAsOfAsync(scope, ct);
        var rows = await db.SkillMatrixEntries
            .Where(s => s.OrganizationId == scope.OrganizationId && s.AsOfUtc == asOf)
            .Where(s => !scope.DepartmentId.HasValue || s.DepartmentId == scope.DepartmentId)
            .ToListAsync(ct);

        var criticalGaps = rows.Where(r => r.Gap != null)
            .GroupBy(r => r.SkillName)
            .Select(g => new SkillGapSummaryDto { SkillName = g.Key, GapCount = g.Count(), DepartmentName = g.First().DepartmentId?.ToString() ?? "All" })
            .ToList();

        var covered = rows.Count(r => r.Gap == null);
        var coverage = rows.Count == 0 ? 100m : covered * 100m / rows.Count;

        return new SkillMatrixDto
        {
            OrganizationId = scope.OrganizationId,
            AsOfUtc = asOf,
            Rows = rows.Select(r => new SkillMatrixRowDto
            {
                SkillName = r.SkillName,
                CurrentLevel = r.CurrentLevel,
                TargetLevel = r.TargetLevel,
                Gap = r.Gap,
                DepartmentId = r.DepartmentId,
                PersonName = r.PersonId.ToString(),
                IsCritical = r.IsCritical
            }).ToList(),
            CriticalGaps = criticalGaps,
            CoveragePercent = coverage
        };
    }

    public async Task<CompetencyMatrixDto> GetCompetenciesAsync(IntelligenceScope scope, CancellationToken ct = default)
    {
        var asOf = await GetMatrixAsOfAsync(scope, ct);
        var rows = await db.CompetencyMatrixEntries
            .Where(c => c.OrganizationId == scope.OrganizationId && c.AsOfUtc == asOf)
            .ToListAsync(ct);

        return new CompetencyMatrixDto
        {
            OrganizationId = scope.OrganizationId,
            AsOfUtc = asOf,
            Rows = rows.Select(r => new CompetencyMatrixRowDto
            {
                CompetencyName = r.CompetencyName,
                CurrentLevel = r.CurrentLevel,
                TargetLevel = r.TargetLevel,
                Gap = r.Gap,
                ProfessionalCategory = r.ProfessionalCategory,
                DepartmentId = r.DepartmentId,
                PersonName = r.PersonId.ToString()
            }).ToList(),
            Trends = rows.GroupBy(r => r.CompetencyName).Select(g => new CompetencyTrendDto
            {
                CompetencyName = g.Key,
                Improving = g.Count(x => x.Gap == null),
                Declining = g.Count(x => x.Gap != null),
                Stable = 0
            }).ToList()
        };
    }

    public async Task<TrainingMatrixDto> GetTrainingAsync(IntelligenceScope scope, CancellationToken ct = default)
    {
        var asOf = await GetMatrixAsOfAsync(scope, ct);
        var rows = await db.TrainingMatrixEntries
            .Where(t => t.OrganizationId == scope.OrganizationId && t.AsOfUtc == asOf)
            .ToListAsync(ct);

        var mandatory = rows.Where(r => r.IsMandatory).ToList();
        var orgCompliance = mandatory.Count == 0 ? 100m : mandatory.Count(r => r.Status == "Completed") * 100m / mandatory.Count;

        return new TrainingMatrixDto
        {
            OrganizationId = scope.OrganizationId,
            AsOfUtc = asOf,
            OrganizationCompliancePercent = orgCompliance,
            ByScope = new[]
            {
                new TrainingScopeComplianceDto { ScopeType = "Organization", ScopeName = scope.OrganizationId.ToString(), CompliancePercent = orgCompliance }
            },
            Rows = rows.Select(r => new TrainingMatrixRowDto
            {
                CourseName = r.CourseName,
                Status = r.Status,
                IsMandatory = r.IsMandatory,
                PersonName = r.PersonId.ToString(),
                DueDate = r.DueDate
            }).ToList()
        };
    }

    public async Task<LicenseDashboardDto> GetLicensesAsync(IntelligenceScope scope, CancellationToken ct = default)
    {
        var asOf = await GetMatrixAsOfAsync(scope, ct);
        var items = await db.LicenseMatrixEntries.Where(l => l.OrganizationId == scope.OrganizationId && l.AsOfUtc == asOf).ToListAsync(ct);
        return BuildCredentialDashboard<LicenseDashboardDto>(scope.OrganizationId, asOf, items.Select(i => (i.LicenseName, i.Status, i.PersonId.ToString(), i.ExpiryDate, i.DaysUntilExpiry, i.DepartmentId?.ToString())).ToList(),
            dto => new LicenseDashboardDto
            {
                OrganizationId = dto.OrgId,
                AsOfUtc = dto.AsOf,
                Active = dto.Active,
                Expiring = dto.Expiring,
                Expired = dto.Expired,
                RenewalRequired = dto.RenewalRequired,
                ByDepartment = dto.ByGroup,
                Items = dto.Items
            });
    }

    public async Task<CertificationDashboardDto> GetCertificationsAsync(IntelligenceScope scope, CancellationToken ct = default)
    {
        var asOf = await GetMatrixAsOfAsync(scope, ct);
        var items = await db.CertificationMatrixEntries.Where(c => c.OrganizationId == scope.OrganizationId && c.AsOfUtc == asOf).ToListAsync(ct);
        return BuildCredentialDashboard<CertificationDashboardDto>(scope.OrganizationId, asOf,
            items.Select(i => (i.CertificationName, i.Status, i.PersonId.ToString(), i.ExpiryDate, i.DaysUntilExpiry, i.ProfessionalCategory)).ToList(),
            dto => new CertificationDashboardDto
            {
                OrganizationId = dto.OrgId,
                AsOfUtc = dto.AsOf,
                Active = dto.Active,
                Expiring = dto.Expiring,
                Expired = dto.Expired,
                RenewalRequired = dto.RenewalRequired,
                ByProfessionalCategory = dto.ByGroup,
                Items = dto.Items
            });
    }

    public async Task<ForecastDto> GetForecastAsync(IntelligenceScope scope, string forecastType, CancellationToken ct = default)
    {
        var forecast = await db.ForecastSnapshots
            .Where(f => f.OrganizationId == scope.OrganizationId && f.ForecastType == forecastType)
            .OrderByDescending(f => f.Version)
            .FirstOrDefaultAsync(ct);

        if (forecast == null)
        {
            return new ForecastDto { OrganizationId = scope.OrganizationId, ForecastType = forecastType, Version = 0, GeneratedAtUtc = DateTime.UtcNow };
        }

        var points = JsonSerializer.Deserialize<List<ForecastDataPointDto>>(forecast.ResultsJson) ?? new List<ForecastDataPointDto>();
        return new ForecastDto
        {
            OrganizationId = scope.OrganizationId,
            ForecastType = forecast.ForecastType,
            Version = forecast.Version,
            GeneratedAtUtc = forecast.GeneratedAtUtc,
            DataPoints = points,
            ParametersSummary = forecast.ParametersJson
        };
    }

    public async Task<RecommendationListDto> GetRecommendationsAsync(IntelligenceScope scope, CancellationToken ct = default)
    {
        var items = await db.WorkforceRecommendations
            .Where(r => r.OrganizationId == scope.OrganizationId && r.IsActive)
            .OrderByDescending(r => r.GeneratedAtUtc)
            .Take(50)
            .ToListAsync(ct);

        return new RecommendationListDto
        {
            OrganizationId = scope.OrganizationId,
            GeneratedAtUtc = items.FirstOrDefault()?.GeneratedAtUtc ?? DateTime.UtcNow,
            Items = items.Select(MapRecommendation).ToList()
        };
    }

    public async Task<WorkforceSearchResultDto> SearchAsync(IntelligenceScope scope, string query, int page, int pageSize, CancellationToken ct = default)
    {
        var q = query.Trim().ToLowerInvariant();
        var baseQuery = db.WorkforceSearchIndexes.Where(i => i.OrganizationId == scope.OrganizationId);
        if (!string.IsNullOrWhiteSpace(q))
        {
            baseQuery = baseQuery.Where(i => i.SearchText.ToLower().Contains(q));
        }

        var total = await baseQuery.CountAsync(ct);
        var hits = await baseQuery.OrderBy(i => i.SearchText).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new WorkforceSearchResultDto
        {
            TotalCount = total,
            Hits = hits.Select(h => new WorkforceSearchHitDto
            {
                PersonId = h.PersonId,
                EmploymentId = h.EmploymentId,
                DisplayName = h.SearchText.Split(' ').FirstOrDefault() ?? h.PersonId.ToString(),
                DepartmentName = h.DepartmentName,
                ProfessionalCategory = h.ProfessionalCategory,
                MatchedFields = string.IsNullOrWhiteSpace(q) ? Array.Empty<string>() : new[] { "searchText" }
            }).ToList()
        };
    }

    public async Task<OrganizationAnalyticsDto> GetOrganizationAnalyticsAsync(IntelligenceScope scope, CancellationToken ct = default)
    {
        var snap = await db.OrganizationAnalyticsSnapshots
            .Where(o => o.OrganizationId == scope.OrganizationId)
            .OrderByDescending(o => o.AsOfUtc)
            .FirstOrDefaultAsync(ct);

        return new OrganizationAnalyticsDto
        {
            OrganizationId = scope.OrganizationId,
            AsOfUtc = snap?.AsOfUtc ?? DateTime.UtcNow,
            DepartmentSizes = DeserializeDict(snap?.DepartmentSizeJson),
            ReportingDepth = snap?.ReportingDepth ?? 0,
            SpanOfControlAvg = snap?.SpanOfControlAvg ?? 0,
            CrossFunctionalAssignments = snap?.CrossFunctionalAssignments ?? 0,
            Vacancies = snap?.Vacancies ?? 0,
            TransfersInPeriod = snap?.TransfersInPeriod ?? 0,
            PromotionsInPeriod = snap?.PromotionsInPeriod ?? 0,
            LeadershipDistribution = DeserializeDict(snap?.LeadershipDistributionJson)
        };
    }

    public async Task<ResponsibilityMatrixDto> GetResponsibilitiesAsync(IntelligenceScope scope, CancellationToken ct = default)
    {
        var asOf = await GetMatrixAsOfAsync(scope, ct);
        var rows = await db.ResponsibilityMatrixEntries.Where(r => r.OrganizationId == scope.OrganizationId && r.AsOfUtc == asOf).ToListAsync(ct);
        return new ResponsibilityMatrixDto
        {
            OrganizationId = scope.OrganizationId,
            AsOfUtc = asOf,
            Covered = rows.Count(r => r.CoverageStatus == "Covered"),
            Gaps = rows.Count(r => r.CoverageStatus == "Gap"),
            Duplicates = rows.Count(r => r.CoverageStatus == "Duplicate"),
            Rows = rows.Select(r => new ResponsibilityRowDto
            {
                ResponsibilityName = r.ResponsibilityName,
                OwnerId = r.OwnerId,
                ReviewerId = r.ReviewerId,
                ApproverId = r.ApproverId,
                CommitteeId = r.CommitteeId,
                CoverageStatus = r.CoverageStatus
            }).ToList()
        };
    }

    public async Task<CommitteeAnalyticsDto> GetCommitteesAsync(IntelligenceScope scope, CancellationToken ct = default)
    {
        var rows = await db.CommitteeAnalyticsSnapshots.Where(c => c.OrganizationId == scope.OrganizationId).ToListAsync(ct);
        return new CommitteeAnalyticsDto
        {
            OrganizationId = scope.OrganizationId,
            AsOfUtc = rows.FirstOrDefault()?.AsOfUtc ?? DateTime.UtcNow,
            Committees = rows.Select(c => new CommitteeAnalyticsRowDto
            {
                CommitteeId = c.CommitteeId,
                CommitteeName = c.CommitteeName,
                MemberCount = c.MemberCount,
                AttendanceRate = c.AttendanceRate,
                MeetingFrequencyPerMonth = c.MeetingFrequencyPerMonth,
                DecisionCompletionRate = c.DecisionCompletionRate,
                ActionClosureRate = c.ActionClosureRate
            }).ToList()
        };
    }

    public async Task<AvailabilityAnalyticsDto> GetAvailabilityAsync(IntelligenceScope scope, CancellationToken ct = default)
    {
        var snap = await db.AvailabilityAnalyticsSnapshots
            .Where(a => a.OrganizationId == scope.OrganizationId)
            .OrderByDescending(a => a.AsOfUtc)
            .FirstOrDefaultAsync(ct);

        if (snap == null)
        {
            var headcount = (await GetLatestSnapshotAsync(scope, ct))?.TotalHeadcount ?? 0;
            return new AvailabilityAnalyticsDto
            {
                OrganizationId = scope.OrganizationId,
                AsOfUtc = DateTime.UtcNow,
                Working = headcount,
                CoveragePercent = 100
            };
        }

        return new AvailabilityAnalyticsDto
        {
            OrganizationId = scope.OrganizationId,
            AsOfUtc = snap.AsOfUtc,
            Working = snap.WorkingCount,
            OnLeave = snap.OnLeaveCount,
            Vacation = snap.VacationCount,
            Unavailable = snap.UnavailableCount,
            InTraining = snap.InTrainingCount,
            InMeeting = snap.InMeetingCount,
            CoveragePercent = snap.CoveragePercent
        };
    }

    private async Task<Domain.ReadModels.WorkforceSnapshot?> GetLatestSnapshotAsync(IntelligenceScope scope, CancellationToken ct)
    {
        var query = db.WorkforceSnapshots.Where(s => s.OrganizationId == scope.OrganizationId);
        if (scope.HospitalId.HasValue) query = query.Where(s => s.HospitalId == scope.HospitalId || s.HospitalId == null);
        if (scope.AsOf.HasValue) query = query.Where(s => s.AsOfUtc <= scope.AsOf.Value);
        return await query.OrderByDescending(s => s.AsOfUtc).FirstOrDefaultAsync(ct);
    }

    private async Task<Domain.ReadModels.ExecutiveMetrics?> GetLatestExecutiveMetricsAsync(IntelligenceScope scope, CancellationToken ct)
        => await db.ExecutiveMetrics.Where(m => m.OrganizationId == scope.OrganizationId)
            .OrderByDescending(m => m.AsOfUtc).FirstOrDefaultAsync(ct);

    private async Task<DateTime> GetMatrixAsOfAsync(IntelligenceScope scope, CancellationToken ct)
    {
        if (scope.AsOf.HasValue) return scope.AsOf.Value;
        var snap = await GetLatestSnapshotAsync(scope, ct);
        return snap?.AsOfUtc ?? DateTime.UtcNow;
    }

    private static Dictionary<string, int> DeserializeDict(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, int>();
        return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
    }

    private static RecommendationItemDto MapRecommendation(Domain.ReadModels.WorkforceRecommendation r)
    {
        var refs = JsonSerializer.Deserialize<List<SourceReferenceDto>>(r.SourceReferencesJson) ?? new List<SourceReferenceDto>();
        return new RecommendationItemDto
        {
            Id = r.Id,
            RecommendationType = r.RecommendationType,
            Title = r.Title,
            Explanation = r.Explanation,
            SourceReferences = refs,
            Confidence = r.Confidence,
            Priority = r.Priority,
            ModelVersion = r.ModelVersion
        };
    }

    private sealed record CredentialDashDto(Guid OrgId, DateTime AsOf, int Active, int Expiring, int Expired, int RenewalRequired, List<CredentialRowDto> ByGroup, List<CredentialRowDto> Items);

    private static T BuildCredentialDashboard<T>(Guid orgId, DateTime asOf, List<(string Name, string Status, string Person, DateTime? Expiry, int? Days, string? Group)> items, Func<CredentialDashDto, T> factory)
    {
        var rows = items.Select(i => new CredentialRowDto
        {
            Name = i.Name,
            Status = i.Status,
            PersonName = i.Person,
            ExpiryDate = i.Expiry,
            DaysUntilExpiry = i.Days,
            GroupKey = i.Group
        }).ToList();

        var dto = new CredentialDashDto(
            orgId, asOf,
            rows.Count(r => r.Status == "Active"),
            rows.Count(r => r.Status == "Expiring"),
            rows.Count(r => r.Status == "Expired"),
            rows.Count(r => r.Status == "RenewalRequired"),
            rows.GroupBy(r => r.GroupKey ?? "Unknown").Select(g => new CredentialRowDto { Name = g.Key!, Status = $"{g.Count()} items" }).ToList(),
            rows);
        return factory(dto);
    }
}
