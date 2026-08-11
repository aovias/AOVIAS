namespace OrganizationService.Domain.Entities;

/// <summary>CRP Phase 3 — durable onboarding funnel telemetry.</summary>
public sealed class OnboardingTelemetryEvent
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? HospitalId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string? Stage { get; set; }
    public string? CorrelationId { get; set; }
    public string? PayloadJson { get; set; }
    public DateTime OccurredAtUtc { get; set; }
}
