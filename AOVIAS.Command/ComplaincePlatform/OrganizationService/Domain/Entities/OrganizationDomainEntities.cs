namespace OrganizationService.Domain.Entities;

public class OrganizationalUnit
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid? ParentUnitId { get; set; }
    public Guid UnitTypeValueId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class OrganizationMembershipHistory
{
    public Guid Id { get; set; }
    public Guid MembershipId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public Guid ActorUserId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class OrganizationTimelineEntry
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public Guid? ResourceId { get; set; }
    public string? ResourceType { get; set; }
    public Guid? ActorUserId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string PayloadJson { get; set; } = "{}";
}

public class OrganizationAuditLog
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime OccurredAt { get; set; }
}

/// <summary>A configuration domain — e.g. <c>AuditType</c>, <c>Severity</c>, <c>RiskCategory</c>.</summary>
public class OrganizationLookupCategory
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>False for categories only the platform may change (e.g. system enumerations).</summary>
    public bool IsTenantConfigurable { get; set; } = true;

    /// <summary>True when the platform owns the values and tenant overrides are not accepted.</summary>
    public bool IsSystemManaged { get; set; }

    public ICollection<OrganizationLookupValue> Values { get; set; } = new List<OrganizationLookupValue>();
}

/// <summary>
/// A configurable value within a configuration category (Stage 5B.1).
///
/// <para>
/// <see cref="OrganizationId"/> is the pivot: <c>null</c> marks a platform default visible to every
/// tenant, while a value marks that organization's override of the same <see cref="Code"/>.
/// Resolution prefers the override and falls back to the default, so an organization that has
/// configured nothing still receives a complete catalog.
/// </para>
/// </summary>
public class OrganizationLookupValue
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }

    /// <summary>Null = platform default; set = this organization's override.</summary>
    public Guid? OrganizationId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    /// <summary>Retire a value without deleting it and orphaning historical records.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Scheduled activation, e.g. a severity scale that changes at financial year end.</summary>
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    /// <summary>Hierarchy, e.g. risk category → sub-category.</summary>
    public Guid? ParentValueId { get; set; }

    /// <summary>
    /// Domain-specific attributes as JSON, typed by the consumer. Keeps a new attribute from
    /// requiring a migration — the point of a metadata-driven store.
    /// </summary>
    public string? Metadata { get; set; }

    public OrganizationLookupCategory? Category { get; set; }
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public DateTime OccurredOn { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedOn { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}
