using OrganizationService.Domain.Entities;
using OrganizationService.Infrastructure.Persistence;

namespace OrganizationService.Application.Services;

public sealed class OnboardingTelemetryAppService
{
    private readonly OrganizationDbContext _db;
    private readonly ILogger<OnboardingTelemetryAppService> _logger;

    public OnboardingTelemetryAppService(OrganizationDbContext db, ILogger<OnboardingTelemetryAppService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<OnboardingTelemetryEvent> RecordAsync(
        Guid organizationId,
        Guid? userId,
        Guid? hospitalId,
        string eventType,
        string? stage,
        string? correlationId,
        string? payloadJson,
        CancellationToken cancellationToken = default)
    {
        if (organizationId == Guid.Empty)
        {
            throw new InvalidOperationException("OrganizationId is required.");
        }

        if (string.IsNullOrWhiteSpace(eventType))
        {
            throw new InvalidOperationException("EventType is required.");
        }

        var entity = new OnboardingTelemetryEvent
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            HospitalId = hospitalId,
            EventType = eventType.Trim(),
            Stage = stage,
            CorrelationId = correlationId,
            PayloadJson = payloadJson,
            OccurredAtUtc = DateTime.UtcNow
        };

        _db.OnboardingTelemetryEvents.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "OnboardingTelemetry {EventType} Org={OrganizationId} Stage={Stage} CorrelationId={CorrelationId}",
            entity.EventType,
            entity.OrganizationId,
            entity.Stage,
            entity.CorrelationId);
        return entity;
    }
}
