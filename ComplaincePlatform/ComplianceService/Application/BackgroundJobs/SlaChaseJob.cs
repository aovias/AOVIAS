using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.BackgroundJobs;

/// <summary>
/// Daily SLA chase: re-evaluate open deficiencies and emit SLA/overdue signals.
/// Complements <see cref="TaskOverdueEscalationJob"/> (does not re-emit TaskOverdue).
/// </summary>
public sealed class SlaChaseJob
{
    private readonly ComplianceDbContext _db;
    private readonly ISlaEvaluationService _slaEvaluation;
    private readonly IComplianceNotificationPublisher _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SlaChaseJob> _logger;

    public SlaChaseJob(
        ComplianceDbContext db,
        ISlaEvaluationService slaEvaluation,
        IComplianceNotificationPublisher notifications,
        IUnitOfWork unitOfWork,
        ILogger<SlaChaseJob> logger)
    {
        _db = db;
        _slaEvaluation = slaEvaluation;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = new[] { 60, 300, 900 })]
    public async Task ChaseOpenSlaAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting SlaChaseJob");

        var openStatuses = new[]
        {
            DeficiencyStatus.Identified,
            DeficiencyStatus.Assigned,
            DeficiencyStatus.InProgress,
            DeficiencyStatus.EvidenceSubmitted,
            DeficiencyStatus.ReviewPending,
            DeficiencyStatus.Verified
        };

        var openIds = await _db.ComplianceDeficiencies.AsNoTracking()
            .Where(d => openStatuses.Contains(d.Status))
            .OrderBy(d => d.SlaDueDate ?? d.DueDate ?? d.DetectedDate)
            .Select(d => d.Id)
            .Take(2000)
            .ToListAsync(cancellationToken);

        var evaluated = 0;
        var breached = 0;
        var atRisk = 0;

        foreach (var batch in openIds.Chunk(50))
        {
            var results = await _slaEvaluation.EvaluateBatchAsync(batch, persist: true, cancellationToken);
            foreach (var result in results)
            {
                evaluated++;
                if (string.Equals(result.CurrentSlaStatus, nameof(SlaStatus.Breached), StringComparison.OrdinalIgnoreCase))
                {
                    breached++;
                    await NotifyDeficiencySlaAsync(result, "CapaOverdue", "CAPA SLA breached", cancellationToken);
                }
                else if (string.Equals(result.CurrentSlaStatus, nameof(SlaStatus.AtRisk), StringComparison.OrdinalIgnoreCase)
                         || string.Equals(result.CurrentSlaStatus, nameof(SlaStatus.Warning), StringComparison.OrdinalIgnoreCase))
                {
                    atRisk++;
                }
            }
        }

        // Surface overdue open tasks as SLA-chase notifications without duplicating TaskOverdue emission
        // (TaskOverdueEscalationJob owns ComplianceEventType.TaskOverdue).
        var today = DateTime.UtcNow.Date;
        var overdueTasks = await _db.ComplianceTasks.AsNoTracking()
            .Where(t => t.DueDate != null
                        && t.DueDate.Value.Date < today
                        && t.Status != ComplianceTaskStatus.Completed)
            .OrderBy(t => t.DueDate)
            .Take(500)
            .Select(t => new { t.Id, t.HospitalId, t.AssignedToUserId, t.DueDate, t.Title })
            .ToListAsync(cancellationToken);

        var taskNotified = 0;
        foreach (var task in overdueTasks)
        {
            if (string.IsNullOrWhiteSpace(task.AssignedToUserId))
            {
                continue;
            }

            await _notifications.PublishAsync(new ComplianceNotificationMessage
            {
                EventType = "TaskAssigned",
                Title = "Task overdue (SLA chase)",
                Body = $"Task '{task.Title}' is overdue (due {task.DueDate:yyyy-MM-dd}).",
                HospitalId = task.HospitalId,
                RecipientUserId = task.AssignedToUserId,
                Data = new Dictionary<string, string>
                {
                    ["taskId"] = task.Id.ToString(),
                    ["resourceId"] = task.Id.ToString(),
                    ["dueDate"] = task.DueDate?.ToString("O") ?? string.Empty,
                    ["source"] = "SlaChaseJob",
                },
            }, cancellationToken);
            taskNotified++;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "SlaChaseJob completed. Evaluated={Evaluated} Breached={Breached} AtRiskOrWarning={AtRisk} OverdueTaskNotices={TaskNotices}",
            evaluated,
            breached,
            atRisk,
            taskNotified);
    }

    private async Task NotifyDeficiencySlaAsync(
        SlaEvaluationResultDto result,
        string eventType,
        string title,
        CancellationToken ct)
    {
        var deficiency = await _db.ComplianceDeficiencies.AsNoTracking()
            .Where(d => d.Id == result.DeficiencyId)
            .Select(d => new { d.Id, d.OrganizationId, d.HospitalId, d.AssignedTo, d.CapaNumber, d.DeficiencyCode, d.SlaDueDate })
            .FirstOrDefaultAsync(ct);

        if (deficiency == null)
        {
            return;
        }

        // SlaEvaluationService already emits DeficiencySlaBreached on status transition.
        // Chase job adds a CAPA-overdue notification chase for assignees.
        await _notifications.PublishAsync(new ComplianceNotificationMessage
        {
            EventType = eventType,
            Title = title,
            Body = $"Deficiency {deficiency.CapaNumber ?? deficiency.DeficiencyCode} SLA due {deficiency.SlaDueDate:yyyy-MM-dd} is breached.",
            HospitalId = deficiency.HospitalId,
            OrganizationId = deficiency.OrganizationId,
            RecipientUserId = deficiency.AssignedTo,
            Data = new Dictionary<string, string>
            {
                ["deficiencyId"] = deficiency.Id.ToString(),
                ["capaNumber"] = deficiency.CapaNumber ?? deficiency.DeficiencyCode ?? string.Empty,
                ["dueDate"] = deficiency.SlaDueDate?.ToString("O") ?? string.Empty,
                ["resourceId"] = deficiency.Id.ToString(),
                ["source"] = "SlaChaseJob",
            },
        }, ct);
    }
}
