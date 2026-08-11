namespace PersonService.Application.DTOs;

public class PersonDto
{
    public Guid Id { get; set; }
    public string PlatformId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string SearchName { get; set; } = string.Empty;
    public string? LegalFirstName { get; set; }
    public string? LegalLastName { get; set; }
    public string? DisplayName { get; set; }
    public string? PreferredName { get; set; }
    public string? ShortName { get; set; }
    public string? Initials { get; set; }
    public string? MiddleName { get; set; }
    public string? LocalLanguageName { get; set; }
    public Guid? PrefixValueId { get; set; }
    public Guid? SuffixValueId { get; set; }
    public Guid? GenderValueId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Guid? NationalityValueId { get; set; }
    public Guid? MaritalStatusValueId { get; set; }
    public Guid? BloodGroupValueId { get; set; }
    public Guid? PreferredLanguageValueId { get; set; }
    public string? TimeZone { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<PersonContactDto> Contacts { get; set; } = new();
    public List<PersonAddressDto> Addresses { get; set; } = new();
    public List<PersonDocumentDto> Documents { get; set; } = new();
    public List<EmergencyContactDto> EmergencyContacts { get; set; } = new();
    public PersonPreferencesDto? Preferences { get; set; }
}

public class PersonListItemDto
{
    public Guid Id { get; set; }
    public string PlatformId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? PrimaryEmail { get; set; }
    public string? PrimaryPhone { get; set; }
    public string? PhotoUrl { get; set; }
}

public class PersonContactDto
{
    public Guid Id { get; set; }
    public string ContactKind { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public Guid? ContactTypeValueId { get; set; }
}

public class PersonAddressDto
{
    public Guid Id { get; set; }
    public Guid? AddressTypeValueId { get; set; }
    public string Line1 { get; set; } = string.Empty;
    public string? Line2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public bool IsPrimary { get; set; }
}

public class PersonDocumentDto
{
    public Guid Id { get; set; }
    public Guid? DocumentTypeValueId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
}

public class EmergencyContactDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? RelationshipValueId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

public class PersonPreferencesDto
{
    public string Theme { get; set; } = "system";
    public string? LanguageCode { get; set; }
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public string TimeFormat { get; set; } = "24h";
    public string? DashboardLayout { get; set; }
    public string? AccessibilityOptions { get; set; }
    public string? NotificationDefaults { get; set; }
    public string? AiPreferences { get; set; }
    public Guid? PreferredContactMethodValueId { get; set; }
}

public class PersonTimelineDto
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
}

public class LookupValueDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class CreatePersonRequest
{
    public string? LegalFirstName { get; set; }
    public string? LegalLastName { get; set; }
    public string? DisplayName { get; set; }
    public string? PreferredName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? GenderValueId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Guid? CreatedByUserId { get; set; }
}

public class UpdatePersonRequest : CreatePersonRequest
{
    public string? MiddleName { get; set; }
    public string? ShortName { get; set; }
    public string? Initials { get; set; }
    public string? LocalLanguageName { get; set; }
    public Guid? PrefixValueId { get; set; }
    public Guid? SuffixValueId { get; set; }
    public Guid? NationalityValueId { get; set; }
    public Guid? MaritalStatusValueId { get; set; }
    public Guid? BloodGroupValueId { get; set; }
    public Guid? PreferredLanguageValueId { get; set; }
    public string? TimeZone { get; set; }
    public PersonPreferencesDto? Preferences { get; set; }
}

public class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class InternalCreatePersonResponse
{
    public Guid PersonId { get; set; }
    public string PlatformId { get; set; } = string.Empty;
}
