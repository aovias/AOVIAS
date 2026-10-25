using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>
/// Enriched read projections for the Deficiency Command Center (Sprint 6 H5).
/// </summary>
public sealed class DeficiencyCommandCenterQueryService : IDeficiencyCommandCenterQueryService
{
    private readonly ComplianceDbContext _db;
    private readonly ISlaEvaluationService _slaEvaluation;
    private readonly IEscalationService _escalationService;
    private readonly ICapaQueryService _capaQuery;
    private readonly IDeficiencyMetricsService _deficiencyMetrics;

    public DeficiencyCommandCenterQueryService(
        ComplianceDbContext db,
        ISlaEvaluationService slaEvaluation,
        IEscalationService escalationService,
        ICapaQueryService capaQuery,
        IDeficiencyMetricsService deficiencyMetrics)
    {
        _db = db;
        _slaEvaluation = slaEvaluation;
        _escalationService = escalationService;
        _capaQuery = capaQuery;
        _deficiencyMetrics = deficiencyMetrics;
    }

    public async Task<DeficiencyCommandCenterDashboardDto> GetDashboardAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken ct = default)
    {
        // Tenant isolation must hold here. `IgnoreQueryFilters()` was bypassing the global
        // `TenantId == OrganizationId` filter while scoping only on hospital + framework, so the
        // dashboard aggregated every tenant that shares a hospital id — inflating every deficiency
        // KPI and leaking across tenants. The filter is the designed boundary; respect it.
        var rows = await _db.ComplianceDeficiencies
            .AsNoTracking()
            .Where(d => d.HospitalId == hospitalId && d.FrameworkId == frameworkId)
            .ToListAsync(ct);

        var openIds = rows
            .Where(d => d.Status != DeficiencyStatus.Closed && d.Status != DeficiencyStatus.Cancelled)
            .Select(d => d.Id)
            .ToList();

        if (openIds.Count > 0)
        {
            await _slaEvaluation.EvaluateBatchAsync(openIds, persist: true, ct);
            rows = await _db.ComplianceDeficiencies
                .AsNoTracking()
                .Where(d => d.HospitalId == hospitalId && d.FrameworkId == frameworkId)
                .ToListAsync(ct);
        }

        var dto = new DeficiencyCommandCenterDashboardDto { TotalDeficiencies = rows.Count };
        var organizationId = rows.FirstOrDefault()?.OrganizationId ?? Guid.Empty;
        var scope = AnalyticsScope.ForHospital(organizationId, hospitalId, frameworkId);
        var counts = await _deficiencyMetrics.GetCountsAsync(scope, ct);
        var capaMetrics = organizationId != Guid.Empty
            ? await _deficiencyMetrics.GetCapaMetricsAsync(scope, ct)
            : new CapaMetricsDto();
        var slaMetrics = organizationId != Guid.Empty
            ? await _deficiencyMetrics.GetSlaMetricsAsync(scope, ct)
            : new SlaEscalationMetricsDto();

        dto.OpenDeficiencies = counts.Open;
        dto.ResolvedDeficiencies = counts.Closed;
        dto.OpenCapas = capaMetrics.OpenCapas;
        dto.OverdueCapas = capaMetrics.OverdueCapas;
        dto.EffectiveCapas = capaMetrics.EffectiveCapas;
        dto.IneffectiveCapas = capaMetrics.IneffectiveCapas;
        dto.RecurringIssues = capaMetrics.RecurringIssues;
        dto.PendingEffectivenessReviews = capaMetrics.PendingReviews;
        dto.AverageOpenCapaAgeDays = capaMetrics.AverageOpenCapaAgeDays;
        dto.AgingBuckets = capaMetrics.AgingBuckets ?? new CapaAgingBucketsDto();
        dto.DepartmentOverdueCapas = capaMetrics.DepartmentOverdueCapas;
        dto.SlaOnTrack = slaMetrics.OnTrackCount;
        dto.SlaWarning = slaMetrics.WarningCount;
        dto.SlaAtRisk = slaMetrics.AtRiskCount;
        dto.SlaBreached = slaMetrics.BreachedCount;
        dto.EscalatedDeficiencies = slaMetrics.EscalatedCount;
        dto.CriticalEscalations = slaMetrics.CriticalEscalations;

        var byRootCause = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var byRisk = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var byLifecycle = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var byDepartment = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var byStage = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var d in rows)
        {
            var isOpen = DeficiencyMetricDefinitions.IsOpen(d);
            var closed = DeficiencyMetricDefinitions.IsClosed(d);

            switch (d.Severity)
            {
                case DeficiencySeverity.Critical:
                    dto.CriticalDeficiencies++;
                    if (isOpen) dto.BlockingDeficiencies++;
                    break;
                case DeficiencySeverity.Major:
                    dto.MajorDeficiencies++;
                    break;
                case DeficiencySeverity.Moderate:
                    dto.ModerateDeficiencies++;
                    break;
                case DeficiencySeverity.Minor:
                    dto.MinorDeficiencies++;
                    break;
            }

            switch (d.Status)
            {
                case DeficiencyStatus.Identified:
                    dto.AwaitingAssignment++;
                    break;
                case DeficiencyStatus.InProgress:
                    dto.InProgressDeficiencies++;
                    break;
                case DeficiencyStatus.EvidenceSubmitted:
                    dto.AwaitingEvidence++;
                    break;
                case DeficiencyStatus.ReviewPending:
                    dto.VerificationPending++;
                    break;
                case DeficiencyStatus.Verified:
                    dto.VerifiedDeficiencies++;
                    break;
            }

            if (d.AssessedRiskLevel == DeficiencyRiskLevel.High)
            {
                dto.HighRiskDeficiencies++;
            }
            else if (d.AssessedRiskLevel == DeficiencyRiskLevel.Critical)
            {
                dto.CriticalRiskDeficiencies++;
            }

            if (d.OriginType == DeficiencyOriginType.AuditFinding)
            {
                dto.AuditOriginDeficiencies++;
            }
            else if (d.OriginType == DeficiencyOriginType.Manual)
            {
                dto.ManualOriginDeficiencies++;
            }
            else if (string.Equals(d.Source, "EvaluationEngine", StringComparison.OrdinalIgnoreCase)
                     || d.OriginType == DeficiencyOriginType.EvaluationEngine)
            {
                dto.AssessmentOriginDeficiencies++;
            }

            Increment(byRootCause, d.RootCauseCategory?.ToString() ?? "Unclassified");
            Increment(byRisk, d.AssessedRiskLevel?.ToString() ?? "Unassessed");
            var lifecycle = DeficiencyLifecycleTransitions.ToLifecycle(d.Status);
            Increment(byLifecycle, DeficiencyLifecycleTransitions.GetDisplayName(lifecycle));
            Increment(byDepartment, string.IsNullOrWhiteSpace(d.Department) ? "Unassigned" : d.Department!);
            Increment(byStage, d.MaturityLevel.ToString());
        }

        dto.ByRootCauseCategory = ToBreakdown(byRootCause);
        dto.ByRiskLevel = ToBreakdown(byRisk);
        dto.ByLifecycleStatus = ToBreakdown(byLifecycle);
        dto.ByDepartment = ToBreakdown(byDepartment);
        dto.ByStage = ToBreakdown(byStage);

        dto.AuditFindings = organizationId != Guid.Empty
            ? await _deficiencyMetrics.CountOpenFindingsAsync(organizationId, ct)
            : 0;

        return dto;
    }

    private static void Increment(Dictionary<string, int> map, string key)
    {
        map[key] = map.GetValueOrDefault(key) + 1;
    }

    private static IReadOnlyList<DeficiencyCountByLabelDto> ToBreakdown(Dictionary<string, int> map) =>
        map.OrderByDescending(x => x.Value)
            .Select(x => new DeficiencyCountByLabelDto { Label = x.Key, Count = x.Value })
            .ToList();

    public async Task<DeficiencyWorkspaceListDto> GetWorkspaceListAsync(
        Guid hospitalId,
        Guid frameworkId,
        string? status,
        string? severity,
        string? maturityLevel,
        string? search,
        Guid? objectiveElementId,
        string? sortBy,
        bool sortDesc,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = BuildWorkspaceQuery(hospitalId, frameworkId, status, severity, maturityLevel, search, objectiveElementId);
        var total = await query.CountAsync(ct);
        query = ApplySort(query, sortBy, sortDesc);

        var pageRows = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var deficiencyIds = pageRows.Select(x => x.Deficiency.Id).ToList();
        var slaById = (await _slaEvaluation.EvaluateBatchAsync(deficiencyIds, persist: true, ct))
            .ToDictionary(x => x.DeficiencyId);

        var tasksByDeficiency = await LoadPrimaryTasksAsync(deficiencyIds, ct);

        var items = pageRows
            .Select(row =>
            {
                tasksByDeficiency.TryGetValue(row.Deficiency.Id, out var task);
                slaById.TryGetValue(row.Deficiency.Id, out var sla);
                return MapListItem(row, task, sla);
            })
            .ToList();

        return new DeficiencyWorkspaceListDto
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<DeficiencyWorkspaceDetailDto?> GetWorkspaceDetailAsync(Guid id, CancellationToken ct = default)
    {
        var row = await (
            from d in _db.ComplianceDeficiencies.IgnoreQueryFilters().AsNoTracking()
            where d.Id == id
            join obl in _db.OrganizationComplianceObligations.AsNoTracking()
                on d.ComplianceObligationId equals obl.Id
            join ch in _db.ComplianceChapters.AsNoTracking()
                on obl.ChapterId equals ch.Id
            join std in _db.ComplianceStandards.AsNoTracking()
                on obl.StandardId equals std.Id
            select new WorkspaceRow
            {
                Deficiency = d,
                Obligation = obl,
                ChapterCode = ch.Code,
                ChapterName = ch.Name,
                StandardCode = std.Code,
                StandardName = std.Name,
            }).FirstOrDefaultAsync(ct);

        if (row == null)
        {
            return null;
        }

        var tasks = await LoadPrimaryTasksAsync(new[] { id }, ct);
        tasks.TryGetValue(id, out var task);

        var sla = await _slaEvaluation.EvaluateAsync(id, persist: true, ct);
        var escalation = await _escalationService.GetHistoryAsync(id, ct);

        var detail = MapDetail(row, task, sla);
        var evidenceStats = await GetEvidenceStatsAsync(row.Deficiency.ObjectiveElementId, ct);
        detail.EvidenceCount = evidenceStats.Total;
        detail.RejectedEvidenceCount = evidenceStats.Rejected;
        detail.NeedsMoreEvidenceCount = evidenceStats.NeedsMore;
        detail.LatestEvidenceTitle = evidenceStats.LatestTitle;
        detail.LatestEvidenceDate = evidenceStats.LatestDate;

        if (detail.BlocksCore || detail.BlocksCommitment || detail.BlocksAchievement || detail.BlocksExcellence)
        {
            detail.BlockingReason = $"Critical {detail.MaturityLevel} deficiency blocks stage progression until resolved.";
        }

        if (row.Deficiency.OriginType == DeficiencyOriginType.AuditFinding)
        {
            detail.OriginType = row.Deficiency.OriginType.ToString();
            detail.OriginFindingId = row.Deficiency.OriginId;
            detail.OriginFindingNumber = row.Deficiency.OriginReference;
            detail.PromotedAt = row.Deficiency.PromotedAt;

            if (row.Deficiency.OriginId.HasValue)
            {
                var findingSource = await _db.Findings
                    .AsNoTracking()
                    .Where(f => f.Id == row.Deficiency.OriginId.Value)
                    .Select(f => f.RaisedBy)
                    .FirstOrDefaultAsync(ct);
                if (findingSource != default)
                {
                    detail.OriginFindingSource = findingSource.ToString();
                }
            }
        }

        var def = row.Deficiency;
        detail.ProblemStatement = def.ProblemStatement;
        detail.RootCauseSummary = def.RootCauseSummary;
        detail.ImpactDescription = def.ImpactDescription;
        detail.CorrectiveActionSummary = def.CorrectiveActionSummary;
        detail.PreventiveActionSummary = def.PreventiveActionSummary;
        detail.Why1 = def.Why1;
        detail.Why2 = def.Why2;
        detail.Why3 = def.Why3;
        detail.Why4 = def.Why4;
        detail.Why5 = def.Why5;
        detail.FinalRootCause = def.FinalRootCause;
        detail.ActionPlan = def.ActionPlan;
        detail.ExpectedOutcome = def.ExpectedOutcome;
        detail.RemediationTargetCompletion = def.RemediationTargetCompletion;
        detail.SuccessCriteria = def.SuccessCriteria;
        detail.EffectivenessReviewStatus = def.EffectivenessReviewStatus.ToString();

        var timeline = await _db.DeficiencyAuditTrails
            .AsNoTracking()
            .Where(t => t.DeficiencyId == id)
            .OrderByDescending(t => t.OccurredAt)
            .Take(50)
            .Select(t => new DeficiencyTimelineEntryDto
            {
                Action = t.Action,
                ActorType = t.ActorType,
                OccurredAt = t.OccurredAt,
                DetailJson = t.DetailJson,
            })
            .ToListAsync(ct);

        detail.Timeline = timeline;

        detail.EscalationHistory = escalation.Select(h => new DeficiencyEscalationHistoryItemDto
        {
            Id = h.Id,
            Level = h.Level,
            TargetUserId = h.TargetUserId,
            TargetLabel = h.TargetLabel,
            Reason = h.Reason,
            State = h.State,
            TriggeredAt = h.TriggeredAt,
            AcknowledgedAt = h.AcknowledgedAt,
            ResolvedAt = h.ResolvedAt,
        }).ToList();

        var activeEscalation = escalation.FirstOrDefault(h => h.State is "Escalated" or "Acknowledged" or "Pending");
        if (activeEscalation != null)
        {
            detail.EscalationTargetUserId = activeEscalation.TargetUserId;
            detail.EscalationTargetLabel = activeEscalation.TargetLabel;
        }

        var capa = await _capaQuery.GetCapaAsync(id, ct);
        if (capa != null && detail is DeficiencyWorkspaceDetailDto capaDetail)
        {
            capaDetail.CapaLifecycleSteps = capa.LifecycleSteps;
            capaDetail.RecurrenceReason = capa.RecurrenceReason;
            capaDetail.RecurrenceDetectedAt = capa.RecurrenceDetectedAt;
        }

        return detail;
    }

    private IQueryable<WorkspaceRow> BuildWorkspaceQuery(
        Guid hospitalId,
        Guid frameworkId,
        string? status,
        string? severity,
        string? maturityLevel,
        string? search,
        Guid? objectiveElementId)
    {
        // Same tenant boundary as GetDashboardAsync. Leaving the bypass here made the workspace grid
        // disagree with the KPI header above it — 176 rows under a "88 total" card — because the
        // dashboard respected the filter and the grid did not.
        var deficiencies = _db.ComplianceDeficiencies
            .AsNoTracking()
            .Where(d => d.HospitalId == hospitalId && d.FrameworkId == frameworkId);

        if (!string.IsNullOrWhiteSpace(status)
            && Enum.TryParse<DeficiencyStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            deficiencies = deficiencies.Where(d => d.Status == parsedStatus);
        }

        if (!string.IsNullOrWhiteSpace(severity)
            && Enum.TryParse<DeficiencySeverity>(severity, ignoreCase: true, out var parsedSeverity))
        {
            deficiencies = deficiencies.Where(d => d.Severity == parsedSeverity);
        }

        if (!string.IsNullOrWhiteSpace(maturityLevel)
            && Enum.TryParse<ObjectiveElementClassification>(maturityLevel, ignoreCase: true, out var parsedStage))
        {
            deficiencies = deficiencies.Where(d => d.MaturityLevel == parsedStage);
        }

        if (objectiveElementId.HasValue && objectiveElementId.Value != Guid.Empty)
        {
            deficiencies = deficiencies.Where(d => d.ObjectiveElementId == objectiveElementId.Value);
        }

        var query =
            from d in deficiencies
            join obl in _db.OrganizationComplianceObligations.AsNoTracking()
                on d.ComplianceObligationId equals obl.Id
            join ch in _db.ComplianceChapters.AsNoTracking()
                on obl.ChapterId equals ch.Id
            join std in _db.ComplianceStandards.AsNoTracking()
                on obl.StandardId equals std.Id
            select new WorkspaceRow
            {
                Deficiency = d,
                Obligation = obl,
                ChapterCode = ch.Code,
                ChapterName = ch.Name,
                StandardCode = std.Code,
                StandardName = std.Name,
            };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Deficiency.Title.ToLower().Contains(term)
                || x.Deficiency.DeficiencyCode.ToLower().Contains(term)
                || x.Obligation.Code.ToLower().Contains(term)
                || x.Obligation.Title.ToLower().Contains(term)
                || x.ChapterCode.ToLower().Contains(term)
                || x.StandardCode.ToLower().Contains(term));
        }

        return query;
    }

    private async Task<Dictionary<Guid, ComplianceTask>> LoadPrimaryTasksAsync(
        IReadOnlyList<Guid> deficiencyIds,
        CancellationToken ct)
    {
        if (deficiencyIds.Count == 0)
        {
            return new Dictionary<Guid, ComplianceTask>();
        }

        var tasks = await _db.ComplianceTasks
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(t => t.SourceDeficiencyId != null
                        && deficiencyIds.Contains(t.SourceDeficiencyId.Value)
                        && t.Status != ComplianceTaskStatus.Completed)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(ct);

        return tasks
            .GroupBy(t => t.SourceDeficiencyId!.Value)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private static IQueryable<WorkspaceRow> ApplySort(IQueryable<WorkspaceRow> query, string? sortBy, bool sortDesc)
    {
        return (sortBy ?? "severity").ToLowerInvariant() switch
        {
            "duedate" => sortDesc
                ? query.OrderByDescending(x => x.Deficiency.DueDate)
                : query.OrderBy(x => x.Deficiency.DueDate),
            "status" => sortDesc
                ? query.OrderByDescending(x => x.Deficiency.Status)
                : query.OrderBy(x => x.Deficiency.Status),
            "updated" => sortDesc
                ? query.OrderByDescending(x => x.Deficiency.UpdatedAt)
                : query.OrderBy(x => x.Deficiency.UpdatedAt),
            "stage" => sortDesc
                ? query.OrderByDescending(x => x.Deficiency.MaturityLevel)
                : query.OrderBy(x => x.Deficiency.MaturityLevel),
            _ => sortDesc
                ? query.OrderByDescending(x => x.Deficiency.Severity).ThenByDescending(x => x.Deficiency.DetectedDate)
                : query.OrderBy(x => x.Deficiency.Severity).ThenBy(x => x.Deficiency.DetectedDate),
        };
    }

    private DeficiencyWorkspaceItemDto MapListItem(
        WorkspaceRow row,
        ComplianceTask? task,
        SlaEvaluationResultDto? sla = null) =>
        MapCore(row, task, new DeficiencyWorkspaceItemDto(), sla);

    private DeficiencyWorkspaceDetailDto MapDetail(
        WorkspaceRow row,
        ComplianceTask? task,
        SlaEvaluationResultDto? sla = null) =>
        MapCore(row, task, new DeficiencyWorkspaceDetailDto
        {
            RiskCategory = row.Deficiency.RiskCategory,
            Source = row.Deficiency.Source,
            LastEvaluatedDate = row.Deficiency.LastEvaluatedDate,
            ComplianceObligationId = row.Deficiency.ComplianceObligationId,
        }, sla);

    private T MapCore<T>(
        WorkspaceRow row,
        ComplianceTask? primaryTask,
        T dto,
        SlaEvaluationResultDto? sla = null) where T : DeficiencyWorkspaceItemDto
    {
        var d = row.Deficiency;
        var isOpen = d.Status != DeficiencyStatus.Closed;
        var isCritical = d.Severity == DeficiencySeverity.Critical && isOpen;
        var stage = d.MaturityLevel;

        dto.Id = d.Id;
        dto.DeficiencyCode = d.DeficiencyCode;
        dto.Title = d.Title;
        dto.Description = d.Description;
        dto.DeficiencyType = d.DeficiencyType.ToString();
        dto.Severity = d.Severity.ToString();
        dto.Status = d.Status.ToString();
        var lifecycle = DeficiencyLifecycleTransitions.ToLifecycle(d.Status);
        dto.LifecycleStatus = DeficiencyLifecycleTransitions.GetDisplayName(lifecycle);
        dto.MaturityLevel = stage.ToString();
        dto.RootCauseCategory = d.RootCauseCategory?.ToString();
        dto.Classification = "ComplianceDeficiency";
        dto.ObjectiveElementId = d.ObjectiveElementId;
        dto.ObjectiveElementCode = row.Obligation.Code;
        dto.ObjectiveElementName = row.Obligation.Title;
        dto.ChapterCode = row.ChapterCode;
        dto.ChapterName = row.ChapterName;
        dto.StandardCode = row.StandardCode;
        dto.StandardName = row.StandardName;
        dto.AssignedOwner = d.AssignedTo ?? primaryTask?.AssignedToUserId;
        dto.Department = d.Department;
        dto.DueDate = d.DueDate ?? primaryTask?.DueDate;
        dto.AssignedDate = d.AssignedDate;
        dto.TargetResolutionDate = d.TargetResolutionDate ?? d.DueDate;
        dto.ActualResolutionDate = d.ActualResolutionDate;
        dto.RiskLikelihood = d.RiskLikelihood;
        dto.RiskImpact = d.RiskImpact;
        dto.RiskScore = d.RiskScore;
        dto.AssessedRiskLevel = d.AssessedRiskLevel?.ToString();
        dto.IsOverdue = isOpen && dto.DueDate.HasValue && dto.DueDate.Value.Date < DateTime.UtcNow.Date;

        ApplySlaFields(d, dto, sla);
        ApplyCapaFields(d, dto);

        dto.EvidenceStatus = row.Obligation.EvidenceStatus.ToString();
        dto.VerificationStatus = row.Obligation.VerificationStatus.ToString();
        dto.DetectedDate = d.DetectedDate;
        dto.UpdatedAt = d.UpdatedAt;
        dto.ClosedDate = d.ClosedDate;

        if (primaryTask != null)
        {
            dto.PrimaryRemediationTaskId = primaryTask.Id;
            dto.PrimaryTaskTitle = primaryTask.Title;
            dto.PrimaryTaskStatus = primaryTask.Status.ToString();
        }

        dto.BlocksCore = isCritical && stage == ObjectiveElementClassification.Core;
        dto.BlocksCommitment = isCritical && stage == ObjectiveElementClassification.Commitment;
        dto.BlocksAchievement = isCritical && stage == ObjectiveElementClassification.Achievement;
        dto.BlocksExcellence = isCritical && stage == ObjectiveElementClassification.Excellence;
        dto.BlockingStageLabel = isCritical
            ? $"Blocks {stage}"
            : isOpen ? "Not Blocking" : "No Longer Blocking";

        return dto;
    }

    private static void ApplySlaFields(
        ComplianceDeficiency d,
        DeficiencyWorkspaceItemDto dto,
        SlaEvaluationResultDto? sla)
    {
        dto.SlaStatus = sla?.CurrentSlaStatus ?? d.CurrentSlaStatus?.ToString();
        dto.SlaDueDate = sla?.SlaDueDate ?? d.SlaDueDate;
        dto.DaysRemaining = sla?.DaysRemaining;
        dto.SlaPercentConsumed = sla?.PercentConsumed;
        dto.EscalationLevel = d.CurrentEscalationLevel;
        dto.EscalationState = d.CurrentEscalationState?.ToString();
        dto.IsOverdue = sla?.IsOverdue ?? dto.IsOverdue;

        if (dto is DeficiencyWorkspaceDetailDto detail && sla != null)
        {
            detail.SlaPolicyId = sla.SlaPolicyId;
            detail.SlaPolicyName = sla.PolicyName;
            detail.SlaStartDate = sla.SlaStartDate;
            detail.ElapsedHours = sla.ElapsedHours;
            detail.RemainingHours = sla.RemainingHours;
            detail.OverdueHours = sla.OverdueHours;
        }
    }

    private void ApplyCapaFields(ComplianceDeficiency d, DeficiencyWorkspaceItemDto dto)
    {
        var corrective = _capaQuery.ResolveActionStatus(
            d.CorrectiveActionStatus, d.CorrectiveActionTargetDate, d.CorrectiveActionCompletedDate);
        var preventive = _capaQuery.ResolveActionStatus(
            d.PreventiveActionStatus, d.PreventiveActionTargetDate, d.PreventiveActionCompletedDate);

        dto.CapaNumber = d.CapaNumber;
        dto.OwnerAcceptedAt = d.OwnerAcceptedAt;
        dto.OwnerAcceptedBy = d.OwnerAcceptedBy;
        dto.OwnerAcceptanceRejected = d.OwnerAcceptanceRejected;
        dto.CorrectiveActionStatus = corrective.ToString();
        dto.PreventiveActionStatus = preventive.ToString();
        dto.EffectivenessReviewStatus = d.EffectivenessReviewStatus.ToString();
        dto.IsRecurringIssue = d.IsRecurringIssue;
        dto.CapaOverdue = CapaQueryService.IsOverdue(corrective, d.CorrectiveActionTargetDate, d.CorrectiveActionCompletedDate)
            || CapaQueryService.IsOverdue(preventive, d.PreventiveActionTargetDate, d.PreventiveActionCompletedDate);

        if (dto is DeficiencyWorkspaceDetailDto detail)
        {
            detail.CorrectiveActionOwner = d.CorrectiveActionOwner ?? d.AssignedTo;
            detail.CorrectiveActionStartDate = d.CorrectiveActionStartDate;
            detail.CorrectiveActionTargetDate = d.CorrectiveActionTargetDate;
            detail.CorrectiveActionCompletedDate = d.CorrectiveActionCompletedDate;
            detail.CorrectiveActionOverdue = CapaQueryService.IsOverdue(
                corrective, d.CorrectiveActionTargetDate, d.CorrectiveActionCompletedDate);
            detail.PreventiveActionOwner = d.PreventiveActionOwner;
            detail.PreventiveActionTargetDate = d.PreventiveActionTargetDate;
            detail.PreventiveActionCompletedDate = d.PreventiveActionCompletedDate;
            detail.PreventiveActionSuccessCriteria = d.PreventiveActionSuccessCriteria ?? d.SuccessCriteria;
            detail.PreventiveActionOverdue = CapaQueryService.IsOverdue(
                preventive, d.PreventiveActionTargetDate, d.PreventiveActionCompletedDate);
            detail.EffectivenessReviewScheduledDate = d.EffectivenessReviewScheduledDate;
            detail.EffectivenessReviewDaysOffset = d.EffectivenessReviewDaysOffset;
            detail.EffectivenessReviewDate = d.EffectivenessReviewDate;
            detail.EffectivenessReviewer = d.EffectivenessReviewer;
            detail.EffectivenessReviewNotes = d.EffectivenessReviewNotes;
            detail.EffectivenessReviewOutcome = d.EffectivenessReviewOutcome;
            detail.RecurrenceReason = d.RecurrenceReason;
            detail.RecurrenceDetectedAt = d.RecurrenceDetectedAt;
        }
    }

    /// <summary>
    /// Counts every evidence link on the objective element, in any verification state.
    ///
    /// <para>
    /// This is a <b>ledger</b>, not a work queue: it answers "how much evidence stands against this
    /// objective element", which is why approved and rejected links are included. The Evidence
    /// Review Queue answers a different question — "what is waiting for me to review" — and so
    /// defaults to Awaiting Review. The two legitimately disagree, and RC2S1-M4.1 confirmed the
    /// disagreement is a difference of question, not of data.
    /// </para>
    ///
    /// <para>
    /// Neither <c>Evidence</c> nor <c>ObjectiveElementEvidence</c> carries a global query filter
    /// (see <c>ComplianceDbContext.OnModelCreating</c>), so the <c>IgnoreQueryFilters()</c> these
    /// queries used to carry was a no-op. It has been removed: it suggested a tenant boundary was
    /// deliberately being crossed here, which is not the case, and reading it that way produced one
    /// wrong root-cause diagnosis already.
    /// </para>
    /// </summary>
    private async Task<(int Total, int Rejected, int NeedsMore, string? LatestTitle, DateTime? LatestDate)> GetEvidenceStatsAsync(
        Guid objectiveElementId,
        CancellationToken ct)
    {
        var links = await _db.ObjectiveElementEvidences
            .AsNoTracking()
            .Where(x => x.ObjectiveElementId == objectiveElementId)
            .Select(x => new { x.VerificationStatus, x.EvidenceId, x.LinkedAt })
            .ToListAsync(ct);

        if (links.Count == 0)
        {
            return (0, 0, 0, null, null);
        }

        var evidenceIds = links.Select(x => x.EvidenceId).Distinct().ToList();
        var titles = await _db.Evidences
            .AsNoTracking()
            .Where(e => evidenceIds.Contains(e.Id))
            .Select(e => new { e.Id, e.Title, e.UploadedOn })
            .ToListAsync(ct);

        var latest = titles.OrderByDescending(t => t.UploadedOn).FirstOrDefault();

        return (
            links.Count,
            links.Count(x => x.VerificationStatus == EvidenceVerificationStatus.Rejected),
            links.Count(x => x.VerificationStatus == EvidenceVerificationStatus.NeedsMoreEvidence),
            latest?.Title,
            latest?.UploadedOn);
    }

    private sealed class WorkspaceRow
    {
        public ComplianceDeficiency Deficiency { get; init; } = null!;
        public OrganizationComplianceObligation Obligation { get; init; } = null!;
        public string ChapterCode { get; init; } = string.Empty;
        public string ChapterName { get; init; } = string.Empty;
        public string StandardCode { get; init; } = string.Empty;
        public string StandardName { get; init; } = string.Empty;
    }
}
