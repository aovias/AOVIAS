using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WorkforceIntelligenceService.Application.Contracts;
using WorkforceIntelligenceService.Application.Intelligence.Platform;
using WorkforceIntelligenceService.Domain.ReadModels;
using WorkforceIntelligenceService.Infrastructure.Clients;
using WorkforceIntelligenceService.Infrastructure.Persistence;

namespace WorkforceIntelligenceService.Application.Intelligence.Snapshots;

public sealed class WorkforceSnapshotService(
    WorkforceIntelligenceDbContext db,
    IWorkforceSourceClient workforce,
    IProfessionalSourceClient professional,
    IGovernanceSourceClient governance,
    IOrganizationSourceClient organization,
    IIntelligenceOutboxWriter outbox,
    IOptions<IntelligenceOptions> options,
    ILogger<WorkforceSnapshotService> logger) : IWorkforceSnapshotService, ISnapshotJob
{
    public string JobName => "workforce-snapshot";

    public Task ExecuteAsync(Guid organizationId, string correlationId, CancellationToken ct = default)
        => RefreshOrganizationAsync(organizationId, correlationId, fullReconcile: false, ct);

    public async Task RefreshOrganizationAsync(Guid organizationId, string correlationId, bool fullReconcile = false, CancellationToken ct = default)
    {
        var asOf = DateTime.UtcNow;
        var employments = (await workforce.ListEmploymentsAsync(organizationId, ct))
            .Where(e => string.Equals(e.EmploymentStatus, "Active", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var profiles = (await professional.ListProfilesAsync(organizationId, ct)).ToList();
        var gov = await governance.GetSummaryAsync(organizationId, ct);
        var deptSizes = await organization.GetDepartmentSizesAsync(organizationId, ct);

        var prev = await db.WorkforceSnapshots
            .Where(s => s.OrganizationId == organizationId)
            .OrderByDescending(s => s.AsOfUtc)
            .FirstOrDefaultAsync(ct);

        var version = (prev?.Version ?? 0) + 1;
        var headcount = employments.Count;
        var fte = employments.Sum(e => e.Fte ?? 1m);

        var byDept = employments
            .GroupBy(e => e.MasterDepartmentId?.ToString() ?? "Unassigned")
            .ToDictionary(g => g.Key, g => g.Count());
        var byHospital = employments
            .GroupBy(e => e.HospitalId?.ToString() ?? "Unassigned")
            .ToDictionary(g => g.Key, g => g.Count());
        var byType = employments
            .GroupBy(e => e.EmploymentType ?? "Unknown")
            .ToDictionary(g => g.Key, g => g.Count());
        var byProfession = profiles
            .GroupBy(p => p.YearsOfExperience switch
            {
                < 2 => "Junior",
                < 10 => "Mid",
                _ => "Senior"
            })
            .ToDictionary(g => g.Key, g => g.Count());

        var expDist = profiles
            .GroupBy(p => p.YearsOfExperience switch
            {
                null => "Unknown",
                < 5 => "0-4",
                < 10 => "5-9",
                < 20 => "10-19",
                _ => "20+"
            })
            .ToDictionary(g => g.Key, g => g.Count());

        var qualDist = profiles
            .SelectMany(p => p.Qualifications)
            .GroupBy(q => q.Title)
            .ToDictionary(g => g.Key, g => g.Count());

        decimal? growth = prev is null || prev.TotalHeadcount == 0 ? null : (decimal)(headcount - prev.TotalHeadcount) / prev.TotalHeadcount;
        var terminations = employments.Count(e => e.EndDate.HasValue && e.EndDate > DateTime.UtcNow.AddMonths(-12));
        decimal? attrition = headcount == 0 ? null : (decimal)terminations / headcount;
        decimal? retention = attrition.HasValue ? 1 - attrition : null;

        var snapshot = new WorkforceSnapshot
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AsOfUtc = asOf,
            Version = version,
            CorrelationId = correlationId,
            TotalHeadcount = headcount,
            TotalFte = fte,
            ByDepartmentJson = JsonSerializer.Serialize(byDept),
            ByHospitalJson = JsonSerializer.Serialize(byHospital),
            ByProfessionJson = JsonSerializer.Serialize(byProfession),
            ByEmploymentTypeJson = JsonSerializer.Serialize(byType),
            AgeDistributionJson = JsonSerializer.Serialize(new Dictionary<string, int>()),
            ExperienceDistributionJson = JsonSerializer.Serialize(expDist),
            QualificationDistributionJson = JsonSerializer.Serialize(qualDist),
            GrowthRate = growth,
            AttritionRate = attrition,
            RetentionRate = retention
        };
        db.WorkforceSnapshots.Add(snapshot);

        await ProjectMatricesAsync(organizationId, asOf, employments, profiles, ct);
        await ProjectGovernanceAsync(organizationId, asOf, gov, ct);
        await ProjectOrgAnalyticsAsync(organizationId, asOf, deptSizes, employments, ct);
        await ProjectSearchIndexAsync(organizationId, asOf, employments, profiles, ct);

        var trainingRows = await db.TrainingMatrixEntries.Where(t => t.OrganizationId == organizationId && t.AsOfUtc == asOf).ToListAsync(ct);
        var mandatory = trainingRows.Where(t => t.IsMandatory).ToList();
        var trainingCompliance = mandatory.Count == 0 ? 100m : mandatory.Count(t => t.Status == "Completed") * 100m / mandatory.Count;

        var expiring = await professional.GetExpiringAsync(organizationId, options.Value.CredentialExpiryWarningDays, ct);
        var skillGaps = await db.SkillMatrixEntries.CountAsync(s => s.OrganizationId == organizationId && s.Gap != null, ct);

        var exec = new ExecutiveMetrics
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AsOfUtc = asOf,
            Headcount = headcount,
            OpenVacancies = Math.Max(0, gov.TotalResponsibilities - gov.AssignedResponsibilities),
            TrainingCompliancePercent = trainingCompliance,
            CredentialRiskCount = expiring.Count,
            CriticalSkillGaps = skillGaps,
            CommitteeActionBacklog = gov.OpenActions,
            AvailabilityCoveragePercent = headcount == 0 ? 0 : 85m,
            SnapshotId = snapshot.Id
        };
        db.ExecutiveMetrics.Add(exec);

        await db.SaveChangesAsync(ct);
        await outbox.EnqueueAsync("WorkforceSnapshotUpdated", snapshot.Id, new { organizationId, snapshotId = snapshot.Id, asOf, version }, ct);

        if (fullReconcile && prev != null && prev.TotalHeadcount != headcount)
        {
            logger.LogWarning("Reconciliation drift for org {OrgId}: snapshot headcount {New} vs previous {Old}", organizationId, headcount, prev.TotalHeadcount);
        }

        logger.LogInformation("Workforce snapshot v{Version} for org {OrgId}: headcount={Headcount}", version, organizationId, headcount);
    }

    private async Task ProjectMatricesAsync(
        Guid organizationId, DateTime asOf,
        IReadOnlyList<SourceEmploymentDto> employments,
        IReadOnlyList<SourceProfessionalProfileDto> profiles,
        CancellationToken ct)
    {
        var warningDays = options.Value.CredentialExpiryWarningDays;
        var profileByEmployment = profiles.ToDictionary(p => p.EmploymentId);

        foreach (var emp in employments)
        {
            if (!profileByEmployment.TryGetValue(emp.Id, out var profile)) continue;

            foreach (var skill in profile.Skills)
            {
                db.SkillMatrixEntries.Add(new SkillMatrixEntry
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    HospitalId = emp.HospitalId,
                    DepartmentId = emp.MasterDepartmentId,
                    PersonId = emp.PersonId,
                    EmploymentId = emp.Id,
                    SkillName = skill.SkillName,
                    CurrentLevel = skill.ProficiencyLevel ?? "Unknown",
                    TargetLevel = skill.IsCritical ? "Expert" : null,
                    Gap = skill.IsCritical && skill.ProficiencyLevel != "Expert" ? "Below target" : null,
                    IsCritical = skill.IsCritical,
                    CoverageScore = skill.ProficiencyLevel == "Expert" ? 100 : 50,
                    AsOfUtc = asOf
                });
            }

            foreach (var comp in profile.Competencies)
            {
                var gap = comp.TargetLevel != null && comp.Level != comp.TargetLevel ? $"{comp.Level} vs {comp.TargetLevel}" : null;
                db.CompetencyMatrixEntries.Add(new CompetencyMatrixEntry
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    DepartmentId = emp.MasterDepartmentId,
                    PersonId = emp.PersonId,
                    EmploymentId = emp.Id,
                    CompetencyName = comp.SkillName,
                    CurrentLevel = comp.Level ?? "Unknown",
                    TargetLevel = comp.TargetLevel,
                    Gap = gap,
                    AssessmentDate = comp.AssessmentDate,
                    AsOfUtc = asOf
                });
            }

            foreach (var tr in profile.TrainingRecords)
            {
                var status = tr.CompletionDate.HasValue ? "Completed"
                    : tr.DueDate.HasValue && tr.DueDate < DateTime.UtcNow ? "Expired"
                    : tr.DueDate.HasValue && tr.DueDate < DateTime.UtcNow.AddDays(30) ? "Upcoming"
                    : "Pending";

                db.TrainingMatrixEntries.Add(new TrainingMatrixEntry
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    HospitalId = emp.HospitalId,
                    DepartmentId = emp.MasterDepartmentId,
                    PersonId = emp.PersonId,
                    EmploymentId = emp.Id,
                    CourseName = tr.CourseName,
                    IsMandatory = tr.IsMandatory,
                    Status = status,
                    CompliancePercent = status == "Completed" ? 100 : 0,
                    DueDate = tr.DueDate,
                    AsOfUtc = asOf
                });
            }

            foreach (var lic in profile.Licenses)
            {
                var status = ClassifyCredential(lic.ExpiryDate, lic.Status, warningDays);
                db.LicenseMatrixEntries.Add(new LicenseMatrixEntry
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    PersonId = emp.PersonId,
                    EmploymentId = emp.Id,
                    LicenseName = lic.LicenseNumber,
                    Status = status,
                    ExpiryDate = lic.ExpiryDate,
                    DaysUntilExpiry = lic.ExpiryDate.HasValue ? (int)(lic.ExpiryDate.Value - DateTime.UtcNow).TotalDays : null,
                    DepartmentId = emp.MasterDepartmentId,
                    AsOfUtc = asOf
                });
            }

            foreach (var cert in profile.Certifications)
            {
                var status = ClassifyCredential(cert.ExpiryDate, cert.VerificationStatus, warningDays);
                db.CertificationMatrixEntries.Add(new CertificationMatrixEntry
                {
                    Id = Guid.NewGuid(),
                    OrganizationId = organizationId,
                    PersonId = emp.PersonId,
                    EmploymentId = emp.Id,
                    CertificationName = cert.CertificationName,
                    Status = status,
                    ExpiryDate = cert.ExpiryDate,
                    DaysUntilExpiry = cert.ExpiryDate.HasValue ? (int)(cert.ExpiryDate.Value - DateTime.UtcNow).TotalDays : null,
                    DepartmentId = emp.MasterDepartmentId,
                    AsOfUtc = asOf
                });
            }
        }

        await Task.CompletedTask;
    }

    private static string ClassifyCredential(DateTime? expiry, string status, int warningDays)
    {
        if (expiry.HasValue && expiry.Value < DateTime.UtcNow) return "Expired";
        if (expiry.HasValue && expiry.Value < DateTime.UtcNow.AddDays(warningDays)) return "Expiring";
        if (string.Equals(status, "RenewalRequired", StringComparison.OrdinalIgnoreCase)) return "RenewalRequired";
        return "Active";
    }

    private async Task ProjectGovernanceAsync(Guid organizationId, DateTime asOf, SourceGovernanceSummaryDto gov, CancellationToken ct)
    {
        foreach (var r in gov.Responsibilities)
        {
            var coverage = r.OwnerId.HasValue ? "Covered" : "Gap";
            if (gov.Responsibilities.Count(x => x.Name == r.Name) > 1) coverage = "Duplicate";
            db.ResponsibilityMatrixEntries.Add(new ResponsibilityMatrixEntry
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ResponsibilityId = r.Id,
                ResponsibilityName = r.Name,
                OwnerId = r.OwnerId,
                ReviewerId = r.ReviewerId,
                ApproverId = r.ApproverId,
                CommitteeId = r.CommitteeId,
                CoverageStatus = coverage,
                AsOfUtc = asOf
            });
        }

        foreach (var c in gov.Committees)
        {
            db.CommitteeAnalyticsSnapshots.Add(new CommitteeAnalyticsSnapshot
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                CommitteeId = c.Id,
                CommitteeName = c.Name,
                MemberCount = c.MemberCount,
                AttendanceRate = c.AttendanceRate,
                MeetingFrequencyPerMonth = 1,
                DecisionCompletionRate = c.DecisionCompletionRate,
                ActionClosureRate = c.ActionClosureRate,
                AsOfUtc = asOf
            });
        }

        await Task.CompletedTask;
    }

    private async Task ProjectOrgAnalyticsAsync(
        Guid organizationId, DateTime asOf,
        Dictionary<string, int> deptSizes,
        IReadOnlyList<SourceEmploymentDto> employments,
        CancellationToken ct)
    {
        var managers = employments.Where(e => e.ManagerEmploymentId.HasValue).GroupBy(e => e.ManagerEmploymentId!.Value).ToList();
        var span = managers.Count == 0 ? 0 : (decimal)managers.Average(g => g.Count());

        db.OrganizationAnalyticsSnapshots.Add(new OrganizationAnalyticsSnapshot
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            DepartmentSizeJson = JsonSerializer.Serialize(deptSizes),
            ReportingDepth = 3,
            LeadershipDistributionJson = JsonSerializer.Serialize(managers.ToDictionary(m => m.Key.ToString(), m => m.Count())),
            SpanOfControlAvg = span,
            CrossFunctionalAssignments = 0,
            Vacancies = 0,
            TransfersInPeriod = 0,
            PromotionsInPeriod = 0,
            AsOfUtc = asOf
        });
        await Task.CompletedTask;
    }

    private async Task ProjectSearchIndexAsync(
        Guid organizationId, DateTime asOf,
        IReadOnlyList<SourceEmploymentDto> employments,
        IReadOnlyList<SourceProfessionalProfileDto> profiles,
        CancellationToken ct)
    {
        var existing = await db.WorkforceSearchIndexes.Where(x => x.OrganizationId == organizationId).ToListAsync(ct);
        db.WorkforceSearchIndexes.RemoveRange(existing);

        var profileMap = profiles.ToDictionary(p => p.EmploymentId);
        foreach (var emp in employments)
        {
            profileMap.TryGetValue(emp.Id, out var profile);
            var skills = profile?.Skills.Select(s => s.SkillName).ToList() ?? new List<string>();
            var certs = profile?.Certifications.Select(c => c.CertificationName).ToList() ?? new List<string>();
            var searchText = string.Join(' ', new[] { emp.PersonSearchName }.Concat(skills).Concat(certs));

            db.WorkforceSearchIndexes.Add(new WorkforceSearchIndex
            {
                Id = Guid.NewGuid(),
                PersonId = emp.PersonId,
                EmploymentId = emp.Id,
                OrganizationId = organizationId,
                SearchText = searchText,
                SkillsJson = JsonSerializer.Serialize(skills),
                CertificationsJson = JsonSerializer.Serialize(certs),
                YearsOfExperience = profile?.YearsOfExperience,
                IndexedAtUtc = asOf
            });
        }
    }
}
