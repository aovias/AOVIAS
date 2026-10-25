using System.Globalization;
using ComplianceService.Application.AI.Contracts;
using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.AI.Providers;

public sealed class AISlaContextProvider : IAISlaContextProvider
{
    private readonly IEscalationService _escalation;
    private readonly IAnalyticsAggregationService _aggregation;

    public AISlaContextProvider(IEscalationService escalation, IAnalyticsAggregationService aggregation)
    {
        _escalation = escalation;
        _aggregation = aggregation;
    }

    public async Task<AIContextContribution> BuildAsync(AIContextBuildContext context, CancellationToken cancellationToken = default)
    {
        var scope = context.Scope;
        var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sources = new List<AIContextSourceDto>();

        var sla = await _escalation.GetMetricsForScopeAsync(
            scope.OrganizationId, scope.HospitalId, scope.FrameworkId, cancellationToken);
        var governance = await _aggregation.GetGovernanceMetricsAsync(scope, cancellationToken);

        facts["slaBreachedCount"] = sla.BreachedCount.ToString(CultureInfo.InvariantCulture);
        facts["slaAtRiskCount"] = sla.AtRiskCount.ToString(CultureInfo.InvariantCulture);
        facts["slaOnTrackCount"] = sla.OnTrackCount.ToString(CultureInfo.InvariantCulture);
        facts["slaEscalatedCount"] = sla.EscalatedCount.ToString(CultureInfo.InvariantCulture);
        facts["slaCriticalEscalations"] = sla.CriticalEscalations.ToString(CultureInfo.InvariantCulture);
        facts["slaHealthScore"] = governance.Sla.SlaHealthScore.ToString(CultureInfo.InvariantCulture);
        facts["slaBreachRatePercent"] = Math.Round(sla.BreachRate * 100, 1).ToString(CultureInfo.InvariantCulture);
        facts["slaEscalationRatePercent"] = Math.Round(sla.EscalationRate * 100, 1).ToString(CultureInfo.InvariantCulture);

        if (sla.DepartmentSlaCompliance.Count > 0)
        {
            facts["slaDepartmentHealth"] = string.Join("; ",
                sla.DepartmentSlaCompliance
                    .OrderBy(d => d.Count)
                    .Take(5)
                    .Select(d => $"{d.Label}: {d.Count} at risk/breached"));
        }

        if (sla.BreachedCount > 0)
        {
            sources.Add(new AIContextSourceDto
            {
                SourceType = "SlaBreach",
                SourceId = "summary",
                Summary = $"{sla.BreachedCount} deficiencies have breached SLA resolution targets",
                RelevanceScore = 0.95m
            });
        }

        if (sla.CriticalEscalations > 0)
        {
            sources.Add(new AIContextSourceDto
            {
                SourceType = "Escalation",
                SourceId = "critical",
                Summary = $"{sla.CriticalEscalations} critical escalations require leadership attention",
                RelevanceScore = 0.97m
            });
        }

        return new AIContextContribution { Facts = facts, Sources = sources };
    }
}
