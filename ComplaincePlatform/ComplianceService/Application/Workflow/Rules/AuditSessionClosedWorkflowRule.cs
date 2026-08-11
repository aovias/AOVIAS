using ComplianceService.Application.Contracts;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Domain.Auditor;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Workflow.Rules;

/// <summary>
/// AuditSessionClosed → auto-promote Accepted findings without CAPA, route assignment, notify.
/// </summary>
public sealed class AuditSessionClosedWorkflowRule : IWorkflowRule
{
    private readonly ComplianceDbContext _db;
    private readonly IFindingPromotionService _promotionService;
    private readonly IAssignmentRoutingService _assignmentRouting;
    private readonly IComplianceNotificationPublisher _notifications;
    private readonly ILogger<AuditSessionClosedWorkflowRule> _logger;

    public AuditSessionClosedWorkflowRule(
        ComplianceDbContext db,
        IFindingPromotionService promotionService,
        IAssignmentRoutingService assignmentRouting,
        IComplianceNotificationPublisher notifications,
        ILogger<AuditSessionClosedWorkflowRule> logger)
    {
        _db = db;
        _promotionService = promotionService;
        _assignmentRouting = assignmentRouting;
        _notifications = notifications;
        _logger = logger;
    }

    public ComplianceEventType EventType => ComplianceEventType.AuditSessionClosed;

    public async Task ExecuteAsync(ComplianceEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        var sessionId = envelope.EntityId;
        var organizationId = envelope.TenantId;

        var session = await _db.Set<AuditSession>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("AuditSessionClosed rule: session {SessionId} not found", sessionId);
            return;
        }

        var hospitalId = session.HospitalId
            ?? await ResolveHospitalFromFindingsAsync(sessionId, cancellationToken)
            ?? Guid.Empty;

        if (hospitalId == Guid.Empty)
        {
            _logger.LogWarning(
                "AuditSessionClosed rule: cannot resolve hospital for session {SessionId}; skipping auto-promote",
                sessionId);
            return;
        }

        var acceptedOpen = await _db.Findings
            .Where(f => f.AuditSessionId == sessionId
                        && f.Status == FindingStatus.Accepted
                        && f.PromotedDeficiencyId == null
                        && !f.IsResolved)
            .OrderBy(f => f.DateRaised)
            .ToListAsync(cancellationToken);

        if (acceptedOpen.Count == 0)
        {
            _logger.LogInformation(
                "AuditSessionClosed rule: no Accepted findings without CAPA for session {SessionId}",
                sessionId);
            return;
        }

        var promoted = 0;
        foreach (var finding in acceptedOpen)
        {
            try
            {
                var result = await _promotionService.PromoteAsync(
                    organizationId,
                    finding.Id,
                    hospitalId,
                    envelope.UserId,
                    cancellationToken,
                    allowAdditional: false);

                promoted++;

                try
                {
                    await _assignmentRouting.TryAutoAssignDeficiencyAsync(
                        result.DeficiencyId,
                        forceAutoAssign: false,
                        updatedBy: "Workflow:AuditSessionClosed",
                        ct: cancellationToken);
                }
                catch (Exception routeEx)
                {
                    _logger.LogWarning(
                        routeEx,
                        "AssignmentRouting failed for deficiency {DeficiencyId} from finding {FindingId}",
                        result.DeficiencyId,
                        finding.Id);
                }

                await _notifications.PublishAsync(new ComplianceNotificationMessage
                {
                    EventType = "CapaCreated",
                    Title = "CAPA created from closed audit",
                    Body = $"Finding {finding.FindingNumber} auto-promoted to CAPA {result.DeficiencyCode} after audit session close.",
                    HospitalId = hospitalId,
                    OrganizationId = organizationId,
                    RecipientUserId = finding.AssignedToUserId?.ToString(),
                    Data = new Dictionary<string, string>
                    {
                        ["findingId"] = finding.Id.ToString(),
                        ["deficiencyId"] = result.DeficiencyId.ToString(),
                        ["capaNumber"] = result.DeficiencyCode ?? string.Empty,
                        ["auditSessionId"] = sessionId.ToString(),
                        ["resourceId"] = result.DeficiencyId.ToString(),
                        ["source"] = "WorkflowRuleEngine",
                    },
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to auto-promote Accepted finding {FindingId} for session {SessionId}",
                    finding.Id,
                    sessionId);
            }
        }

        _logger.LogInformation(
            "AuditSessionClosed rule: promoted {Promoted}/{Total} Accepted findings for session {SessionId}",
            promoted,
            acceptedOpen.Count,
            sessionId);
    }

    private async Task<Guid?> ResolveHospitalFromFindingsAsync(Guid sessionId, CancellationToken ct)
    {
        var journeyId = await _db.Findings.AsNoTracking()
            .Where(f => f.AuditSessionId == sessionId && f.ComplianceJourneyId != null)
            .Select(f => f.ComplianceJourneyId)
            .FirstOrDefaultAsync(ct);

        if (journeyId == null)
        {
            return null;
        }

        return await _db.ComplianceJourneys.AsNoTracking()
            .Where(j => j.Id == journeyId.Value)
            .Select(j => (Guid?)j.HospitalId)
            .FirstOrDefaultAsync(ct);
    }
}
