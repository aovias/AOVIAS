using PersonService.Domain.Entities;
using PersonService.Domain.Enums;
using PersonService.Domain.ValueObjects;
using Shared.Infrastructure.Domain;

namespace PersonService.Domain.Aggregates;

public class Person : BaseEntity
{
    public string PlatformId { get; set; } = string.Empty;
    public PersonStatus Status { get; set; } = PersonStatus.Active;
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

    public string? PhotoStorageKey { get; set; }
    public string? PhotoUrl { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<PersonContact> Contacts { get; set; } = new List<PersonContact>();
    public ICollection<PersonAddress> Addresses { get; set; } = new List<PersonAddress>();
    public ICollection<PersonDocument> Documents { get; set; } = new List<PersonDocument>();
    public ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();
    public PersonPreferences? Preferences { get; set; }

    public void RecomputeSearchName()
    {
        SearchName = PersonNameHelper.ComputeSearchName(LegalFirstName, LegalLastName, DisplayName, PreferredName);
    }

    public void SoftDelete(Guid? actorUserId)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        Status = PersonStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = actorUserId;
    }

    public bool CanMutate() =>
        !IsDeleted && Status is not PersonStatus.Deceased and not PersonStatus.Merged and not PersonStatus.Duplicate;
}
