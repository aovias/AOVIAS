using System.Globalization;
using ComplianceService.Application.AI.Contracts;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;

namespace ComplianceService.Application.AI.Providers;

public sealed class AICapaContextProvider : IAICapaContextProvider
{
    private readonly ICapaQueryService _capaQuery;

    public AICapaContextProvider(ICapaQueryService capaQuery)
    {
        _capaQuery = capaQuery;
    }

    public async Task<AIContextContribution> BuildAsync(AIContextBuildContext context, CancellationToken cancellationToken = default)
    {
        var scope = context.Scope;
        var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sources = new List<AIContextSourceDto>();

        var metrics = await _capaQuery.GetMetricsAsync(scope.OrganizationId, scope.HospitalId, scope.FrameworkId, cancellationToken);

        facts["capaOpenCount"] = metrics.OpenCapas.ToString(CultureInfo.InvariantCulture);
        facts["capaOverdueCount"] = metrics.OverdueCapas.ToString(CultureInfo.InvariantCulture);
        facts["capaPendingReviewCount"] = metrics.PendingReviews.ToString(CultureInfo.InvariantCulture);
        facts["capaRecurringIssueCount"] = metrics.RecurringIssues.ToString(CultureInfo.InvariantCulture);
        facts["capaEffectivePercent"] = Math.Round(metrics.CapaEffectivenessRate * 100, 1).ToString(CultureInfo.InvariantCulture);
        facts["capaIneffectivePercent"] = metrics.EffectiveCapas + metrics.IneffectiveCapas > 0
            ? Math.Round(100.0 - metrics.CapaEffectivenessRate * 100, 1).ToString(CultureInfo.InvariantCulture)
            : "0";

        if (metrics.TopRootCauses.Count > 0)
        {
            facts["capaTopRootCauses"] = string.Join("; ",
                metrics.TopRootCauses.Take(5).Select(r => $"{r.Label}: {r.Count}"));
        }

        if (metrics.DepartmentCapaPerformance.Count > 0)
        {
            facts["capaDepartmentPerformance"] = string.Join("; ",
                metrics.DepartmentCapaPerformance.Take(5).Select(d => $"{d.Label}: {d.Count} open"));
        }

        foreach (var dept in metrics.DepartmentCapaPerformance.OrderByDescending(d => d.Count).Take(3))
        {
            sources.Add(new AIContextSourceDto
            {
                SourceType = "CapaDepartment",
                SourceId = dept.Label,
                Summary = $"{dept.Label}: {dept.Count} open CAPA items",
                RelevanceScore = 0.8m
            });
        }

        if (context.Request.EntityType?.Equals("Capa", StringComparison.OrdinalIgnoreCase) == true
            && Guid.TryParse(context.Request.EntityId, out var deficiencyId))
        {
            var capa = await _capaQuery.GetCapaAsync(deficiencyId, cancellationToken);
            if (capa != null)
            {
                facts["focusedCapaCode"] = capa.DeficiencyCode ?? deficiencyId.ToString();
                facts["focusedCapaCorrectiveStatus"] = capa.CorrectiveActionStatus ?? "Unknown";
                facts["focusedCapaPreventiveStatus"] = capa.PreventiveActionStatus ?? "Unknown";
                facts["focusedCapaEffectivenessStatus"] = capa.EffectivenessReviewStatus ?? "Unknown";
                if (capa.CorrectiveActionOverdue || capa.PreventiveActionOverdue)
                {
                    facts["focusedCapaOverdue"] = "true";
                }
            }
        }

        return new AIContextContribution { Facts = facts, Sources = sources };
    }
}
