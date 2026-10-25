using Shared.Infrastructure.Domain;
using PersonService.Domain.Aggregates;

namespace PersonService.Domain.Entities;

public class PersonContact : BaseEntity
{
    public Guid PersonId { get; set; }
    public string ContactKind { get; set; } = "Email";
    public string Value { get; set; } = string.Empty;
    public string? NormalizedValue { get; set; }
    public Guid? ContactTypeValueId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsDeleted { get; set; }
    public Person? Person { get; set; }
}

public class PersonAddress : BaseEntity
{
    public Guid PersonId { get; set; }
    public Guid? AddressTypeValueId { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsDeleted { get; set; }
    public Person? Person { get; set; }
}

public class PersonDocument : BaseEntity
{
    public Guid PersonId { get; set; }
    public Guid? DocumentTypeValueId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string VerificationStatus { get; set; } = "Pending";
    public string? AttachmentStorageKey { get; set; }
    public string? AttachmentUrl { get; set; }
    public bool IsDeleted { get; set; }
    public Person? Person { get; set; }
}

public class EmergencyContact : BaseEntity
{
    public Guid PersonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? RelationshipValueId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsDeleted { get; set; }
    public Person? Person { get; set; }
}

public class PersonPreferences
{
    public Guid PersonId { get; set; }
    public string Theme { get; set; } = "system";
    public string? LanguageCode { get; set; }
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string TimeFormat { get; set; } = "24h";
    public string? DashboardLayout { get; set; }
    public string? AccessibilityOptions { get; set; }
    public string? NotificationDefaults { get; set; }
    public string? AiPreferences { get; set; }
    public Guid? PreferredContactMethodValueId { get; set; }
    public Person? Person { get; set; }
}

public class PersonTimelineEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PersonId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? CorrelationId { get; set; }
}

public class PersonLookupCategory
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; } = true;
    public ICollection<PersonLookupValue> Values { get; set; } = new List<PersonLookupValue>();
}

public class PersonLookupValue
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? MetadataJson { get; set; }
    public PersonLookupCategory? Category { get; set; }
}

public class PersonAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid PersonId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string EventCode { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? CorrelationId { get; set; }
    public string Outcome { get; set; } = "Success";
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
