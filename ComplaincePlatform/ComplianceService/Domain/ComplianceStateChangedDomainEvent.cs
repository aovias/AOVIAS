using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Domain;

/// <summary>
/// Domain event raised whenever a compliance journey changes lifecycle state.
/// Intended for projections, analytics, and cross-service notifications.
/// </summary>
public class ComplianceStateChangedDomainEvent : IDomainEvent
{
    public Guid TenantId { get; }
    public Guid OrganizationId { get; }
    public Guid HospitalId { get; }
    public Guid FrameworkId { get; }
    public Guid ComplianceJourneyId { get; }
    public ComplianceLifecycleState FromStatus { get; }
    public ComplianceLifecycleState ToStatus { get; }
    public Guid ChangedByUserId { get; }
    public string Role { get; }
    public string? Remarks { get; }
    public DateTime ChangedAtUtc { get; }
    public DateTime OccurredOn => ChangedAtUtc;

    public ComplianceStateChangedDomainEvent(
        Guid tenantId,
        Guid organizationId,
        Guid hospitalId,
        Guid frameworkId,
        Guid complianceJourneyId,
        ComplianceLifecycleState fromStatus,
        ComplianceLifecycleState toStatus,
        Guid changedByUserId,
        string role,
        string? remarks,
        DateTime changedAtUtc)
    {
        TenantId = tenantId;
        OrganizationId = organizationId;
        HospitalId = hospitalId;
        FrameworkId = frameworkId;
        ComplianceJourneyId = complianceJourneyId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        ChangedByUserId = changedByUserId;
        Role = role;
        Remarks = remarks;
        ChangedAtUtc = changedAtUtc;
    }
}

