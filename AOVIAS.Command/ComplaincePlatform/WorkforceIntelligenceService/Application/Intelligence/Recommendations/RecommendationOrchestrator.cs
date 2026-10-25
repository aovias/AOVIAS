using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WorkforceIntelligenceService.Application.Contracts;
using WorkforceIntelligenceService.Application.Intelligence.Platform;
using WorkforceIntelligenceService.Domain.ReadModels;
using WorkforceIntelligenceService.Infrastructure.Clients;
using WorkforceIntelligenceService.Infrastructure.Persistence;

namespace WorkforceIntelligenceService.Application.Intelligence.Recommendations;

public sealed class RecommendationOrchestrator(
    WorkforceIntelligenceDbContext db,
    IProfessionalSourceClient professional,
    IIntelligenceOutboxWriter outbox,
    IAnalyticsCacheService cache) : IRecommendationOrchestrator
{
    public async Task<int> GenerateAsync(Guid organizationId, string correlationId, CancellationToken ct = default)
    {
        var existing = await db.WorkforceRecommendations.Where(r => r.OrganizationId == organizationId && r.IsActive).ToListAsync(ct);
        foreach (var r in existing) r.IsActive = false;

        var recommendations = new List<WorkforceRecommendation>();
        var warningDays = 90;

        var expiring = await professional.GetExpiringAsync(organizationId, warningDays, ct);
        foreach (var item in expiring.Take(20))
        {
            recommendations.Add(new WorkforceRecommendation
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                RecommendationType = item.CredentialType.Contains("License") ? "LicenseRenewal" : "CertificationRenewal",
                Title = $"Renew {item.CredentialName} for {item.PersonName}",
                Explanation = $"{item.CredentialName} expires in {item.DaysUntilExpiry} days. Schedule renewal to maintain compliance.",
                SourceReferencesJson = JsonSerializer.Serialize(new[]
                {
                    new SourceReferenceDto { Service = "ProfessionalService", EntityType = item.CredentialType, EntityId = item.PersonId.ToString() }
                }),
                Confidence = 0.95m,
                ModelVersion = "rule-based-v1",
                Priority = item.DaysUntilExpiry < 30 ? "High" : "Medium",
                GeneratedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                IsActive = true
            });
        }

        var trainingGaps = await db.TrainingMatrixEntries
            .Where(t => t.OrganizationId == organizationId && t.IsMandatory && t.Status != "Completed")
            .Take(15)
            .ToListAsync(ct);
        foreach (var gap in trainingGaps)
        {
            recommendations.Add(new WorkforceRecommendation
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                RecommendationType = "TrainingSuggestion",
                Title = $"Complete mandatory training: {gap.CourseName}",
                Explanation = $"Mandatory training '{gap.CourseName}' is {gap.Status}. Assign completion before due date {gap.DueDate:yyyy-MM-dd}.",
                SourceReferencesJson = JsonSerializer.Serialize(new[]
                {
                    new SourceReferenceDto { Service = "ProfessionalService", EntityType = "TrainingRecord", EntityId = gap.EmploymentId.ToString() }
                }),
                Confidence = 1.0m,
                ModelVersion = "rule-based-v1",
                Priority = gap.Status == "Expired" ? "High" : "Medium",
                GeneratedAtUtc = DateTime.UtcNow,
                IsActive = true
            });
        }

        var respGaps = await db.ResponsibilityMatrixEntries
            .Where(r => r.OrganizationId == organizationId && r.CoverageStatus == "Gap")
            .Take(10)
            .ToListAsync(ct);
        foreach (var gap in respGaps)
        {
            recommendations.Add(new WorkforceRecommendation
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                RecommendationType = "ComplianceRisk",
                Title = $"Assign owner for {gap.ResponsibilityName}",
                Explanation = "Responsibility has no assigned owner. Governance coverage gap increases compliance risk.",
                SourceReferencesJson = JsonSerializer.Serialize(new[]
                {
                    new SourceReferenceDto { Service = "GovernanceService", EntityType = "Responsibility", EntityId = gap.ResponsibilityId.ToString() }
                }),
                Confidence = 1.0m,
                ModelVersion = "rule-based-v1",
                Priority = "High",
                GeneratedAtUtc = DateTime.UtcNow,
                IsActive = true
            });
        }

        var skillGaps = await db.SkillMatrixEntries
            .Where(s => s.OrganizationId == organizationId && s.IsCritical && s.Gap != null)
            .Take(10)
            .ToListAsync(ct);
        foreach (var gap in skillGaps)
        {
            recommendations.Add(new WorkforceRecommendation
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                RecommendationType = "SkillDevelopment",
                Title = $"Develop critical skill: {gap.SkillName}",
                Explanation = $"Critical skill '{gap.SkillName}' is below target level. Consider training or mentorship.",
                SourceReferencesJson = JsonSerializer.Serialize(new[]
                {
                    new SourceReferenceDto { Service = "ProfessionalService", EntityType = "ProfessionalSkill", EntityId = gap.EmploymentId.ToString() }
                }),
                Confidence = 0.85m,
                ModelVersion = "rule-based-v1",
                Priority = "Medium",
                GeneratedAtUtc = DateTime.UtcNow,
                IsActive = true
            });
        }

        db.WorkforceRecommendations.AddRange(recommendations);
        await db.SaveChangesAsync(ct);
        await cache.InvalidateOrganizationAsync(organizationId, ct);
        await outbox.EnqueueAsync("RecommendationGenerated", organizationId, new { organizationId, count = recommendations.Count }, ct);
        return recommendations.Count;
    }
}
