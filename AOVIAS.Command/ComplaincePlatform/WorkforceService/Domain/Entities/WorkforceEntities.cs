using WorkforceService.Domain.Aggregates;
using Shared.Infrastructure.Domain;

namespace WorkforceService.Domain.Entities;

public class PositionAssignment : BaseEntity
{
    public Guid EmploymentId { get; set; }
    public Guid PositionId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsDeleted { get; set; }
    public Employment? Employment { get; set; }
    public Position? Position { get; set; }

    public bool IsActive(DateTime? asOf = null)
    {
        var at = asOf ?? DateTime.UtcNow;
        return !IsDeleted && EffectiveFrom <= at && (EffectiveTo == null || EffectiveTo > at);
    }
}

public class DepartmentAssignment : BaseEntity
{
    public Guid EmploymentId { get; set; }
    public Guid MasterDepartmentId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsPrimary { get; set; }
    public decimal AllocationPercentage { get; set; } = 100;
    public bool IsTemporary { get; set; }
    public bool IsDeleted { get; set; }
    public Employment? Employment { get; set; }

    public bool IsActive(DateTime? asOf = null)
    {
        var at = asOf ?? DateTime.UtcNow;
        return !IsDeleted && EffectiveFrom <= at && (EffectiveTo == null || EffectiveTo > at);
    }
}

public class ReportingRelationship : BaseEntity
{
    public Guid EmploymentId { get; set; }
    public Guid ManagerEmploymentId { get; set; }
    public Guid? RelationshipTypeValueId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsDeleted { get; set; }
    public Employment? Employment { get; set; }

    public bool IsActive(DateTime? asOf = null)
    {
        var at = asOf ?? DateTime.UtcNow;
        return !IsDeleted && EffectiveFrom <= at && (EffectiveTo == null || EffectiveTo > at);
    }
}

public class EmploymentContract : BaseEntity
{
    public Guid EmploymentId { get; set; }
    public Guid? ContractTypeValueId { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? RenewalDate { get; set; }
    public string Status { get; set; } = "Active";
    public string? AttachmentUrl { get; set; }
    public bool IsDeleted { get; set; }
    public Employment? Employment { get; set; }
}

public class AvailabilityProfile
{
    public Guid EmploymentId { get; set; }
    public string? WorkingDaysJson { get; set; }
    public string? WorkingHoursJson { get; set; }
    public string? ShiftPlaceholder { get; set; }
    public string? LeaveStatus { get; set; }
    public Employment? Employment { get; set; }
}

public class EmploymentHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmploymentId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? PreviousValueJson { get; set; }
    public string? NewValueJson { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class WorkforceTimelineEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmploymentId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? CorrelationId { get; set; }
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public DateTime OccurredOn { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedOn { get; set; }
    public DateTime? RelayProcessedOn { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
public class TransferRecord : BaseEntity
{
    public Guid EmploymentId { get; set; }
    public string TransferKind { get; set; } = string.Empty;
    public Guid? TargetOrganizationId { get; set; }
    public Guid? TargetHospitalId { get; set; }
    public Guid? TargetMasterDepartmentId { get; set; }
    public Guid? TargetPositionId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public Guid? ReasonValueId { get; set; }
    public string? Notes { get; set; }
    public Employment? Employment { get; set; }
}

public class PromotionRecord : BaseEntity
{
    public Guid EmploymentId { get; set; }
    public Guid? PreviousPositionId { get; set; }
    public Guid NewPositionId { get; set; }
    public DateTime EffectiveDate { get; set; }
    public Guid? ReasonValueId { get; set; }
    public string? Notes { get; set; }
    public string? SalaryPlaceholder { get; set; }
    public Guid? GradeValueId { get; set; }
    public Guid? BandValueId { get; set; }
    public Employment? Employment { get; set; }
}

public class WorkforceLookupCategory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public ICollection<WorkforceLookupValue> Values { get; set; } = new List<WorkforceLookupValue>();
}

public class WorkforceLookupValue
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? MetadataJson { get; set; }
    public WorkforceLookupCategory? Category { get; set; }
}

public class WorkforceAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid EmploymentId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid? ActorUserId { get; set; }
    public string EventCode { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? CorrelationId { get; set; }
}
