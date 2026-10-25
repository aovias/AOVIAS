using ComplianceService.Application.Contracts;

namespace ComplianceService.Infrastructure.Notifications;

/// <summary>
/// Fallback publisher when CommunicationService:BaseUrl / Communication:BaseUrl is not configured.
/// Logs structured notification events via ILogger.
/// </summary>
public sealed class ConsoleComplianceNotificationPublisher : IComplianceNotificationPublisher
{
    private readonly ILogger<ConsoleComplianceNotificationPublisher> _logger;

    public ConsoleComplianceNotificationPublisher(ILogger<ConsoleComplianceNotificationPublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync(ComplianceNotificationMessage message, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "ComplianceNotification EventType={EventType} Title={Title} Recipient={Recipient} HospitalId={HospitalId} Body={Body}",
            message.EventType,
            message.Title,
            message.RecipientUserId ?? "(broadcast)",
            message.HospitalId,
            message.Body);

        return Task.CompletedTask;
    }
}
