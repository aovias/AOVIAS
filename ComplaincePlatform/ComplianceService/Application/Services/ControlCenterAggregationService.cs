using System.Security.Cryptography;
using System.Text;
using ComplianceService.Application.ComplianceScoring;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs.ControlCenter;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Services;

/// <summary>
/// Control Center intelligence engine: risk scoring, audit-confidence and insight generation, plus
/// idempotent materialization of derived signals (scheduled audits, activity events, alerts).
/// Compliance percentages always come from <see cref="IComplianceScoreService"/> (single source of truth).
/// </summary>
public sealed class ControlCenterAggregationService : IControlCenterAggregationService
{
    private const decimal TargetCompliance = 80m;
    private const decimal AuditThreshold = 80m;

    private readonly IComplianceScoreService _score;
    private readonly IControlCenterRepository _repository;
    private readonly ILogger<ControlCenterAggregationService> _logger;

    public ControlCenterAggregationService(
        IComplianceScoreService score,
        IControlCenterRepository repository,
        ILogger<ControlCenterAggregationService> logger)
    {
        _score = score;
        _repository = repository;
        _logger = logger;
    }

    // ---------------------------------------------------------------------
    // Population (writes) - derives live signals from real data, idempotently.
    // ---------------------------------------------------------------------
    public async Task PopulateSignalsAsync(ControlCenterScope scope, ControlCenterDataset dataset, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Seed canonical + observed departments.
            var departmentNames = new List<string> { "Security", "Administration", "Clinical", "HR" };
            departmentNames.AddRange(dataset.Tasks
                .Where(t => !string.IsNullOrWhiteSpace(t.DepartmentName))
                .Select(t => t.DepartmentName!.Trim()));
            await _repository.SeedDepartmentsAsync(scope, departmentNames, cancellationToken);
            dataset.Departments = dataset.Departments.Count > 0
                ? dataset.Departments
                : departmentNames.Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(n => new ComplianceDepartment { OrganizationId = scope.OrganizationId, Name = n, Code = n })
                    .ToList();

            // 2. Scheduled audits from journeys that have an AuditDate.
            var audits = BuildScheduledAudits(scope, dataset);
            await _repository.UpsertScheduledAuditsAsync(audits, cancellationToken);
            dataset.ScheduledAudits = audits;

            // 3. Activity events projected from recent real entity changes.
            var events = BuildActivityEvents(scope, dataset);
            await _repository.UpsertActivityEventsAsync(events, cancellationToken);
            dataset.Activities = events
                .Concat(dataset.Activities)
                .GroupBy(e => e.Id)
                .Select(g => g.First())
                .OrderByDescending(e => e.OccurredAt)
                .Take(60)
                .ToList();

            // 4. Live alerts from current posture.
            var alerts = BuildAlerts(scope, dataset);
            await _repository.UpsertAlertsAsync(alerts, cancellationToken);
            dataset.Alerts = alerts;
        }
        catch (Exception ex)
        {
            // Population must never break the read path.
            _logger.LogWarning(ex, "Control Center signal population failed; continuing with available data.");
        }
    }

    private static List<ScheduledAudit> BuildScheduledAudits(ControlCenterScope scope, ControlCenterDataset dataset)
    {
        var now = DateTime.UtcNow;
        var list = new List<ScheduledAudit>();
        foreach (var journey in dataset.Journeys.Where(j => j.AuditDate.HasValue))
        {
            var name = journey.Framework?.Name ?? "Framework";
            var auditDate = journey.AuditDate!.Value;
            list.Add(new ScheduledAudit
            {
                Id = DeterministicGuid($"audit:{journey.Id}"),
                TenantId = scope.TenantId,
                OrganizationId = scope.OrganizationId,
                HospitalId = journey.HospitalId,
                FrameworkId = journey.FrameworkId,
                ComplianceJourneyId = journey.Id,
                Title = $"{name} audit",
                ScheduledDate = auditDate,
                Status = auditDate.Date >= now.Date ? ScheduledAuditStatus.Planned : ScheduledAuditStatus.Completed,
                ProgramYear = auditDate.Year,
                ProgramMonth = auditDate.Month,
                CreatedAt = now
            });
        }

        return list;
    }

    private static List<ActivityEvent> BuildActivityEvents(ControlCenterScope scope, ControlCenterDataset dataset)
    {
        var events = new List<ActivityEvent>();

        foreach (var e in dataset.RecentEvidence.Take(10))
        {
            events.Add(NewEvent(scope, ActivityEventType.EvidenceUploaded, $"evidence:{e.Id}",
                "Evidence Uploaded",
                string.IsNullOrWhiteSpace(e.Title) ? "Evidence uploaded" : e.Title!,
                e.UploadedBy, e.Department, e.HospitalId, e.CreatedAt == default ? e.UploadedOn : e.CreatedAt));
        }

        foreach (var t in dataset.Tasks.Where(x => x.Status == ComplianceTaskStatus.Completed).Take(10))
        {
            events.Add(NewEvent(scope, ActivityEventType.TaskCompleted, $"task:{t.Id}",
                "Task Completed",
                string.IsNullOrWhiteSpace(t.Title) ? "Compliance task completed" : t.Title!,
                t.AssignedToUserId, t.DepartmentName, t.HospitalId, t.CreatedAt));
        }

        foreach (var f in dataset.OpenFindings.Take(10))
        {
            events.Add(NewEvent(scope, ActivityEventType.FindingRaised, $"finding:{f.Id}",
                "Finding Raised",
                string.IsNullOrWhiteSpace(f.Title) ? f.Description : f.Title,
                f.CreatedByType.ToString(), f.Department.ToString(), f.ComplianceJourney?.HospitalId, f.DateRaised));
        }

        foreach (var d in dataset.ActiveDeficiencies
                     .Where(x => x.CorrectiveActionStatus == CapaActionStatus.Completed
                                 && x.CorrectiveActionCompletedDate.HasValue)
                     .Take(10))
        {
            events.Add(NewEvent(scope, ActivityEventType.CapaApproved, $"capa:{d.Id}",
                "CAPA Corrective Action Completed",
                string.IsNullOrWhiteSpace(d.Title) ? d.DeficiencyCode : d.Title,
                d.CorrectiveActionOwner ?? d.AssignedTo, d.Department, d.HospitalId,
                d.CorrectiveActionCompletedDate!.Value));
        }

        foreach (var a in dataset.ScheduledAudits.Take(10))
        {
            events.Add(NewEvent(scope, ActivityEventType.AuditScheduled, $"auditsched:{a.Id}",
                "Audit Scheduled",
                a.Title, "System", null, a.HospitalId, a.CreatedAt == default ? a.ScheduledDate : a.CreatedAt));
        }

        return events;
    }

    private static ActivityEvent NewEvent(
        ControlCenterScope scope, ActivityEventType type, string sourceKey,
        string title, string description, string? actor, string? department, Guid? hospitalId, DateTime occurredAt)
        => new()
        {
            Id = DeterministicGuid($"evt:{sourceKey}"),
            TenantId = scope.TenantId,
            OrganizationId = scope.OrganizationId,
            HospitalId = hospitalId ?? scope.HospitalId,
            Type = type,
            Title = title,
            Description = Truncate(description, 2000),
            ActorName = string.IsNullOrWhiteSpace(actor) ? "System" : Truncate(actor!, 200),
            DepartmentName = string.IsNullOrWhiteSpace(department) ? null : Truncate(department!, 200),
            OccurredAt = occurredAt == default ? DateTime.UtcNow : occurredAt,
            CreatedAt = DateTime.UtcNow
        };

    private List<Alert> BuildAlerts(ControlCenterScope scope, ControlCenterDataset dataset)
    {
        var now = DateTime.UtcNow;
        var alerts = new List<Alert>();

        var criticalOverdue = dataset.Tasks.Count(t => IsCritical(t) && IsOverdue(t, now));
        if (criticalOverdue > 0)
        {
            alerts.Add(NewAlert(scope, AlertSeverity.Critical, AlertType.CriticalTasks, "critical-tasks-overdue",
                "Critical Tasks Overdue",
                $"{criticalOverdue} critical task(s) are past their due date and require immediate attention.",
                $"{criticalOverdue} tasks"));
        }

        var missingClauses = Math.Max(0, dataset.ScorableObjectiveElements - dataset.ObjectiveElementsWithVerifiedEvidence);
        if (missingClauses > 0)
        {
            alerts.Add(NewAlert(scope, AlertSeverity.High, AlertType.MissingEvidence, "missing-evidence",
                "Evidence Missing",
                $"{missingClauses} clause(s) are missing required evidence documentation.",
                $"{missingClauses} clause"));
        }

        var overdueCapa = dataset.ActiveDeficiencies.Count(d => IsOverdueDeficiencyCapa(d, now));
        if (overdueCapa > 0)
        {
            alerts.Add(NewAlert(scope, AlertSeverity.High, AlertType.OverdueCapa, "overdue-capa",
                "Corrective Actions Overdue",
                $"{overdueCapa} corrective action(s) are past their due date.",
                $"{overdueCapa} CAPA"));
        }

        // Compliance drop per framework from the snapshot history.
        foreach (var drop in ComputeFrameworkDrops(dataset))
        {
            var severity = drop.DropPercent >= 10 ? AlertSeverity.Critical : AlertSeverity.High;
            alerts.Add(NewAlert(scope, severity, AlertType.ComplianceDrop, $"compliance-drop:{drop.FrameworkId}",
                $"{drop.Code} Compliance Dropped",
                $"{drop.Code} compliance dropped to {drop.Latest:0}%, down from {drop.Previous:0}%.",
                $"-{drop.DropPercent:0}%"));
        }

        var nextAudit = dataset.ScheduledAudits
            .Where(a => a.ScheduledDate.Date >= now.Date && a.ScheduledDate.Date <= now.AddDays(14).Date)
            .OrderBy(a => a.ScheduledDate)
            .FirstOrDefault();
        if (nextAudit != null)
        {
            alerts.Add(NewAlert(scope, AlertSeverity.Low, AlertType.AuditRisk, "upcoming-audit",
                "Upcoming Audit",
                $"{nextAudit.Title} is scheduled for {nextAudit.ScheduledDate:dd MMM yyyy}.",
                null));
        }

        return alerts;
    }

    private Alert NewAlert(
        ControlCenterScope scope, AlertSeverity severity, AlertType type, string dedupe,
        string title, string description, string? metric)
        => new()
        {
            Id = DeterministicGuid($"alert:{scope.TenantId}:{scope.HospitalId}:{dedupe}"),
            TenantId = scope.TenantId,
            OrganizationId = scope.OrganizationId,
            HospitalId = scope.HospitalId,
            FrameworkId = scope.FrameworkId,
            Severity = severity,
            Type = type,
            Title = title,
            Description = Truncate(description, 2000),
            Metric = metric,
            DedupeKey = dedupe,
            Status = AlertStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

    // ---------------------------------------------------------------------
    // Overview build (reads / pure computation).
    // ---------------------------------------------------------------------
    public async Task<ControlCenterOverviewDto> BuildOverviewAsync(ControlCenterScope scope, ControlCenterDataset dataset, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Compliance scores from the single source of truth.
        var chapterScoreById = new Dictionary<Guid, (decimal Sum, int Count)>();
        var overallScores = new List<decimal>();
        foreach (var hid in scope.HospitalIds)
        {
            try
            {
                var result = scope.FrameworkId.HasValue
                    ? await _score.GetFrameworkScoreAsync(hid, scope.FrameworkId.Value, cancellationToken)
                    : await _score.GetHospitalScoreAsync(hid, cancellationToken);
                overallScores.Add(result.OverallScore);
                foreach (var cs in result.ChapterScores)
                {
                    var entry = chapterScoreById.TryGetValue(cs.ChapterId, out var v) ? v : (0m, 0);
                    chapterScoreById[cs.ChapterId] = (entry.Item1 + cs.WeightedScore, entry.Item2 + 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Compliance score lookup failed for hospital {HospitalId}.", hid);
            }
        }

        var complianceReadiness = overallScores.Count > 0 ? Round2(overallScores.Average()) : 0m;
        var chapterCompliance = BuildChapterCompliance(dataset, chapterScoreById);

        var openFindings = dataset.OpenFindings.Count;
        var criticalFindings = dataset.OpenFindings.Count(f => f.RiskLevel == FindingRiskLevel.Critical);
        var criticalTasks = dataset.Tasks.Count(t => IsCritical(t) && t.Status != ComplianceTaskStatus.Completed);
        var overdueCapa = dataset.ActiveDeficiencies.Count(d => IsOverdueDeficiencyCapa(d, now));
        var capaPending = dataset.ActiveDeficiencies.Count(IsPendingDeficiencyCapa);
        var missingClauses = Math.Max(0, dataset.ScorableObjectiveElements - dataset.ObjectiveElementsWithVerifiedEvidence);

        var coverage = BuildEvidenceCoverage(dataset);
        var auditReadiness = BuildAuditReadiness(complianceReadiness, criticalFindings, openFindings, overdueCapa, missingClauses, capaPending, coverage.CoveragePercent);
        var upcomingAudits = dataset.ScheduledAudits.Count(a => a.ScheduledDate.Date >= now.Date &&
                                                                a.Status is ScheduledAuditStatus.Planned or ScheduledAuditStatus.InProgress);

        var trend = BuildTrend(dataset);
        var deltas = ComputeTrendDeltas(dataset);

        var kpis = new ComplianceCommandCenterKpiDto
        {
            ComplianceReadiness = complianceReadiness,
            AuditConfidence = auditReadiness.ConfidenceScore,
            OpenFindings = openFindings,
            CriticalTasks = criticalTasks,
            EvidenceCoverage = coverage.CoveragePercent,
            UpcomingAudits = upcomingAudits,
            ComplianceReadinessTrend = deltas.ComplianceDelta,
            AuditConfidenceTrend = deltas.ConfidenceDelta,
            OpenFindingsTrend = 0,
            CriticalTasksTrend = 0,
            EvidenceCoverageTrend = 0,
            UpcomingAuditsTrend = 0,
            ComplianceReadinessSparkline = deltas.ComplianceSparkline,
            AuditConfidenceSparkline = deltas.ConfidenceSparkline,
            EvidenceCoverageSparkline = new List<decimal>()
        };

        var highRisk = BuildHighRiskChapters(dataset, chapterScoreById, now);
        var departmentRisk = BuildDepartmentRisk(dataset, now);
        var actionCenter = BuildActionCenter(dataset, now);
        var intelligence = BuildIntelligence(dataset, deltas, criticalTasks, missingClauses, capaPending, now);
        var recommended = BuildRecommendedActions(dataset, missingClauses, criticalTasks, capaPending, now);
        var activity = BuildActivityDtos(dataset);
        var alertDtos = BuildAlertDtos(dataset);

        return new ControlCenterOverviewDto
        {
            Header = new ComplianceHeaderDto
            {
                IsLive = true,
                LastUpdated = now,
                OrganizationId = scope.OrganizationId.ToString(),
                OrganizationName = dataset.OrganizationName
            },
            Intelligence = new ComplianceIntelligenceDto { Insights = intelligence },
            RecommendedActions = recommended,
            Kpis = kpis,
            HighRiskChapters = highRisk,
            DepartmentRisks = departmentRisk,
            ActionCenter = actionCenter,
            Trend = trend,
            ChapterCompliance = chapterCompliance,
            EvidenceCoverage = coverage,
            AuditReadiness = auditReadiness,
            RecentActivity = activity,
            Alerts = alertDtos
        };
    }

    // ---------------------------------------------------------------------
    // Builders
    // ---------------------------------------------------------------------
    private List<ChapterComplianceDto> BuildChapterCompliance(ControlCenterDataset dataset, Dictionary<Guid, (decimal Sum, int Count)> chapterScores)
    {
        var chapterById = dataset.Chapters.ToDictionary(c => c.Id, c => c);
        return chapterScores
            .Select(kv =>
            {
                chapterById.TryGetValue(kv.Key, out var chapter);
                var label = chapter != null
                    ? (!string.IsNullOrWhiteSpace(chapter.Code) ? chapter.Code : chapter.Name)
                    : kv.Key.ToString()[..8];
                return new ChapterComplianceDto
                {
                    Chapter = label ?? string.Empty,
                    Compliance = kv.Value.Count > 0 ? Round2(kv.Value.Sum / kv.Value.Count) : 0m
                };
            })
            .OrderBy(c => c.Compliance)
            .ToList();
    }

    private List<HighRiskChapterDto> BuildHighRiskChapters(ControlCenterDataset dataset, Dictionary<Guid, (decimal Sum, int Count)> chapterScores, DateTime now)
    {
        var chapterById = dataset.Chapters.ToDictionary(c => c.Id, c => c);

        // Per-chapter operational signals mapped from tasks/findings.
        var overdueByChapter = dataset.Tasks
            .Where(t => IsOverdue(t, now) && t.ComplianceStandard?.Chapter != null)
            .GroupBy(t => t.ComplianceStandard!.Chapter!.Id)
            .ToDictionary(g => g.Key, g => g.Count());

        var findingsByChapter = dataset.OpenFindings
            .Where(f => f.TaskId.HasValue)
            .Select(f => dataset.Tasks.FirstOrDefault(t => t.Id == f.TaskId!.Value))
            .Where(t => t?.ComplianceStandard?.Chapter != null)
            .GroupBy(t => t!.ComplianceStandard!.Chapter!.Id)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<HighRiskChapterDto>();
        foreach (var kv in chapterScores)
        {
            chapterById.TryGetValue(kv.Key, out var chapter);
            var compliancePct = kv.Value.Count > 0 ? kv.Value.Sum / kv.Value.Count : 0m;
            var openFindings = findingsByChapter.GetValueOrDefault(kv.Key, 0);
            var overdueTasks = overdueByChapter.GetValueOrDefault(kv.Key, 0);

            var risk = ControlCenterFormulas.ChapterRiskScore(compliancePct, openFindings, overdueTasks, missingEvidence: 0, auditFailures: 0);
            var openIssues = openFindings + overdueTasks;

            var label = chapter != null
                ? (!string.IsNullOrWhiteSpace(chapter.Code)
                    ? (string.IsNullOrWhiteSpace(chapter.Name) ? chapter.Code : $"{chapter.Code} - {chapter.Name}")
                    : chapter.Name)
                : kv.Key.ToString()[..8];

            result.Add(new HighRiskChapterDto
            {
                Chapter = label ?? string.Empty,
                ComplianceScore = Round2(compliancePct),
                RiskLevel = ControlCenterFormulas.ChapterRiskBandToUi(risk),
                OpenIssues = openIssues
            });
        }

        return result
            .OrderByDescending(c => c.OpenIssues)
            .ThenBy(c => c.ComplianceScore)
            .Take(8)
            .ToList();
    }

    private List<DepartmentRiskDto> BuildDepartmentRisk(ControlCenterDataset dataset, DateTime now)
    {
        var names = dataset.Departments.Select(d => d.Name)
            .Concat(new[] { "Security", "Administration", "Clinical", "HR" })
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var result = new List<DepartmentRiskDto>();
        foreach (var name in names)
        {
            var deptTasks = dataset.Tasks.Where(t => string.Equals(t.DepartmentName, name, StringComparison.OrdinalIgnoreCase)).ToList();
            var openTasks = deptTasks.Count(t => t.Status != ComplianceTaskStatus.Completed);
            var overdue = deptTasks.Count(t => IsOverdue(t, now));
            var findings = dataset.OpenFindings.Count(f => string.Equals(f.Department.ToString(), name, StringComparison.OrdinalIgnoreCase));

            var overdueRatio = openTasks > 0 ? (decimal)overdue / openTasks : 0m;
            var risk = Math.Clamp(overdueRatio * 60m + Math.Min(40m, findings * 8m), 0m, 100m);

            // Skip departments with no signal at all to avoid noise.
            if (deptTasks.Count == 0 && findings == 0)
            {
                continue;
            }

            result.Add(new DepartmentRiskDto
            {
                Department = name,
                RiskScore = Round2(risk),
                RiskLevel = ControlCenterFormulas.DepartmentRiskBandToUi(risk)
            });
        }

        return result.OrderByDescending(d => d.RiskScore).Take(8).ToList();
    }

    private List<ActionCenterItemDto> BuildActionCenter(ControlCenterDataset dataset, DateTime now)
    {
        return dataset.Tasks
            .Where(t => t.Status != ComplianceTaskStatus.Completed)
            .OrderByDescending(t => IsCritical(t))
            .ThenBy(t => t.DueDate ?? DateTime.MaxValue)
            .Take(10)
            .Select(t => new ActionCenterItemDto
            {
                Id = t.Id.ToString(),
                Title = string.IsNullOrWhiteSpace(t.Title) ? "Compliance task" : t.Title!,
                ComplianceImpact = BuildImpactLabel(t),
                Owner = string.IsNullOrWhiteSpace(t.AssignedToUserId) ? "Unassigned" : t.AssignedToUserId,
                Department = string.IsNullOrWhiteSpace(t.DepartmentName) ? "General" : t.DepartmentName!,
                DueDate = t.DueDate,
                Priority = TaskPriorityToUi(t),
                Status = TaskStatusToUi(t, now)
            })
            .ToList();
    }

    private List<ComplianceInsightDto> BuildIntelligence(ControlCenterDataset dataset, TrendDeltas deltas, int criticalTasks, int missingClauses, int capaPending, DateTime now)
    {
        var insights = new List<ComplianceInsightDto>();

        foreach (var drop in ComputeFrameworkDrops(dataset))
        {
            insights.Add(new ComplianceInsightDto
            {
                Type = "ComplianceDrop",
                Severity = drop.DropPercent >= 10 ? "critical" : "warning",
                Message = $"{drop.Code} compliance dropped to {drop.Latest:0}%",
                Metric = $"-{drop.DropPercent:0}%"
            });
        }

        if (criticalTasks > 0)
        {
            insights.Add(new ComplianceInsightDto
            {
                Type = "CriticalTasks",
                Severity = "warning",
                Message = $"{criticalTasks} critical task(s) need attention",
                Metric = "Immediate action required"
            });
        }

        if (missingClauses > 0)
        {
            insights.Add(new ComplianceInsightDto
            {
                Type = "MissingEvidence",
                Severity = "warning",
                Message = "Evidence missing for compliance clauses",
                Metric = $"{missingClauses} clause"
            });
        }

        if (deltas.ComplianceDelta > 0)
        {
            insights.Add(new ComplianceInsightDto
            {
                Type = "ComplianceImprovement",
                Severity = "success",
                Message = $"Compliance improved {deltas.ComplianceDelta:0}% recently",
                Metric = $"+{deltas.ComplianceDelta:0}%"
            });
        }

        if (insights.Count == 0)
        {
            insights.Add(new ComplianceInsightDto
            {
                Type = "AuditRisk",
                Severity = "info",
                Message = "Compliance posture is stable",
                Metric = null
            });
        }

        return insights;
    }

    private List<RecommendedActionDto> BuildRecommendedActions(ControlCenterDataset dataset, int missingClauses, int criticalTasks, int capaPending, DateTime now)
    {
        var actions = new List<RecommendedActionDto>();

        if (missingClauses > 0)
        {
            actions.Add(new RecommendedActionDto
            {
                Id = "upload-evidence",
                Title = "Upload missing evidence",
                Description = $"Will improve compliance by closing {missingClauses} clause(s)",
                Priority = "high",
                ImpactPercent = 3,
                ActionType = "UploadEvidence"
            });
        }

        var topCriticalTask = dataset.Tasks
            .Where(t => IsCritical(t) && IsOverdue(t, now))
            .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
            .FirstOrDefault();
        if (topCriticalTask != null)
        {
            actions.Add(new RecommendedActionDto
            {
                Id = topCriticalTask.Id.ToString(),
                Title = $"Complete {(string.IsNullOrWhiteSpace(topCriticalTask.Title) ? "critical task" : topCriticalTask.Title)}",
                Description = "Critical for compliance readiness",
                Priority = "high",
                ImpactPercent = 4,
                ActionType = "CompleteTask",
                EntityId = topCriticalTask.Id.ToString()
            });
        }

        if (capaPending > 0)
        {
            actions.Add(new RecommendedActionDto
            {
                Id = "review-capa",
                Title = "Review pending corrective actions",
                Description = $"{capaPending} action(s) awaiting approval",
                Priority = "medium",
                ImpactPercent = 2,
                ActionType = "ApproveCAPA"
            });
        }

        return actions;
    }

    private EvidenceCoverageDto BuildEvidenceCoverage(ControlCenterDataset dataset)
    {
        var total = dataset.EvidenceApproved + dataset.EvidencePending + dataset.EvidenceRejected;
        decimal coverage = dataset.ScorableObjectiveElements > 0
            ? Round2(100m * dataset.ObjectiveElementsWithVerifiedEvidence / dataset.ScorableObjectiveElements)
            : (total > 0 ? Round2(100m * dataset.EvidenceApproved / total) : 0m);

        return new EvidenceCoverageDto
        {
            Approved = dataset.EvidenceApproved,
            Pending = dataset.EvidencePending,
            Rejected = dataset.EvidenceRejected,
            Total = total,
            CoveragePercent = coverage
        };
    }

    /// <summary>
    /// Audit confidence (0-100): clamp(100 - 6*criticalNC - 3*openFindings - 4*overdueCapa
    /// - 1.5*missingEvidenceClauses + 0.3*evidenceCoverage% + 0.4*avgChapterReadiness%).
    /// </summary>
    private AuditReadinessDto BuildAuditReadiness(
        decimal avgChapterReadiness, int criticalNc, int openFindings, int overdueCapa, int missingClauses, int capaPending, decimal evidenceCoverage)
    {
        var confidence = ControlCenterFormulas.AuditConfidence(
            avgChapterReadiness, criticalNc, openFindings, overdueCapa, missingClauses, evidenceCoverage);

        var probability = ControlCenterFormulas.AuditPassProbability(confidence, AuditThreshold);

        var status = ControlCenterFormulas.ConfidenceStatus(confidence);

        var recommendation = confidence >= AuditThreshold
            ? "Maintain current controls; schedule the audit with confidence."
            : $"Close {Math.Max(criticalNc + overdueCapa, 1)} high-impact gap(s) before scheduling the audit.";

        return new AuditReadinessDto
        {
            ConfidenceScore = Round2(confidence),
            Status = status,
            CriticalFindings = criticalNc,
            CorrectiveActionsPending = capaPending,
            ClausesMissingEvidence = missingClauses,
            AuditPassProbability = probability,
            ThresholdPercent = AuditThreshold,
            Recommendation = recommendation
        };
    }

    private List<ComplianceTrendPointDto> BuildTrend(ControlCenterDataset dataset)
    {
        if (dataset.Snapshots.Count == 0)
        {
            return new List<ComplianceTrendPointDto>();
        }

        return dataset.Snapshots
            .GroupBy(s => s.CalculatedAt.Date)
            .OrderBy(g => g.Key)
            .Select(g => new ComplianceTrendPointDto
            {
                Date = g.Key.ToString("MMM dd"),
                Compliance = Round2(g.Average(s => s.OverallScore)),
                Target = TargetCompliance
            })
            .ToList();
    }

    private TrendDeltas ComputeTrendDeltas(ControlCenterDataset dataset)
    {
        var byDay = dataset.Snapshots
            .GroupBy(s => s.CalculatedAt.Date)
            .OrderBy(g => g.Key)
            .ToList();

        var complianceSeries = byDay.Select(g => Round2(g.Average(s => s.OverallScore))).ToList();
        var confidenceSeries = byDay.Select(g => Round2(g.Average(s => s.AuditConfidenceScore))).ToList();

        decimal complianceDelta = complianceSeries.Count >= 2 ? Round2(complianceSeries[^1] - complianceSeries[0]) : 0m;
        decimal confidenceDelta = confidenceSeries.Count >= 2 ? Round2(confidenceSeries[^1] - confidenceSeries[0]) : 0m;

        return new TrendDeltas
        {
            ComplianceDelta = complianceDelta,
            ConfidenceDelta = confidenceDelta,
            ComplianceSparkline = complianceSeries.TakeLast(7).ToList(),
            ConfidenceSparkline = confidenceSeries.TakeLast(7).ToList()
        };
    }

    private List<ActivityEventDto> BuildActivityDtos(ControlCenterDataset dataset)
        => dataset.Activities
            .OrderByDescending(a => a.OccurredAt)
            .Take(20)
            .Select(a => new ActivityEventDto
            {
                Id = a.Id.ToString(),
                Type = ActivityTypeToUi(a.Type),
                Title = a.Title,
                Description = a.Description,
                User = string.IsNullOrWhiteSpace(a.ActorName) ? "System" : a.ActorName!,
                Timestamp = a.OccurredAt,
                Department = a.DepartmentName
            })
            .ToList();

    private List<AlertDto> BuildAlertDtos(ControlCenterDataset dataset)
        => dataset.Alerts
            .Where(a => a.Status == AlertStatus.Active)
            .OrderBy(a => (int)a.Severity)
            .Select(a => new AlertDto
            {
                Id = a.Id.ToString(),
                Severity = AlertSeverityToUi(a.Severity),
                Title = a.Title,
                Description = a.Description,
                Timestamp = a.CreatedAt,
                Actions = BuildAlertActions(a.Type)
            })
            .ToList();

    private static List<AlertActionDto> BuildAlertActions(AlertType type) => type switch
    {
        AlertType.CriticalTasks => new()
        {
            new() { Label = "Review Now", Type = "primary", Action = "review-tasks" },
            new() { Label = "Assign Task", Type = "secondary", Action = "assign" }
        },
        AlertType.ComplianceDrop => new()
        {
            new() { Label = "View Details", Type = "primary", Action = "view-details" },
            new() { Label = "Upload Evidence", Type = "secondary", Action = "upload" }
        },
        AlertType.MissingEvidence => new()
        {
            new() { Label = "Upload Evidence", Type = "primary", Action = "upload-evidence" }
        },
        AlertType.OverdueCapa => new()
        {
            new() { Label = "Review CAPA", Type = "primary", Action = "review-capa" }
        },
        AlertType.AuditRisk => new()
        {
            new() { Label = "Prepare Checklist", Type = "primary", Action = "checklist" }
        },
        _ => new() { new() { Label = "View", Type = "primary", Action = "view" } }
    };

    // ---------------------------------------------------------------------
    // Shared helpers
    // ---------------------------------------------------------------------
    private sealed class TrendDeltas
    {
        public decimal ComplianceDelta { get; set; }
        public decimal ConfidenceDelta { get; set; }
        public List<decimal> ComplianceSparkline { get; set; } = new();
        public List<decimal> ConfidenceSparkline { get; set; } = new();
    }

    private sealed record FrameworkDrop(Guid FrameworkId, string Code, decimal Latest, decimal Previous, decimal DropPercent);

    private static IEnumerable<FrameworkDrop> ComputeFrameworkDrops(ControlCenterDataset dataset)
    {
        var drops = new List<FrameworkDrop>();
        foreach (var group in dataset.Snapshots.GroupBy(s => s.FrameworkId).Where(g => g.Key != Guid.Empty))
        {
            var ordered = group.OrderByDescending(s => s.CalculatedAt).ToList();
            var latest = ordered.First();
            var previous = ordered.FirstOrDefault(s => s.CalculatedAt.Date < latest.CalculatedAt.Date);
            if (previous == null)
            {
                continue;
            }

            var drop = previous.OverallScore - latest.OverallScore;
            if (drop >= 3m)
            {
                var code = dataset.Journeys.FirstOrDefault(j => j.FrameworkId == group.Key)?.Framework?.Name ?? group.Key.ToString()[..8];
                drops.Add(new FrameworkDrop(group.Key, code, latest.OverallScore, previous.OverallScore, Round2(drop)));
            }
        }

        return drops;
    }

    private static string BuildImpactLabel(ComplianceTask t)
    {
        var chapterCode = t.ComplianceStandard?.Chapter?.Code;
        var standardCode = t.ComplianceStandard?.Code;
        if (!string.IsNullOrWhiteSpace(standardCode))
        {
            return standardCode!;
        }
        return string.IsNullOrWhiteSpace(chapterCode) ? "General" : chapterCode!;
    }

    private static bool IsCritical(ComplianceTask t)
        => t.Criticality == TaskCriticalityLevel.Critical || t.Priority == ComplianceTaskPriority.Urgent;

    private static bool IsOverdue(ComplianceTask t, DateTime now)
        => t.DueDate.HasValue && t.DueDate.Value.Date < now.Date && t.Status != ComplianceTaskStatus.Completed;

    /// <summary>CAPA overdue = open deficiency with corrective/preventive target past due (SSOT).</summary>
    private static bool IsOverdueDeficiencyCapa(ComplianceDeficiency d, DateTime now)
        => CapaQueryService.IsOverdue(
                ResolveStoredCapaStatus(d.CorrectiveActionStatus, d.CorrectiveActionTargetDate, d.CorrectiveActionCompletedDate),
                d.CorrectiveActionTargetDate, d.CorrectiveActionCompletedDate)
           || CapaQueryService.IsOverdue(
                ResolveStoredCapaStatus(d.PreventiveActionStatus, d.PreventiveActionTargetDate, d.PreventiveActionCompletedDate),
                d.PreventiveActionTargetDate, d.PreventiveActionCompletedDate);

    private static bool IsPendingDeficiencyCapa(ComplianceDeficiency d)
        => d.CorrectiveActionStatus is CapaActionStatus.NotStarted or CapaActionStatus.InProgress
           || d.PreventiveActionStatus is CapaActionStatus.NotStarted or CapaActionStatus.InProgress
           || !string.IsNullOrWhiteSpace(d.CorrectiveActionSummary);

    private static CapaActionStatus ResolveStoredCapaStatus(
        CapaActionStatus stored, DateTime? targetDate, DateTime? completedDate)
    {
        if (completedDate.HasValue || stored == CapaActionStatus.Completed)
        {
            return CapaActionStatus.Completed;
        }

        return stored;
    }

    private static string TaskPriorityToUi(ComplianceTask t)
    {
        if (t.Criticality == TaskCriticalityLevel.Critical || t.Priority == ComplianceTaskPriority.Urgent)
        {
            return "critical";
        }
        return t.Priority == ComplianceTaskPriority.High ? "high" : "medium";
    }

    private static string TaskStatusToUi(ComplianceTask t, DateTime now)
    {
        if (IsOverdue(t, now))
        {
            return "overdue";
        }
        if (t.DueDate.HasValue && t.DueDate.Value.Date <= now.AddDays(7).Date)
        {
            return "due-soon";
        }
        return "on-track";
    }

    private static string ActivityTypeToUi(ActivityEventType type) => type switch
    {
        ActivityEventType.EvidenceUploaded => "evidence",
        ActivityEventType.TaskCompleted => "task",
        ActivityEventType.FindingRaised => "finding",
        ActivityEventType.AuditScheduled => "audit",
        ActivityEventType.CapaApproved => "task",
        ActivityEventType.PolicyUpdated => "task",
        _ => "task"
    };

    private static string AlertSeverityToUi(AlertSeverity severity) => severity switch
    {
        AlertSeverity.Critical => "critical",
        AlertSeverity.High => "warning",
        AlertSeverity.Medium => "warning",
        _ => "info"
    };

    private static decimal Round2(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string Truncate(string value, int max)
        => string.IsNullOrEmpty(value) ? string.Empty : (value.Length <= max ? value : value[..max]);

    private static Guid DeterministicGuid(string key)
    {
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
        return new Guid(bytes);
    }
}
