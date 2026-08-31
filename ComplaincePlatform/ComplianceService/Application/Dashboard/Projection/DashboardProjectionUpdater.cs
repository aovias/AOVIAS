using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Dashboard.Projection;

/// <summary>
/// Applies incremental projection field updates without full snapshot rebuild.
/// </summary>
public sealed class DashboardProjectionUpdater
{
    private static readonly ComplianceLifecycleState[] ActiveJourneyStates =
    {
        ComplianceLifecycleState.GapAssessment,
        ComplianceLifecycleState.InProgress,
        ComplianceLifecycleState.Submitted,
        ComplianceLifecycleState.AuditScheduled,
        ComplianceLifecycleState.RenewalInProgress,
    };

    private readonly IDashboardProjectionRepository _repository;
    private readonly ComplianceDbContext _dbContext;
    private readonly ILogger<DashboardProjectionUpdater> _logger;

    public DashboardProjectionUpdater(
        IDashboardProjectionRepository repository,
        ComplianceDbContext dbContext,
        ILogger<DashboardProjectionUpdater> logger)
    {
        _repository = repository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ApplyIncrementalAsync(
        DashboardProjectionScope scope,
        OutboxMessage message,
        CancellationToken cancellationToken = default)
    {
        if (scope.IsTenantWide || scope.HospitalId == Guid.Empty)
        {
            return;
        }

        var projection = await _repository.GetOrCreateAsync(scope, cancellationToken);
        if (message.Id != Guid.Empty && projection.LastProcessedOutboxMessageId == message.Id)
        {
            return;
        }

        var payload = DashboardProjectionSerializer.DeserializePayload(projection.PayloadJson);
        var organizationId = scope.OrganizationId != Guid.Empty
            ? scope.OrganizationId
            : await ResolveTenantIdAsync(scope.HospitalId, scope.FrameworkId, cancellationToken);

        if (organizationId == Guid.Empty)
        {
            return;
        }

        if (IsTaskEvent(message.Type))
        {
            await RefreshTasksAsync(organizationId, scope, projection, payload, cancellationToken);
        }

        if (IsFindingEvent(message.Type))
        {
            await RefreshAlertsAsync(organizationId, scope, projection, payload, cancellationToken);
        }

        if (IsDeficiencyEvent(message.Type))
        {
            await RefreshTasksAsync(organizationId, scope, projection, payload, cancellationToken);
            await RefreshAlertsAsync(organizationId, scope, projection, payload, cancellationToken);
        }

        if (message.Type.Contains("EvidenceUploaded", StringComparison.OrdinalIgnoreCase))
        {
            await RefreshRecentActivityAsync(organizationId, scope, projection, payload, cancellationToken);
        }

        projection.PayloadJson = DashboardProjectionSerializer.SerializePayload(payload);
        projection.DataVersion += 1;
        projection.LastEventProcessedAt = DateTime.UtcNow;
        projection.LastEventType = message.Type;
        projection.LastProcessedOutboxMessageId = message.Id;
        projection.IsComplete = projection.TotalJourneysCount > 0 || payload.Frameworks.Count > 0;

        await _repository.SaveAsync(projection, cancellationToken);
    }

    private async Task RefreshTasksAsync(
        Guid organizationId,
        DashboardProjectionScope scope,
        DashboardProjection projection,
        DashboardProjectionPayload payload,
        CancellationToken cancellationToken)
    {
        var frameworkId = scope.FrameworkId == Guid.Empty ? (Guid?)null : scope.FrameworkId;

        var query = _dbContext.ComplianceTasks
            .AsNoTracking()
            .Where(t => t.HospitalId == scope.HospitalId);

        if (frameworkId.HasValue)
        {
            query = query.Where(t =>
                t.ComplianceStandard != null &&
                t.ComplianceStandard.Chapter != null &&
                t.ComplianceStandard.Chapter.FrameworkId == frameworkId.Value);
        }

        var tasks = await query
            .Select(t => new
            {
                t.Status,
                t.Priority,
                t.DueDate,
                FrameworkCode = t.ComplianceStandard != null && t.ComplianceStandard.Chapter != null &&
                                t.ComplianceStandard.Chapter.Framework != null
                    ? t.ComplianceStandard.Chapter.Framework.PlatformId
                    : string.Empty,
            })
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var open = tasks.Count(t => t.Status != ComplianceTaskStatus.Completed);
        var overdue = tasks.Count(t => t.Status != ComplianceTaskStatus.Completed && t.DueDate.HasValue && t.DueDate.Value < now);

        payload.TasksSummary = new TasksSummaryDto
        {
            Total = tasks.Count,
            Open = open,
            Overdue = overdue,
            DueSoon = tasks.Count(t => t.Status != ComplianceTaskStatus.Completed && t.DueDate.HasValue &&
                                       t.DueDate.Value >= now && t.DueDate.Value <= now.AddDays(7)),
            ByPriority = tasks
                .Where(t => t.Status != ComplianceTaskStatus.Completed)
                .GroupBy(t => t.Priority.ToString())
                .ToDictionary(g => g.Key, g => g.Count()),
            ByFramework = tasks
                .Where(t => t.Status != ComplianceTaskStatus.Completed && !string.IsNullOrWhiteSpace(t.FrameworkCode))
                .GroupBy(t => t.FrameworkCode)
                .Select(g => new TasksByFrameworkDto
                {
                    FrameworkCode = g.Key,
                    Open = g.Count(),
                    Overdue = g.Count(x => x.DueDate.HasValue && x.DueDate.Value < now),
                })
                .ToList(),
        };

        projection.PendingTasksCount = open;
        projection.OverdueTasksCount = overdue;
        payload.Summary.OverdueTasks = overdue;
    }

    private async Task RefreshAlertsAsync(
        Guid organizationId,
        DashboardProjectionScope scope,
        DashboardProjection projection,
        DashboardProjectionPayload payload,
        CancellationToken cancellationToken)
    {
        var frameworkId = scope.FrameworkId == Guid.Empty ? (Guid?)null : scope.FrameworkId;

        var findings = await (
            from finding in _dbContext.Findings.AsNoTracking()
            join journey in _dbContext.ComplianceJourneys.AsNoTracking()
                on finding.ComplianceJourneyId equals journey.Id
            where finding.Scope == FindingScope.Audit
                  && !finding.IsResolved
                  && journey.TenantId == organizationId
                  && journey.HospitalId == scope.HospitalId
                  && (!frameworkId.HasValue || journey.FrameworkId == frameworkId.Value)
            select finding
        ).Take(50).ToListAsync(cancellationToken);

        payload.Alerts = findings.Select(f => new AlertSnapshotDto
        {
            AlertId = f.Id.ToString(),
            Type = "AuditFinding",
            Severity = f.Severity.ToString(),
            Message = string.IsNullOrWhiteSpace(f.Title) ? f.Description : f.Title,
            EntityType = "Finding",
            EntityId = f.Id.ToString(),
            Acknowledged = f.IsResolved,
            CreatedAt = f.DateRaised.ToString("O"),
        }).ToList();

        projection.OpenAlertsCount = payload.Alerts.Count;
    }

    private async Task RefreshRecentActivityAsync(
        Guid organizationId,
        DashboardProjectionScope scope,
        DashboardProjection projection,
        DashboardProjectionPayload payload,
        CancellationToken cancellationToken)
    {
        var frameworkId = scope.FrameworkId == Guid.Empty ? (Guid?)null : scope.FrameworkId;

        var activities = await (
            from history in _dbContext.ComplianceJourneyHistories.AsNoTracking()
            join journey in _dbContext.ComplianceJourneys.AsNoTracking()
                on history.ComplianceJourneyId equals journey.Id
            where journey.TenantId == organizationId
                  && journey.HospitalId == scope.HospitalId
                  && (!frameworkId.HasValue || journey.FrameworkId == frameworkId.Value)
            orderby history.ChangedAt descending
            select new ActivitySnapshotDto
            {
                ActivityId = history.Id.ToString(),
                Actor = history.ChangedBy.ToString(),
                CreatedAt = history.ChangedAt.ToString("O"),
                Type = history.NewStatus.ToString(),
                Summary = history.Remarks ?? history.NewStatus.ToString(),
            }
        ).Take(6).ToListAsync(cancellationToken);

        payload.RecentActivity = activities;
        projection.LastActivityAt = activities.FirstOrDefault() is { CreatedAt: var createdAt }
            && DateTime.TryParse(createdAt, out var parsed)
            ? parsed
            : projection.LastActivityAt;
    }

    private async Task<Guid> ResolveTenantIdAsync(
        Guid hospitalId,
        Guid frameworkId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.ComplianceJourneys.AsNoTracking().Where(j => j.HospitalId == hospitalId);
        if (frameworkId != Guid.Empty)
        {
            query = query.Where(j => j.FrameworkId == frameworkId);
        }

        return await query.Select(j => j.TenantId).FirstOrDefaultAsync(cancellationToken);
    }

    private static bool IsTaskEvent(string type) =>
        type.Contains("TaskCreated", StringComparison.OrdinalIgnoreCase)
        || type.Contains("TaskCompleted", StringComparison.OrdinalIgnoreCase)
        || type.Contains("TaskOverdue", StringComparison.OrdinalIgnoreCase);

    private static bool IsFindingEvent(string type) =>
        type.Contains("FindingCreated", StringComparison.OrdinalIgnoreCase)
        || type.Contains("FindingUpdated", StringComparison.OrdinalIgnoreCase);

    private static bool IsDeficiencyEvent(string type) =>
        type.Contains("Deficiency", StringComparison.OrdinalIgnoreCase);
}
