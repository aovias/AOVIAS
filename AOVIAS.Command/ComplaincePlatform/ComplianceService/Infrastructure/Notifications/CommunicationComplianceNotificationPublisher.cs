using ComplianceService.Application.Contracts;
using Shared.Communication;

namespace ComplianceService.Infrastructure.Notifications;

/// <summary>
/// Publishes compliance domain notifications to CommunicationService via <see cref="ICommunicationClient"/>.
/// </summary>
public sealed class CommunicationComplianceNotificationPublisher : IComplianceNotificationPublisher
{
    private readonly ICommunicationClient _client;
    private readonly ILogger<CommunicationComplianceNotificationPublisher> _logger;

    public CommunicationComplianceNotificationPublisher(
        ICommunicationClient client,
        ILogger<CommunicationComplianceNotificationPublisher> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task PublishAsync(ComplianceNotificationMessage message, CancellationToken cancellationToken = default)
    {
        Guid? personId = null;
        if (!string.IsNullOrWhiteSpace(message.RecipientUserId)
            && Guid.TryParse(message.RecipientUserId, out var parsed)
            && parsed != Guid.Empty)
        {
            personId = parsed;
        }

        if (personId is null)
        {
            _logger.LogInformation(
                "ComplianceNotification skipped HTTP enqueue (no PersonId). EventType={EventType} Title={Title} HospitalId={HospitalId}",
                message.EventType,
                message.Title,
                message.HospitalId);
            return;
        }

        var eventType = NormalizeEventType(message.EventType);
        var templateCode = MapTemplateCode(eventType);
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (message.Data != null)
        {
            foreach (var (key, value) in message.Data)
                variables[key] = value;
        }

        variables["title"] = message.Title;
        variables["message"] = message.Body ?? message.Title;
        if (message.HospitalId.HasValue)
            variables["hospitalId"] = message.HospitalId.Value.ToString();
        if (message.OrganizationId.HasValue)
            variables["organizationId"] = message.OrganizationId.Value.ToString();

        var resourceId = variables.GetValueOrDefault("evidenceId")
            ?? variables.GetValueOrDefault("frameworkId")
            ?? variables.GetValueOrDefault("objectiveElementId")
            ?? variables.GetValueOrDefault("taskId")
            ?? variables.GetValueOrDefault("resourceId")
            ?? Guid.NewGuid().ToString("N");

        try
        {
            var result = await _client.SendAsync(new CommunicationRequestDto
            {
                ModuleCode = "Compliance",
                EventType = eventType,
                PersonId = personId,
                TemplateCode = templateCode,
                Subject = message.Title,
                Body = message.Body,
                Variables = variables,
                PreferredChannels = ["InApp", "Email"],
                DedupeKey = $"{eventType}:{personId}:{resourceId}",
            }, cancellationToken);

            _logger.LogInformation(
                "ComplianceNotification queued via CommunicationService EventType={EventType} OutboundMessageId={OutboundMessageId} Status={Status}",
                eventType,
                result.OutboundMessageId,
                result.Status);
        }
        catch (Exception ex)
        {
            // Non-blocking: domain flow must not fail when CommunicationService is down.
            _logger.LogWarning(ex,
                "ComplianceNotification CommunicationService send failed EventType={EventType} Recipient={Recipient}",
                eventType,
                personId);
        }
    }

    internal static string NormalizeEventType(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType)) return "ComplianceEvent";
        // Convert dotted Phase-1 keys (evidence.approved) to PascalCase subscription keys.
        if (!eventType.Contains('.', StringComparison.Ordinal)) return eventType;
        return string.Concat(eventType.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    internal static string MapTemplateCode(string eventType) => eventType switch
    {
        "EvidenceApproved" => "evidence-approved",
        "EvidenceRejected" => "evidence-rejected",
        "EvidenceNeedsMore" or "EvidenceNeeds_more" => "evidence-rejected",
        "FrameworkActivated" => "framework-activated",
        "OeOwnershipAssigned" or "TaskAssigned" => "task-assigned",
        "CapaOverdue" => "capa-overdue",
        "AuditScheduled" => "audit-scheduled",
        "DocumentReviewDue" => "task-assigned",
        "RiskReviewDue" => "task-assigned",
        _ => "task-assigned",
    };
}
