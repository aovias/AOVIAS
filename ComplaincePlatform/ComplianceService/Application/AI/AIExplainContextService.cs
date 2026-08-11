using ComplianceService.Application.AI.Contracts;
using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.AI;

/// <summary>
/// Reusable explainability layer for deficiencies, CAPA, SLA, readiness, analytics KPIs, and stage unlock (Sprint 12C Phase 8).
/// Read-only — produces structured summaries for the AI assistant; does not mutate workflows.
/// </summary>
public interface IAIExplainContextService
{
    Task<AIExplainContextResponseDto> ExplainAsync(AIExplainContextRequestDto request, CancellationToken cancellationToken = default);
    Task<AIReadOnlyToolResponseDto> ExecuteReadOnlyToolAsync(AIReadOnlyToolRequestDto request, CancellationToken cancellationToken = default);
}

public sealed class AIExplainContextService : IAIExplainContextService
{
    private readonly IAnalyticsQueryService _analytics;
    private readonly ICapaQueryService _capaQuery;
    private readonly IStageUnlockingService _stageUnlocking;
    private readonly IDeficiencyMetricsService _deficiencyMetrics;
    private readonly IEscalationService _escalation;
    private readonly ComplianceDbContext _db;

    public AIExplainContextService(
        IAnalyticsQueryService analytics,
        ICapaQueryService capaQuery,
        IStageUnlockingService stageUnlocking,
        IDeficiencyMetricsService deficiencyMetrics,
        IEscalationService escalation,
        ComplianceDbContext db)
    {
        _analytics = analytics;
        _capaQuery = capaQuery;
        _stageUnlocking = stageUnlocking;
        _deficiencyMetrics = deficiencyMetrics;
        _escalation = escalation;
        _db = db;
    }

    public async Task<AIExplainContextResponseDto> ExplainAsync(
        AIExplainContextRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var topic = request.Topic.Trim();
        var entityType = request.EntityType?.Trim() ?? InferEntityType(topic);
        var scope = await _analytics.ResolveScopeAsync(
            request.OrganizationId, request.HospitalId, request.FrameworkId, cancellationToken);

        return entityType.ToLowerInvariant() switch
        {
            "stageunlock" or "stage" or "maturity" => await ExplainStageUnlockAsync(scope, topic, request, cancellationToken),
            "deficiency" => await ExplainDeficiencyAsync(scope, topic, request, cancellationToken),
            "capa" => await ExplainCapaAsync(topic, request, cancellationToken),
            "sla" => await ExplainSlaAsync(scope, topic, cancellationToken),
            "escalation" => await ExplainEscalationAsync(scope, topic, cancellationToken),
            "analytics" or "kpi" => await ExplainAnalyticsKpiAsync(scope, topic, request, cancellationToken),
            _ => await ExplainGeneralAsync(scope, topic, cancellationToken)
        };
    }

    public Task<AIReadOnlyToolResponseDto> ExecuteReadOnlyToolAsync(
        AIReadOnlyToolRequestDto request,
        CancellationToken cancellationToken = default) =>
        request.ToolName.ToLowerInvariant() switch
        {
            "getdeficiencies" => BuildToolResponse("GetDeficiencies", ExplainDeficiencyToolAsync(request, cancellationToken)),
            "getcapas" => BuildToolResponse("GetCapas", ExplainCapaToolAsync(request, cancellationToken)),
            "getslahealth" => BuildToolResponse("GetSlaHealth", ExplainSlaToolAsync(request, cancellationToken)),
            "getescalations" => BuildToolResponse("GetEscalations", ExplainEscalationToolAsync(request, cancellationToken)),
            "getstageunlockstatus" => BuildToolResponse("GetStageUnlockStatus", ExplainStageUnlockToolAsync(request, cancellationToken)),
            "getanalyticsoverview" => BuildToolResponse("GetAnalyticsOverview", ExplainAnalyticsOverviewToolAsync(request, cancellationToken)),
            "getdepartmentmetrics" => BuildToolResponse("GetDepartmentMetrics", ExplainDepartmentMetricsToolAsync(request, cancellationToken)),
            "getrecurringissues" => BuildToolResponse("GetRecurringIssues", ExplainRecurringIssuesToolAsync(request, cancellationToken)),
            "listoverduecapas" => BuildToolResponse("ListOverdueCapas", ExplainOverdueCapasToolAsync(request, cancellationToken)),
            "listoeevidencegaps" => BuildToolResponse("ListOeEvidenceGaps", ExplainOeEvidenceGapsToolAsync(request, cancellationToken)),
            "getdeptreadiness" => BuildToolResponse("GetDeptReadiness", ExplainDeptReadinessToolAsync(request, cancellationToken)),
            _ => Task.FromResult(new AIReadOnlyToolResponseDto
            {
                ToolName = request.ToolName,
                Success = false,
                Summary = $"Unknown read-only tool '{request.ToolName}'."
            })
        };

    private static async Task<AIReadOnlyToolResponseDto> BuildToolResponse(
        string toolName,
        Task<(string Summary, Dictionary<string, string> Facts)> task)
    {
        var (summary, facts) = await task;
        return new AIReadOnlyToolResponseDto
        {
            ToolName = toolName,
            Success = true,
            Summary = summary,
            Facts = facts
        };
    }

    private async Task<AIExplainContextResponseDto> ExplainStageUnlockAsync(
        AnalyticsScope scope,
        string topic,
        AIExplainContextRequestDto request,
        CancellationToken ct)
    {
        var evaluations = await _stageUnlocking.GetStageEvaluationsAsync(
            scope.HospitalId, scope.OrganizationId, scope.FrameworkId, ct);

        var stageName = request.EntityId ?? ExtractStageName(topic) ?? "Commitment";
        if (!Enum.TryParse<ObjectiveElementClassification>(stageName, true, out var stage))
        {
            stage = ObjectiveElementClassification.Commitment;
        }

        var eval = evaluations.FirstOrDefault(e => e.Stage == stage)
            ?? evaluations.FirstOrDefault(e => e.State == MaturityStageLifecycleState.Locked);

        if (eval == null)
        {
            return new AIExplainContextResponseDto
            {
                Topic = topic,
                Summary = $"No stage unlock evaluation is available for {stageName}."
            };
        }

        var locked = eval.State == MaturityStageLifecycleState.Locked;
        var summary = locked
            ? $"{eval.Stage} is locked at {eval.UnlockReadinessPercent:F0}% unlock readiness."
            : $"{eval.Stage} is {eval.State} with {eval.ProgressPercent:F0}% stage progress.";

        return new AIExplainContextResponseDto
        {
            Topic = topic,
            Summary = summary,
            KeyPoints = eval.Criteria.Select(c => $"{c.Label}: {c.Actual}/{c.Required} ({(c.Passed ? "met" : "not met")})").ToList(),
            Blockers = eval.BlockingReasons.ToList(),
            RecommendedActions = eval.Recommendations.ToList(),
            Facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["stage"] = eval.Stage.ToString(),
                ["state"] = eval.State.ToString(),
                ["unlockReadinessPercent"] = eval.UnlockReadinessPercent.ToString("F0"),
                ["progressPercent"] = eval.ProgressPercent.ToString("F0")
            }
        };
    }

    private async Task<AIExplainContextResponseDto> ExplainDeficiencyAsync(
        AnalyticsScope scope,
        string topic,
        AIExplainContextRequestDto request,
        CancellationToken ct)
    {
        if (Guid.TryParse(request.EntityId, out var id))
        {
            var d = await _db.ComplianceDeficiencies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
            if (d != null)
            {
                return new AIExplainContextResponseDto
                {
                    Topic = topic,
                    Summary = $"{d.DeficiencyCode} is {d.Severity} severity, status {d.Status}. {(d.ProblemStatement ?? d.Title).Trim()}",
                    KeyPoints =
                    [
                        $"Department: {d.Department ?? "Unassigned"}",
                        $"Root cause: {d.RootCauseCategory?.ToString() ?? "Not classified"}",
                        d.IsRecurringIssue ? "Flagged as recurring issue" : "Not flagged as recurring"
                    ],
                    Blockers = d.Status is not DeficiencyStatus.Closed and not DeficiencyStatus.Cancelled and not DeficiencyStatus.Verified
                        ? ["Remediation not yet closed or verified"]
                        : [],
                    RecommendedActions = ["Review CAPA plan", "Confirm evidence and verification path"],
                    Facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["code"] = d.DeficiencyCode ?? id.ToString(),
                        ["severity"] = d.Severity.ToString(),
                        ["status"] = d.Status.ToString()
                    }
                };
            }
        }

        var counts = await _deficiencyMetrics.GetCountsAsync(scope, ct);
        return new AIExplainContextResponseDto
        {
            Topic = topic,
            Summary = $"There are {counts.Open} open deficiencies ({counts.CriticalOpen} critical, {counts.RecurringOpen} recurring).",
            KeyPoints =
            [
                $"{counts.EscalatedOpen} escalated",
                $"{counts.BlockingOpen} blocking accreditation readiness"
            ],
            Facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["openCount"] = counts.Open.ToString(),
                ["criticalOpenCount"] = counts.CriticalOpen.ToString()
            }
        };
    }

    private async Task<AIExplainContextResponseDto> ExplainCapaAsync(
        string topic,
        AIExplainContextRequestDto request,
        CancellationToken ct)
    {
        if (!Guid.TryParse(request.EntityId, out var id))
        {
            return new AIExplainContextResponseDto { Topic = topic, Summary = "Specify a deficiency/CAPA id to explain a specific CAPA." };
        }

        var capa = await _capaQuery.GetCapaAsync(id, ct);
        if (capa == null)
        {
            return new AIExplainContextResponseDto { Topic = topic, Summary = "CAPA record not found." };
        }

        return new AIExplainContextResponseDto
        {
            Topic = topic,
            Summary = $"CAPA for {capa.DeficiencyCode}: corrective {capa.CorrectiveActionStatus}, preventive {capa.PreventiveActionStatus}, effectiveness {capa.EffectivenessReviewStatus}.",
            KeyPoints =
            [
                capa.CorrectiveActionOverdue ? "Corrective action is overdue" : "Corrective action on track",
                capa.IsRecurringIssue ? $"Recurring: {capa.RecurrenceReason}" : "Not recurring"
            ],
            RecommendedActions = capa.EffectivenessReviewStatus is "Pending" or "Scheduled"
                ? ["Schedule effectiveness review", "Confirm preventive action evidence"]
                : ["Monitor closure and verification"],
            Facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["correctiveStatus"] = capa.CorrectiveActionStatus ?? "",
                ["preventiveStatus"] = capa.PreventiveActionStatus ?? "",
                ["effectivenessStatus"] = capa.EffectivenessReviewStatus ?? ""
            }
        };
    }

    private async Task<AIExplainContextResponseDto> ExplainSlaAsync(AnalyticsScope scope, string topic, CancellationToken ct)
    {
        var sla = await _escalation.GetMetricsForScopeAsync(scope.OrganizationId, scope.HospitalId, scope.FrameworkId, ct);
        return new AIExplainContextResponseDto
        {
            Topic = topic,
            Summary = $"SLA health: {sla.OnTrackCount} on track, {sla.AtRiskCount} at risk, {sla.BreachedCount} breached.",
            KeyPoints =
            [
                $"{sla.EscalatedCount} escalated",
                $"{sla.CriticalEscalations} critical escalations"
            ],
            Blockers = sla.BreachedCount > 0 ? [$"{sla.BreachedCount} deficiencies have breached SLA targets"] : [],
            RecommendedActions = ["Prioritize breached and at-risk deficiencies", "Review department SLA compliance"],
            Facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["breachedCount"] = sla.BreachedCount.ToString(),
                ["atRiskCount"] = sla.AtRiskCount.ToString()
            }
        };
    }

    private async Task<AIExplainContextResponseDto> ExplainEscalationAsync(AnalyticsScope scope, string topic, CancellationToken ct)
    {
        var sla = await _escalation.GetMetricsForScopeAsync(scope.OrganizationId, scope.HospitalId, scope.FrameworkId, ct);
        return new AIExplainContextResponseDto
        {
            Topic = topic,
            Summary = $"{sla.EscalatedCount} open escalations ({sla.CriticalEscalations} critical). Escalation rate {sla.EscalationRate:P1}.",
            Blockers = sla.CriticalEscalations > 0 ? ["Critical escalations require leadership acknowledgment"] : [],
            RecommendedActions = ["Review escalation history on highest-severity deficiencies"],
            Facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["escalatedCount"] = sla.EscalatedCount.ToString(),
                ["criticalEscalations"] = sla.CriticalEscalations.ToString()
            }
        };
    }

    private async Task<AIExplainContextResponseDto> ExplainAnalyticsKpiAsync(
        AnalyticsScope scope,
        string topic,
        AIExplainContextRequestDto request,
        CancellationToken ct)
    {
        var overview = await _analytics.GetOverviewAsync(scope, ct);
        var kpiKey = request.EntityId ?? topic;
        var card = overview.KpiCards.FirstOrDefault(k =>
            k.Key.Equals(kpiKey, StringComparison.OrdinalIgnoreCase)
            || k.Label.Contains(kpiKey, StringComparison.OrdinalIgnoreCase));

        if (card == null)
        {
            return new AIExplainContextResponseDto
            {
                Topic = topic,
                Summary = $"Analytics overview: readiness {overview.ReadinessScore:F1}%, CMI {overview.ComplianceMaturityIndex:F1}, stage {overview.CurrentStage}.",
                Facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["readinessScore"] = overview.ReadinessScore.ToString("F1"),
                    ["openDeficiencies"] = overview.OpenDeficiencies.ToString()
                }
            };
        }

        return new AIExplainContextResponseDto
        {
            Topic = topic,
            Summary = $"{card.Label} is {card.Value}{card.Unit}. Source: {card.Source ?? "analytics"}.",
            KeyPoints = [$"Metric key: {card.Key}"],
            Facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["key"] = card.Key,
                ["value"] = card.Value?.ToString() ?? "",
                ["label"] = card.Label
            }
        };
    }

    private async Task<AIExplainContextResponseDto> ExplainGeneralAsync(AnalyticsScope scope, string topic, CancellationToken ct)
    {
        var overview = await _analytics.GetOverviewAsync(scope, ct);
        return new AIExplainContextResponseDto
        {
            Topic = topic,
            Summary = $"Hospital readiness {overview.ReadinessScore:F1}% at stage {overview.CurrentStage} with {overview.OpenDeficiencies} open deficiencies.",
            Facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["readinessScore"] = overview.ReadinessScore.ToString("F1"),
                ["currentStage"] = overview.CurrentStage
            }
        };
    }

    private async Task<(string, Dictionary<string, string>)> ExplainDeficiencyToolAsync(AIReadOnlyToolRequestDto req, CancellationToken ct)
    {
        var scope = await _analytics.ResolveScopeAsync(req.OrganizationId, req.HospitalId, req.FrameworkId, ct);
        var explain = await ExplainDeficiencyAsync(scope, "deficiencies", new AIExplainContextRequestDto
        {
            HospitalId = req.HospitalId,
            OrganizationId = req.OrganizationId,
            FrameworkId = req.FrameworkId,
            Topic = "deficiencies"
        }, ct);
        return (explain.Summary, explain.Facts);
    }

    private async Task<(string, Dictionary<string, string>)> ExplainCapaToolAsync(AIReadOnlyToolRequestDto req, CancellationToken ct)
    {
        var scope = await _analytics.ResolveScopeAsync(req.OrganizationId, req.HospitalId, req.FrameworkId, ct);
        var metrics = await _deficiencyMetrics.GetCapaMetricsAsync(scope, ct);
        var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["openCapas"] = metrics.OpenCapas.ToString(),
            ["overdueCapas"] = metrics.OverdueCapas.ToString(),
            ["effectivenessRate"] = (metrics.CapaEffectivenessRate * 100).ToString("F1")
        };
        return ($"{metrics.OpenCapas} open CAPAs ({metrics.OverdueCapas} overdue), effectiveness {metrics.CapaEffectivenessRate:P0}.", facts);
    }

    private async Task<(string, Dictionary<string, string>)> ExplainSlaToolAsync(AIReadOnlyToolRequestDto req, CancellationToken ct)
    {
        var explain = await ExplainSlaAsync(
            await _analytics.ResolveScopeAsync(req.OrganizationId, req.HospitalId, req.FrameworkId, ct),
            "sla health", ct);
        return (explain.Summary, explain.Facts);
    }

    private async Task<(string, Dictionary<string, string>)> ExplainEscalationToolAsync(AIReadOnlyToolRequestDto req, CancellationToken ct)
    {
        var explain = await ExplainEscalationAsync(
            await _analytics.ResolveScopeAsync(req.OrganizationId, req.HospitalId, req.FrameworkId, ct),
            "escalations", ct);
        return (explain.Summary, explain.Facts);
    }

    private async Task<(string, Dictionary<string, string>)> ExplainStageUnlockToolAsync(AIReadOnlyToolRequestDto req, CancellationToken ct)
    {
        var stage = req.Parameters.GetValueOrDefault("stage") ?? "Commitment";
        var explain = await ExplainStageUnlockAsync(
            await _analytics.ResolveScopeAsync(req.OrganizationId, req.HospitalId, req.FrameworkId, ct),
            $"stage unlock {stage}",
            new AIExplainContextRequestDto
            {
                HospitalId = req.HospitalId,
                OrganizationId = req.OrganizationId,
                FrameworkId = req.FrameworkId,
                Topic = stage,
                EntityId = stage
            }, ct);
        return (explain.Summary, explain.Facts);
    }

    private async Task<(string, Dictionary<string, string>)> ExplainAnalyticsOverviewToolAsync(AIReadOnlyToolRequestDto req, CancellationToken ct)
    {
        var scope = await _analytics.ResolveScopeAsync(req.OrganizationId, req.HospitalId, req.FrameworkId, ct);
        var explain = await ExplainGeneralAsync(scope, "analytics overview", ct);
        return (explain.Summary, explain.Facts);
    }

    private async Task<(string, Dictionary<string, string>)> ExplainDepartmentMetricsToolAsync(AIReadOnlyToolRequestDto req, CancellationToken ct)
    {
        var scope = await _analytics.ResolveScopeAsync(req.OrganizationId, req.HospitalId, req.FrameworkId, ct);
        var executive = await _analytics.GetExecutiveAsync(scope, ct);
        var summary = executive.DepartmentPerformance.Count > 0
            ? string.Join("; ", executive.DepartmentPerformance.Take(5).Select(d => $"{d.Label}: {d.Count}"))
            : "No department performance data available.";
        return (summary, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["departmentCount"] = executive.DepartmentPerformance.Count.ToString()
        });
    }

    private async Task<(string, Dictionary<string, string>)> ExplainRecurringIssuesToolAsync(AIReadOnlyToolRequestDto req, CancellationToken ct)
    {
        var scope = await _analytics.ResolveScopeAsync(req.OrganizationId, req.HospitalId, req.FrameworkId, ct);
        var counts = await _deficiencyMetrics.GetCountsAsync(scope, ct);
        var capa = await _deficiencyMetrics.GetCapaMetricsAsync(scope, ct);
        return ($"{counts.RecurringOpen} recurring open deficiencies; {capa.RecurringIssues} recurring CAPA issues.",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["recurringOpenDeficiencies"] = counts.RecurringOpen.ToString(),
                ["recurringCapaIssues"] = capa.RecurringIssues.ToString()
            });
    }

    private async Task<(string, Dictionary<string, string>)> ExplainOverdueCapasToolAsync(AIReadOnlyToolRequestDto req, CancellationToken ct)
    {
        var scope = await _analytics.ResolveScopeAsync(req.OrganizationId, req.HospitalId, req.FrameworkId, ct);
        var capa = await _deficiencyMetrics.GetCapaMetricsAsync(scope, ct);
        var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["overdueCapas"] = capa.OverdueCapas.ToString(),
            ["openCapas"] = capa.OpenCapas.ToString(),
            ["effectivenessRate"] = (capa.CapaEffectivenessRate * 100).ToString("F1")
        };
        return (
            $"{capa.OverdueCapas} overdue CAPA(s) out of {capa.OpenCapas} open. Review CAPA aging and owners before audit.",
            facts);
    }

    private async Task<(string, Dictionary<string, string>)> ExplainOeEvidenceGapsToolAsync(AIReadOnlyToolRequestDto req, CancellationToken ct)
    {
        var scope = await _analytics.ResolveScopeAsync(req.OrganizationId, req.HospitalId, req.FrameworkId, ct);
        var overview = await _analytics.GetOverviewAsync(scope, ct);
        var executive = await _analytics.GetExecutiveAsync(scope, ct);
        var gapCount = executive.RiskExposure.CriticalOpenDeficiencies + executive.RiskExposure.HighRiskDeficiencies;
        var evidenceGapHint = executive.DepartmentPerformance.Count > 0
            ? string.Join("; ", executive.DepartmentPerformance.Take(5).Select(d => $"{d.Label}:{d.Count}"))
            : "No department-level evidence gap breakdown available.";
        var facts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["riskHotspotCount"] = gapCount.ToString(),
            ["departmentGapHints"] = evidenceGapHint,
            ["readinessScore"] = overview.ReadinessScore.ToString("F1"),
            ["openDeficiencies"] = overview.OpenDeficiencies.ToString()
        };
        return (
            $"OE evidence gaps inferred from readiness {overview.ReadinessScore:F1}% with {gapCount} elevated risk item(s) and {overview.OpenDeficiencies} open deficiencies. Focus on OEs without approved evidence.",
            facts);
    }

    private async Task<(string, Dictionary<string, string>)> ExplainDeptReadinessToolAsync(AIReadOnlyToolRequestDto req, CancellationToken ct)
    {
        var scope = await _analytics.ResolveScopeAsync(req.OrganizationId, req.HospitalId, req.FrameworkId, ct);
        var overview = await _analytics.GetOverviewAsync(scope, ct);
        var executive = await _analytics.GetExecutiveAsync(scope, ct);
        var dept = req.Parameters.GetValueOrDefault("department")
                   ?? req.Parameters.GetValueOrDefault("Department");
        IReadOnlyList<AnalyticsBreakdownItemDto> rows = executive.DepartmentPerformance;
        if (!string.IsNullOrWhiteSpace(dept))
        {
            rows = rows
                .Where(d => d.Label.Contains(dept, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var top = rows.Take(8).Select(d => $"{d.Label}:{d.Count}").ToList();
        var summary = top.Count > 0
            ? $"Department readiness signals — {string.Join("; ", top)}. Overall readiness {overview.ReadinessScore:F1}% (accreditation {executive.AccreditationReadiness.Score:F1}%)."
            : $"No department readiness rows available. Overall readiness {overview.ReadinessScore:F1}%.";
        return (summary, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["readinessScore"] = overview.ReadinessScore.ToString("F1"),
            ["accreditationReadiness"] = executive.AccreditationReadiness.Score.ToString("F1"),
            ["departmentCount"] = executive.DepartmentPerformance.Count.ToString(),
            ["filteredCount"] = rows.Count.ToString()
        });
    }

    private static string InferEntityType(string topic)
    {
        var t = topic.ToLowerInvariant();
        if (t.Contains("commitment") || t.Contains("locked") || t.Contains("unlock") || t.Contains("stage"))
        {
            return "StageUnlock";
        }

        if (t.Contains("capa")) return "Capa";
        if (t.Contains("deficiency") || t.Contains("nc")) return "Deficiency";
        if (t.Contains("sla") || t.Contains("breach")) return "Sla";
        if (t.Contains("escalat")) return "Escalation";
        if (t.Contains("kpi") || t.Contains("analytics")) return "Analytics";
        return "General";
    }

    private static string? ExtractStageName(string topic)
    {
        foreach (var stage in new[] { "Commitment", "Achievement", "Excellence", "Core" })
        {
            if (topic.Contains(stage, StringComparison.OrdinalIgnoreCase))
            {
                return stage;
            }
        }

        return null;
    }
}
