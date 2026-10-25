using ComplianceService.Domain;
using ComplianceService.Application.Copilot.Models;
using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Copilot.Rules.Graph;

/// <summary>Sprint 17A — Advisory when entities lack traceability linkage.</summary>
public sealed class TraceabilityGapRule : RecommendationRuleBase
{
    private readonly ComplianceDbContext _db;

    public TraceabilityGapRule(ComplianceDbContext db) => _db = db;

    public override string RuleName => "graph.traceability_gap";
    public override RecommendationCategory Category => RecommendationCategory.Analytics;
    public override IReadOnlyList<CopilotPersona> TargetPersonas =>
        [CopilotPersona.QualityManagerAssistant, CopilotPersona.AccreditationCoach, CopilotPersona.AuditorAssistant];

    public override async Task<IReadOnlyList<RecommendationCandidate>> EvaluateAsync(
        RecommendationRuleContext context,
        CancellationToken cancellationToken = default)
    {
        var results = new List<RecommendationCandidate>();
        if (context.Scope.OrganizationId == Guid.Empty) return results;

        var unmappedDocs = await _db.ControlledDocuments.AsNoTracking()
            .CountAsync(d => d.TenantId == context.Scope.OrganizationId && !d.IsArchived && d.MappedObjectiveElementId == null, cancellationToken);
        var openFindingsNoOe = await _db.Findings.AsNoTracking()
            .CountAsync(f => f.TenantId == context.Scope.OrganizationId && f.Status != FindingStatus.Closed && f.ObjectiveElementId == null, cancellationToken);

        if (unmappedDocs == 0 && openFindingsNoOe == 0) return results;

        foreach (var persona in TargetPersonas)
        {
            results.Add(Build(
                persona: persona.ToString(),
                severity: RecommendationSeverity.Medium,
                title: "Traceability gaps detected in compliance graph",
                rationale: $"{unmappedDocs} documents without OE mapping; {openFindingsNoOe} open findings without objective element linkage.",
                expectedOutcome: "Complete traceability chains improve audit readiness and cross-module navigation.",
                riskIfIgnored: "Auditors cannot follow evidence chains; executive impact views remain incomplete.",
                rationaleData: MakeRationale(RuleName, metrics: new()
                {
                    ["UnmappedDocuments"] = unmappedDocs.ToString(),
                    ["UnlinkedFindings"] = openFindingsNoOe.ToString(),
                }, scope: context.Scope),
                sourceEntityType: "ComplianceGraph",
                deepLink: "/app/compliance-graph/traceability",
                expiresAtUtc: DateTime.UtcNow.AddHours(48)));
        }

        return results;
    }
}
