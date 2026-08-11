using System.Globalization;
using ComplianceService.Application.AI.Contracts;
using ComplianceService.Application.Analytics;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain.Enums;
using ComplianceService.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ComplianceService.Infrastructure.Persistence;

namespace ComplianceService.Application.AI.Providers;

public sealed class AIDeficiencyContextProvider : IAIDeficiencyContextProvider
{
    private readonly IDeficiencyMetricsService _metrics;
    private readonly ComplianceDbContext _db;

    public AIDeficiencyContextProvider(IDeficiencyMetricsService metrics, ComplianceDbContext db)
    {
        _metrics = metrics;
        _db = db;
    }

    public async Task<AIContextContribution> BuildAsync(AIContextBuildContext context, CancellationToken cancellationToken = default)
    {
        var scope = context.Scope;
        var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sources = new List<AIContextSourceDto>();

        var counts = await _metrics.GetCountsAsync(scope, cancellationToken);
        facts["deficiencyOpenCount"] = counts.Open.ToString(CultureInfo.InvariantCulture);
        facts["deficiencyCriticalOpenCount"] = counts.CriticalOpen.ToString(CultureInfo.InvariantCulture);
        facts["deficiencyRecurringOpenCount"] = counts.RecurringOpen.ToString(CultureInfo.InvariantCulture);
        facts["deficiencyEscalatedOpenCount"] = counts.EscalatedOpen.ToString(CultureInfo.InvariantCulture);
        facts["deficiencyBlockingOpenCount"] = counts.BlockingOpen.ToString(CultureInfo.InvariantCulture);

        var openRows = await _metrics.GetScopedOpenAsync(scope, cancellationToken);

        var deptBreakdown = openRows
            .GroupBy(d => string.IsNullOrWhiteSpace(d.Department) ? "Unassigned" : d.Department!.Trim())
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();
        if (deptBreakdown.Count > 0)
        {
            facts["deficiencyDepartmentBreakdown"] = string.Join("; ", deptBreakdown);
        }

        var rootCauseBreakdown = openRows
            .Where(d => d.RootCauseCategory.HasValue)
            .GroupBy(d => d.RootCauseCategory!.Value)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Select(g => $"{g.Key}: {g.Count()}")
            .ToList();
        if (rootCauseBreakdown.Count > 0)
        {
            facts["deficiencyRootCauseBreakdown"] = string.Join("; ", rootCauseBreakdown);
        }

        var topRisks = openRows
            .OrderByDescending(d => d.Severity == DeficiencySeverity.Critical ? 4 :
                d.Severity == DeficiencySeverity.Major ? 3 :
                d.Severity == DeficiencySeverity.Minor ? 2 : 1)
            .ThenByDescending(d => d.IsRecurringIssue)
            .Take(5)
            .ToList();

        foreach (var d in topRisks)
        {
            var label = string.IsNullOrWhiteSpace(d.DeficiencyCode) ? d.Id.ToString()[..8] : d.DeficiencyCode;
            sources.Add(new AIContextSourceDto
            {
                SourceType = "Deficiency",
                SourceId = d.Id.ToString(),
                Summary = $"{label} ({d.Severity}, {d.Status}) — {(d.ProblemStatement ?? d.Title ?? "Open deficiency").Trim()}",
                RelevanceScore = d.Severity == DeficiencySeverity.Critical ? 0.98m :
                    d.Severity == DeficiencySeverity.Major ? 0.9m : 0.75m
            });
        }

        if (topRisks.Count > 0)
        {
            facts["deficiencyTopRisks"] = string.Join("; ",
                topRisks.Select(d =>
                    $"{d.DeficiencyCode ?? d.Id.ToString()[..8]} ({d.Severity}): {(d.ProblemStatement ?? d.Title ?? "Open").Trim()}"));
        }

        if (context.Request.EntityType?.Equals("Deficiency", StringComparison.OrdinalIgnoreCase) == true
            && Guid.TryParse(context.Request.EntityId, out var deficiencyId))
        {
            var detail = await _db.ComplianceDeficiencies.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == deficiencyId, cancellationToken);
            if (detail != null)
            {
                facts["focusedDeficiencyCode"] = detail.DeficiencyCode ?? deficiencyId.ToString();
                facts["focusedDeficiencySeverity"] = detail.Severity.ToString();
                facts["focusedDeficiencyStatus"] = detail.Status.ToString();
                if (!string.IsNullOrWhiteSpace(detail.ProblemStatement))
                {
                    facts["focusedDeficiencyProblem"] = detail.ProblemStatement.Trim();
                }
            }
        }

        return new AIContextContribution { Facts = facts, Sources = sources };
    }
}
