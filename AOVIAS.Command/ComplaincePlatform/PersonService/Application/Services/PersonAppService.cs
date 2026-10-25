using PersonService.Application.Contracts;
using PersonService.Application.DTOs;
using PersonService.Domain.Aggregates;
using PersonService.Domain.Entities;
using PersonService.Domain.Enums;
using PersonService.Domain.ValueObjects;
using PersonService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.DTO;
using Shared.Infrastructure.Development;
using Shared.Infrastructure.Services;
using Shared.Logging;
using Shared.Logging.Audit;

namespace PersonService.Application.Services;

public class PersonAuditService : IPersonAuditService
{
    private readonly PersonDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public PersonAuditService(PersonDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    public async Task RecordAsync(Guid personId, string eventCode, string action, Guid? actorUserId, string? oldValue, string? newValue, string? correlationId, CancellationToken cancellationToken = default)
    {
        _db.PersonAuditLogs.Add(new PersonAuditLog
        {
            PersonId = personId,
            ActorUserId = actorUserId,
            EventCode = eventCode,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            CorrelationId = correlationId
        });
        await _db.SaveChangesAsync(cancellationToken);
        _auditLogger.Success(eventCode, action, targetType: "Person", targetId: personId.ToString(), actorId: actorUserId?.ToString());
    }

    public async Task RecordTimelineAsync(Guid personId, string eventType, string summary, Guid? actorUserId, string? oldJson, string? newJson, string? correlationId, CancellationToken cancellationToken = default)
    {
        _db.PersonTimeline.Add(new PersonTimelineEntry
        {
            PersonId = personId,
            EventType = eventType,
            Summary = summary,
            ActorUserId = actorUserId,
            OldValuesJson = oldJson,
            NewValuesJson = newJson,
            CorrelationId = correlationId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class PersonAppService : IPersonAppService
{
    private readonly PersonDbContext _db;
    private readonly IPlatformIdGenerator _platformIdGenerator;
    private readonly IPersonAuditService _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _env;

    public PersonAppService(
        PersonDbContext db,
        IPlatformIdGenerator platformIdGenerator,
        IPersonAuditService audit,
        IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment env)
    {
        _db = db;
        _platformIdGenerator = platformIdGenerator;
        _audit = audit;
        _httpContextAccessor = httpContextAccessor;
        _env = env;
    }

    public async Task<InternalCreatePersonResponse> CreateAsync(CreatePersonRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.Email) && await HasDuplicateEmailAsync(request.Email, null, cancellationToken))
        {
            if (NonProductionEnvironment.AllowDevelopmentSeeding(_env))
            {
                var existing = await _db.Persons
                    .AsNoTracking()
                    .Include(p => p.Contacts)
                    .FirstOrDefaultAsync(
                        p => !p.IsDeleted && p.Contacts.Any(c => !c.IsDeleted && c.ContactKind == "Email" && c.Value == request.Email),
                        cancellationToken);

                if (existing != null)
                {
                    return new InternalCreatePersonResponse
                    {
                        PersonId = existing.Id,
                        PlatformId = existing.PlatformId,
                    };
                }
            }

            throw new InvalidOperationException("A person with this email already exists.");
        }

        var person = new Person
        {
            Id = Guid.NewGuid(),
            PlatformId = await _platformIdGenerator.GenerateAsync("PRS", cancellationToken),
            Status = PersonStatus.Active,
            LegalFirstName = request.LegalFirstName,
            LegalLastName = request.LegalLastName,
            DisplayName = request.DisplayName ?? BuildDisplayName(request.LegalFirstName, request.LegalLastName),
            PreferredName = request.PreferredName,
            GenderValueId = request.GenderValueId,
            DateOfBirth = request.DateOfBirth,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = request.CreatedByUserId,
            Preferences = new PersonPreferences()
        };
        person.RecomputeSearchName();

        if (!string.IsNullOrWhiteSpace(request.Email))
            AddContact(person, "Email", request.Email, isPrimary: true);
        if (!string.IsNullOrWhiteSpace(request.Phone))
            AddContact(person, "Phone", request.Phone, isPrimary: true);

        _db.Persons.Add(person);
        _db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = person.Id,
            OccurredOn = DateTime.UtcNow,
            Type = "PersonCreated",
            Payload = $"{{\"personId\":\"{person.Id}\"}}"
        });
        await _db.SaveChangesAsync(cancellationToken);

        var correlationId = GetCorrelationId();
        await _audit.RecordAsync(person.Id, "PERSON_CREATED", "CreatePerson", request.CreatedByUserId, null, person.PlatformId, correlationId, cancellationToken);
        await _audit.RecordTimelineAsync(person.Id, "Created", "Person created", request.CreatedByUserId, null, person.DisplayName, correlationId, cancellationToken);

        return new InternalCreatePersonResponse { PersonId = person.Id, PlatformId = person.PlatformId };
    }

    public async Task<IntegrationUpsertResponse> UpsertFromIntegrationAsync(IntegrationUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var email = GetField(request.Fields, "email", "Email");
        var displayName = GetField(request.Fields, "displayName", "DisplayName");
        var firstName = GetField(request.Fields, "legalFirstName", "LegalFirstName", "firstName", "FirstName");
        var lastName = GetField(request.Fields, "legalLastName", "LegalLastName", "lastName", "LastName");

        Person? person = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            person = await _db.Persons
                .Include(p => p.Contacts)
                .FirstOrDefaultAsync(p => !p.IsDeleted && p.Contacts.Any(c => !c.IsDeleted && c.ContactKind == "Email" && c.Value == email), cancellationToken);
        }

        if (person is null)
        {
            var create = await CreateAsync(new CreatePersonRequest
            {
                LegalFirstName = firstName,
                LegalLastName = lastName,
                DisplayName = displayName,
                Email = email,
                Phone = GetField(request.Fields, "phone", "Phone"),
                PreferredName = GetField(request.Fields, "preferredName", "PreferredName")
            }, cancellationToken);
            return new IntegrationUpsertResponse { EntityId = create.PersonId, WasCreated = true };
        }

        person.LegalFirstName = firstName ?? person.LegalFirstName;
        person.LegalLastName = lastName ?? person.LegalLastName;
        person.DisplayName = displayName ?? person.DisplayName;
        person.PreferredName = GetField(request.Fields, "preferredName", "PreferredName") ?? person.PreferredName;
        person.RecomputeSearchName();
        person.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(email))
        {
            var contact = person.Contacts.FirstOrDefault(c => !c.IsDeleted && c.ContactKind == "Email" && c.IsPrimary);
            if (contact is null) AddContact(person, "Email", email, isPrimary: true);
            else contact.Value = email;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(person.Id, "PERSON_INTEGRATION_UPSERT", "IntegrationUpsert", null, null, person.DisplayName, GetCorrelationId(), cancellationToken);
        return new IntegrationUpsertResponse { EntityId = person.Id, WasCreated = false };
    }

    public async Task<bool> RollbackIntegrationAsync(Guid organizationId, Guid personId, CancellationToken cancellationToken = default)
    {
        _ = organizationId;
        return await DeleteAsync(personId, null, cancellationToken);
    }

    private static string? GetField(Dictionary<string, string?> fields, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (fields.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        return null;
    }

    public async Task<PersonDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var person = await _db.Persons
            .AsNoTracking()
            .Include(p => p.Contacts.Where(c => !c.IsDeleted))
            .Include(p => p.Addresses.Where(a => !a.IsDeleted))
            .Include(p => p.Documents.Where(d => !d.IsDeleted))
            .Include(p => p.EmergencyContacts.Where(e => !e.IsDeleted))
            .Include(p => p.Preferences)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        return person == null ? null : MapToDto(person);
    }

    public async Task<PagedResultDto<PersonListItemDto>> ListAsync(int page, int pageSize, string? status, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = _db.Persons.AsNoTracking().Where(p => !p.IsDeleted);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PersonStatus>(status, true, out var st))
            query = query.Where(p => p.Status == st);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(p => p.SearchName)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new PersonListItemDto
            {
                Id = p.Id,
                PlatformId = p.PlatformId,
                DisplayName = p.DisplayName ?? p.SearchName,
                Status = p.Status.ToString(),
                PhotoUrl = p.PhotoUrl,
                PrimaryEmail = p.Contacts.Where(c => !c.IsDeleted && c.ContactKind == "Email" && c.IsPrimary).Select(c => c.Value).FirstOrDefault(),
                PrimaryPhone = p.Contacts.Where(c => !c.IsDeleted && c.ContactKind == "Phone" && c.IsPrimary).Select(c => c.Value).FirstOrDefault()
            }).ToListAsync(cancellationToken);

        return new PagedResultDto<PersonListItemDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<PagedResultDto<PersonListItemDto>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var term = query.Trim().ToUpperInvariant();
        var q = _db.Persons.AsNoTracking().Where(p => !p.IsDeleted && (
            p.SearchName.Contains(term) ||
            p.PlatformId.Contains(term) ||
            p.Contacts.Any(c => !c.IsDeleted && c.NormalizedValue != null && c.NormalizedValue.Contains(term)) ||
            p.Documents.Any(d => !d.IsDeleted && d.DocumentNumber.Contains(term))));

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderBy(p => p.SearchName).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new PersonListItemDto
            {
                Id = p.Id,
                PlatformId = p.PlatformId,
                DisplayName = p.DisplayName ?? p.SearchName,
                Status = p.Status.ToString(),
                PhotoUrl = p.PhotoUrl,
                PrimaryEmail = p.Contacts.Where(c => !c.IsDeleted && c.ContactKind == "Email" && c.IsPrimary).Select(c => c.Value).FirstOrDefault(),
                PrimaryPhone = p.Contacts.Where(c => !c.IsDeleted && c.ContactKind == "Phone" && c.IsPrimary).Select(c => c.Value).FirstOrDefault()
            }).ToListAsync(cancellationToken);

        return new PagedResultDto<PersonListItemDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<PersonDto?> UpdateAsync(Guid id, UpdatePersonRequest request, CancellationToken cancellationToken = default)
    {
        var person = await _db.Persons.Include(p => p.Preferences).FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (person == null || !person.CanMutate()) return null;

        if (!string.IsNullOrWhiteSpace(request.Email) && await HasDuplicateEmailAsync(request.Email, id, cancellationToken))
            throw new InvalidOperationException("A person with this email already exists.");

        person.LegalFirstName = request.LegalFirstName ?? person.LegalFirstName;
        person.LegalLastName = request.LegalLastName ?? person.LegalLastName;
        person.DisplayName = request.DisplayName ?? person.DisplayName;
        person.PreferredName = request.PreferredName ?? person.PreferredName;
        person.MiddleName = request.MiddleName ?? person.MiddleName;
        person.ShortName = request.ShortName ?? person.ShortName;
        person.Initials = request.Initials ?? person.Initials;
        person.LocalLanguageName = request.LocalLanguageName ?? person.LocalLanguageName;
        person.PrefixValueId = request.PrefixValueId ?? person.PrefixValueId;
        person.SuffixValueId = request.SuffixValueId ?? person.SuffixValueId;
        person.GenderValueId = request.GenderValueId ?? person.GenderValueId;
        person.DateOfBirth = request.DateOfBirth ?? person.DateOfBirth;
        person.NationalityValueId = request.NationalityValueId ?? person.NationalityValueId;
        person.MaritalStatusValueId = request.MaritalStatusValueId ?? person.MaritalStatusValueId;
        person.BloodGroupValueId = request.BloodGroupValueId ?? person.BloodGroupValueId;
        person.PreferredLanguageValueId = request.PreferredLanguageValueId ?? person.PreferredLanguageValueId;
        person.TimeZone = request.TimeZone ?? person.TimeZone;
        person.UpdatedAt = DateTime.UtcNow;
        person.UpdatedByUserId = request.CreatedByUserId;
        person.RecomputeSearchName();

        if (request.Preferences != null && person.Preferences != null)
        {
            person.Preferences.Theme = request.Preferences.Theme;
            person.Preferences.LanguageCode = request.Preferences.LanguageCode;
            person.Preferences.DateFormat = request.Preferences.DateFormat;
            person.Preferences.TimeFormat = request.Preferences.TimeFormat;
            person.Preferences.DashboardLayout = request.Preferences.DashboardLayout;
            person.Preferences.AccessibilityOptions = request.Preferences.AccessibilityOptions;
            person.Preferences.NotificationDefaults = request.Preferences.NotificationDefaults;
            person.Preferences.AiPreferences = request.Preferences.AiPreferences;
            person.Preferences.PreferredContactMethodValueId = request.Preferences.PreferredContactMethodValueId;
        }

        await _db.SaveChangesAsync(cancellationToken);
        var correlationId = GetCorrelationId();
        await _audit.RecordAsync(id, "PERSON_UPDATED", "UpdatePerson", request.CreatedByUserId, null, person.DisplayName, correlationId, cancellationToken);
        await _audit.RecordTimelineAsync(id, "Updated", "Person profile updated", request.CreatedByUserId, null, null, correlationId, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        var person = await _db.Persons.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (person == null) return false;
        person.SoftDelete(actorUserId);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(id, "PERSON_DELETED", "DeletePerson", actorUserId, null, null, GetCorrelationId(), cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<PersonContactDto>> GetContactsAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await _db.PersonContacts.AsNoTracking()
            .Where(c => c.PersonId == personId && !c.IsDeleted)
            .Select(c => new PersonContactDto { Id = c.Id, ContactKind = c.ContactKind, Value = c.Value, IsPrimary = c.IsPrimary, ContactTypeValueId = c.ContactTypeValueId })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PersonAddressDto>> GetAddressesAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await _db.PersonAddresses.AsNoTracking()
            .Where(a => a.PersonId == personId && !a.IsDeleted)
            .Select(a => new PersonAddressDto
            {
                Id = a.Id, AddressTypeValueId = a.AddressTypeValueId, Line1 = a.Line1, Line2 = a.Line2,
                City = a.City, State = a.State, PostalCode = a.PostalCode, CountryCode = a.CountryCode, IsPrimary = a.IsPrimary
            }).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PersonTimelineDto>> GetTimelineAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        return await _db.PersonTimeline.AsNoTracking()
            .Where(t => t.PersonId == personId)
            .OrderByDescending(t => t.OccurredAt)
            .Select(t => new PersonTimelineDto { Id = t.Id, OccurredAt = t.OccurredAt, EventType = t.EventType, Summary = t.Summary, ActorUserId = t.ActorUserId })
            .ToListAsync(cancellationToken);
    }

    public async Task<PersonDocumentDto?> AddDocumentAsync(Guid personId, PersonDocumentDto request, CancellationToken cancellationToken = default)
    {
        var person = await _db.Persons.FirstOrDefaultAsync(p => p.Id == personId && !p.IsDeleted, cancellationToken);
        if (person == null || !person.CanMutate()) return null;

        var exists = await _db.PersonDocuments.AnyAsync(d =>
            !d.IsDeleted && d.DocumentNumber == request.DocumentNumber && d.DocumentTypeValueId == request.DocumentTypeValueId && d.PersonId != personId, cancellationToken);
        if (exists) throw new InvalidOperationException("Document number already registered to another person.");

        var doc = new PersonDocument
        {
            Id = Guid.NewGuid(),
            PersonId = personId,
            DocumentTypeValueId = request.DocumentTypeValueId,
            DocumentNumber = request.DocumentNumber,
            Issuer = request.Issuer,
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            VerificationStatus = request.VerificationStatus ?? "Pending",
            CreatedAt = DateTime.UtcNow
        };
        _db.PersonDocuments.Add(doc);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordTimelineAsync(personId, "DocumentAdded", $"Document {request.DocumentNumber} added", null, null, null, GetCorrelationId(), cancellationToken);
        return new PersonDocumentDto { Id = doc.Id, DocumentTypeValueId = doc.DocumentTypeValueId, DocumentNumber = doc.DocumentNumber, Issuer = doc.Issuer, IssueDate = doc.IssueDate, ExpiryDate = doc.ExpiryDate, VerificationStatus = doc.VerificationStatus };
    }

    public async Task<bool> RemoveDocumentAsync(Guid personId, Guid documentId, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        var doc = await _db.PersonDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.PersonId == personId && !d.IsDeleted, cancellationToken);
        if (doc == null) return false;
        doc.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UploadPhotoAsync(Guid personId, Stream fileStream, string fileName, string contentType, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        var person = await _db.Persons.FirstOrDefaultAsync(p => p.Id == personId && !p.IsDeleted, cancellationToken);
        if (person == null || !person.CanMutate()) return false;

        var dir = Path.Combine(_env.ContentRootPath, "App_Data", "person_photos", personId.ToString("N"));
        Directory.CreateDirectory(dir);
        var safeName = Path.GetFileName(fileName);
        var path = Path.Combine(dir, safeName);
        await using var fs = File.Create(path);
        await fileStream.CopyToAsync(fs, cancellationToken);

        person.PhotoStorageKey = path;
        person.PhotoUrl = $"/person-photos/{personId:N}/{safeName}";
        person.UpdatedAt = DateTime.UtcNow;
        person.UpdatedByUserId = actorUserId;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<LookupValueDto>> GetLookupsAsync(string categoryCode, CancellationToken cancellationToken = default)
    {
        return await _db.PersonLookupValues.AsNoTracking()
            .Where(v => v.IsActive && v.Category!.Code == categoryCode)
            .OrderBy(v => v.SortOrder)
            .Select(v => new LookupValueDto { Id = v.Id, Code = v.Code, Name = v.Name, SortOrder = v.SortOrder })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasDuplicateEmailAsync(string email, Guid? excludePersonId, CancellationToken cancellationToken = default)
    {
        var normalized = PersonNameHelper.NormalizeEmail(email);
        var query = _db.PersonContacts.Where(c => !c.IsDeleted && c.ContactKind == "Email" && c.NormalizedValue == normalized);
        if (excludePersonId.HasValue)
            query = query.Where(c => c.PersonId != excludePersonId.Value);
        return await query.AnyAsync(cancellationToken);
    }

    private static void AddContact(Person person, string kind, string value, bool isPrimary)
    {
        var normalized = kind == "Email" ? PersonNameHelper.NormalizeEmail(value) : PersonNameHelper.NormalizePhone(value);
        person.Contacts.Add(new PersonContact
        {
            Id = Guid.NewGuid(),
            PersonId = person.Id,
            ContactKind = kind,
            Value = value.Trim(),
            NormalizedValue = normalized,
            IsPrimary = isPrimary,
            CreatedAt = DateTime.UtcNow
        });
    }

    private static string? BuildDisplayName(string? first, string? last)
    {
        var parts = new[] { first, last }.Where(p => !string.IsNullOrWhiteSpace(p));
        var name = string.Join(' ', parts);
        return string.IsNullOrWhiteSpace(name) ? null : name;
    }

    private string? GetCorrelationId() =>
        _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? _httpContextAccessor.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? _httpContextAccessor.HttpContext?.TraceIdentifier;

    private static PersonDto MapToDto(Person person) => new()
    {
        Id = person.Id,
        PlatformId = person.PlatformId,
        Status = person.Status.ToString(),
        SearchName = person.SearchName,
        LegalFirstName = person.LegalFirstName,
        LegalLastName = person.LegalLastName,
        DisplayName = person.DisplayName,
        PreferredName = person.PreferredName,
        ShortName = person.ShortName,
        Initials = person.Initials,
        MiddleName = person.MiddleName,
        LocalLanguageName = person.LocalLanguageName,
        PrefixValueId = person.PrefixValueId,
        SuffixValueId = person.SuffixValueId,
        GenderValueId = person.GenderValueId,
        DateOfBirth = person.DateOfBirth,
        NationalityValueId = person.NationalityValueId,
        MaritalStatusValueId = person.MaritalStatusValueId,
        BloodGroupValueId = person.BloodGroupValueId,
        PreferredLanguageValueId = person.PreferredLanguageValueId,
        TimeZone = person.TimeZone,
        PhotoUrl = person.PhotoUrl,
        CreatedAt = person.CreatedAt,
        UpdatedAt = person.UpdatedAt,
        Contacts = person.Contacts.Select(c => new PersonContactDto { Id = c.Id, ContactKind = c.ContactKind, Value = c.Value, IsPrimary = c.IsPrimary, ContactTypeValueId = c.ContactTypeValueId }).ToList(),
        Addresses = person.Addresses.Select(a => new PersonAddressDto { Id = a.Id, AddressTypeValueId = a.AddressTypeValueId, Line1 = a.Line1, Line2 = a.Line2, City = a.City, State = a.State, PostalCode = a.PostalCode, CountryCode = a.CountryCode, IsPrimary = a.IsPrimary }).ToList(),
        Documents = person.Documents.Select(d => new PersonDocumentDto { Id = d.Id, DocumentTypeValueId = d.DocumentTypeValueId, DocumentNumber = d.DocumentNumber, Issuer = d.Issuer, IssueDate = d.IssueDate, ExpiryDate = d.ExpiryDate, VerificationStatus = d.VerificationStatus, AttachmentUrl = d.AttachmentUrl }).ToList(),
        EmergencyContacts = person.EmergencyContacts.Select(e => new EmergencyContactDto { Id = e.Id, Name = e.Name, RelationshipValueId = e.RelationshipValueId, Phone = e.Phone, Email = e.Email }).ToList(),
        Preferences = person.Preferences == null ? null : new PersonPreferencesDto
        {
            Theme = person.Preferences.Theme,
            LanguageCode = person.Preferences.LanguageCode,
            DateFormat = person.Preferences.DateFormat,
            TimeFormat = person.Preferences.TimeFormat,
            DashboardLayout = person.Preferences.DashboardLayout,
            AccessibilityOptions = person.Preferences.AccessibilityOptions,
            NotificationDefaults = person.Preferences.NotificationDefaults,
            AiPreferences = person.Preferences.AiPreferences,
            PreferredContactMethodValueId = person.Preferences.PreferredContactMethodValueId
        }
    };
}
