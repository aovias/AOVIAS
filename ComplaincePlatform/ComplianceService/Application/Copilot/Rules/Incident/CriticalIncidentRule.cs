using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Copilot.Rules.Incident;

/// <summary>Sprint 16 — Advisory recommendation when critical patient safety incidents are open.</summary>
public sealed class CriticalIncidentRule : RecommendationRuleBase
{
    private readonly ComplianceDbContext _db;

    public CriticalIncidentRule(ComplianceDbContext db) => _db = db;

    public override string RuleName => "incident.critical_open";
    public override RecommendationCategory Category => RecommendationCategory.Deficiency;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.QualityManagerAssistant, CopilotPersona.CapaAdvisor, CopilotPersona.ExecutiveAdvisor];

    public override async Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        if (context.Scope.OrganizationId == Guid.Empty) return results;

        var criticalOpen = await _db.Incidents.AsNoTracking()
            .CountAsync(i => i.TenantId == context.Scope.OrganizationId
                && (i.Severity == Domain.Enums.IncidentSeverity.Critical || i.Severity == Domain.Enums.IncidentSeverity.Sentinel)
                && i.Status != Domain.Enums.IncidentStatus.Closed
                && i.Status != Domain.Enums.IncidentStatus.Cancelled, cancellationToken);

        if (criticalOpen <= 0) return results;

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: RecommendationSeverity.Critical,
                title: $"{criticalOpen} critical patient safety incident{(criticalOpen != 1 ? "s" : "")} require investigation",
                rationale: "Open critical or sentinel events require structured investigation and may need RCA.",
                expectedOutcome: "Timely investigation reduces patient harm and supports accreditation readiness.",
                riskIfIgnored: "Unresolved critical incidents increase regulatory and patient safety risk.",
                rationaleData: MakeRationale(RuleName, metrics: new() { ["CriticalOpen"] = criticalOpen.ToString() }, scope: context.Scope),
                sourceEntityType: "Incident",
                deepLink: "/app/patient-safety/command-center?quickFilter=critical",
                expiresAtUtc: DateTime.UtcNow.AddHours(12)));
        }

        return results;
    }
}
