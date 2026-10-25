using System.Text.Json;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Dashboard.Summary;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ComplianceService.Application.Workflow.Rules;

/// <summary>
/// EvidenceApproved → notification + dashboard/projection refresh hooks.
/// Complements existing EvidenceReviewService processors without replacing them.
/// </summary>
public sealed class EvidenceApprovedWorkflowRule : IWorkflowRule
{
    private readonly ComplianceDbContext _db;
    private readonly IComplianceNotificationPublisher _notifications;
    private readonly IComplianceSnapshotRecalculationQueue _snapshotQueue;
    private readonly IDashboardSummaryCache _dashboardSummaryCache;
    private readonly ILogger<EvidenceApprovedWorkflowRule> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public EvidenceApprovedWorkflowRule(
        ComplianceDbContext db,
        IComplianceNotificationPublisher notifications,
        IComplianceSnapshotRecalculationQueue snapshotQueue,
        IDashboardSummaryCache dashboardSummaryCache,
        ILogger<EvidenceApprovedWorkflowRule> logger)
    {
        _db = db;
        _notifications = notifications;
        _snapshotQueue = snapshotQueue;
        _dashboardSummaryCache = dashboardSummaryCache;
        _logger = logger;
    }

    public ComplianceEventType EventType => ComplianceEventType.EvidenceApproved;

    public async Task ExecuteAsync(ComplianceEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var payload = ParsePayload(envelope.Payload);
        var evidenceId = payload.EvidenceId ?? envelope.EntityId;
        var hospitalId = envelope.TenantId;

        var evidence = await _db.Set<Evidence>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken);

        if (evidence != null)
        {
            hospitalId = evidence.HospitalId;
            _snapshotQueue.EnqueueHospitalFramework(evidence.HospitalId, evidence.FrameworkId);
            _dashboardSummaryCache.Invalidate(envelope.TenantId, evidence.HospitalId, evidence.FrameworkId);
            _dashboardSummaryCache.Invalidate(evidence.HospitalId, evidence.HospitalId, evidence.FrameworkId);
        }
        else if (payload.ObjectiveElementIds is { Count: > 0 })
        {
            // Soft refresh for tenant scope when evidence row is not resolvable.
            _dashboardSummaryCache.Invalidate(envelope.TenantId);
        }

        string? recipient = null;
        if (evidence?.TaskId is Guid taskId && taskId != Guid.Empty)
        {
            recipient = await _db.ComplianceTasks.AsNoTracking()
                .Where(t => t.Id == taskId)
                .Select(t => t.AssignedToUserId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        await _notifications.PublishAsync(new ComplianceNotificationMessage
        {
            EventType = "EvidenceApproved",
            Title = "Evidence approved",
            Body = $"Evidence {evidenceId} was approved.",
            HospitalId = hospitalId,
            OrganizationId = envelope.TenantId,
            RecipientUserId = recipient,
            Data = new Dictionary<string, string>
            {
                ["evidenceId"] = evidenceId.ToString(),
                ["resourceId"] = evidenceId.ToString(),
                ["frameworkId"] = evidence?.FrameworkId.ToString() ?? string.Empty,
                ["source"] = "WorkflowRuleEngine",
            },
        }, cancellationToken);

        _logger.LogInformation(
            "Workflow EvidenceApproved handled for Evidence {EvidenceId} Hospital {HospitalId}",
            evidenceId,
            hospitalId);
    }

    private static EvidenceApprovedPayload ParsePayload(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return new EvidenceApprovedPayload();
        }

        try
        {
            return JsonSerializer.Deserialize<EvidenceApprovedPayload>(payloadJson, JsonOptions)
                   ?? new EvidenceApprovedPayload();
        }
        catch
        {
            return new EvidenceApprovedPayload();
        }
    }

    private sealed class EvidenceApprovedPayload
    {
        public Guid? EvidenceId { get; set; }
        public Guid? TaskId { get; set; }
        public List<Guid>? ObjectiveElementIds { get; set; }
    }
}
