using ProfessionalService.Application.Contracts;
using ProfessionalService.Application.DTOs;
using ProfessionalService.Domain.Aggregates;
using ProfessionalService.Domain.Entities;
using ProfessionalService.Domain.Enums;
using ProfessionalService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Services;
using Shared.Logging.Audit;

namespace ProfessionalService.Application.Services;

public class ProfessionalAuditService : IProfessionalAuditService
{
    private readonly ProfessionalDbContext _db;
    private readonly IAuditLogger _auditLogger;

    public ProfessionalAuditService(ProfessionalDbContext db, IAuditLogger auditLogger)
    {
        _db = db;
        _auditLogger = auditLogger;
    }

    public async Task RecordAsync(Guid profileId, string eventCode, string action, Guid? actorUserId, string? oldValue, string? newValue, string? correlationId, CancellationToken cancellationToken = default)
    {
        _db.ProfessionalAuditLogs.Add(new ProfessionalAuditLog
        {
            ProfessionalProfileId = profileId,
            ActorUserId = actorUserId,
            EventCode = eventCode,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            CorrelationId = correlationId
        });
        await _db.SaveChangesAsync(cancellationToken);
        _auditLogger.Success(eventCode, action, targetType: "ProfessionalProfile", targetId: profileId.ToString(), actorId: actorUserId?.ToString());
    }

    public async Task RecordTimelineAsync(Guid profileId, string eventType, string summary, Guid? actorUserId, string? correlationId, CancellationToken cancellationToken = default)
    {
        _db.ProfessionalTimeline.Add(new ProfessionalTimelineEntry
        {
            ProfessionalProfileId = profileId,
            EventType = eventType,
            Summary = summary,
            ActorUserId = actorUserId,
            CorrelationId = correlationId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordHistoryAsync(Guid profileId, string eventType, string? reason, string? oldJson, string? newJson, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        _db.ProfessionalHistories.Add(new ProfessionalHistory
        {
            ProfessionalProfileId = profileId,
            EventType = eventType,
            Reason = reason,
            PreviousValueJson = oldJson,
            NewValueJson = newJson,
            ActorUserId = actorUserId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public class ProfessionalProfileAppService : IProfessionalProfileAppService
{
    private readonly ProfessionalDbContext _db;
    private readonly IPlatformIdGenerator _idGenerator;
    private readonly IProfessionalAuditService _audit;
    private readonly IWorkforceServiceClient _workforceClient;
    private readonly IOrganizationServiceClient _orgClient;
    private readonly IHttpContextAccessor _http;

    public ProfessionalProfileAppService(
        ProfessionalDbContext db,
        IPlatformIdGenerator idGenerator,
        IProfessionalAuditService audit,
        IWorkforceServiceClient workforceClient,
        IOrganizationServiceClient orgClient,
        IHttpContextAccessor http)
    {
        _db = db;
        _idGenerator = idGenerator;
        _audit = audit;
        _workforceClient = workforceClient;
        _orgClient = orgClient;
        _http = http;
    }

    public async Task<ProfessionalProfileDto> CreateAsync(CreateProfessionalProfileRequest request, CancellationToken cancellationToken = default)
    {
        var employment = await _workforceClient.GetEmploymentAsync(request.EmploymentId, cancellationToken)
            ?? throw new InvalidOperationException("Employment not found.");

        if (await _db.ProfessionalProfiles.AnyAsync(p => !p.IsDeleted && p.PersonId == employment.PersonId, cancellationToken))
            throw new InvalidOperationException("A professional profile already exists for this person.");

        var profile = new ProfessionalProfile
        {
            Id = Guid.NewGuid(),
            ProfessionalNumber = await _idGenerator.GenerateAsync("PRO", cancellationToken),
            EmploymentId = employment.EmploymentId,
            PersonId = employment.PersonId,
            PersonSearchName = employment.PersonSearchName,
            OrganizationId = employment.OrganizationId,
            HospitalId = employment.HospitalId,
            ProfessionalCategoryValueId = request.ProfessionalCategoryValueId,
            ProfessionalStatusValueId = request.ProfessionalStatusValueId,
            PrimarySpecialtyValueId = request.PrimarySpecialtyValueId,
            Biography = request.Biography,
            YearsOfExperience = request.YearsOfExperience,
            PracticeStartDate = request.PracticeStartDate,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = request.CreatedByUserId
        };

        _db.ProfessionalProfiles.Add(profile);
        _db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = profile.Id, OccurredOn = DateTime.UtcNow, Type = "ProfessionalProfileCreated", Payload = $"{{\"profileId\":\"{profile.Id}\"}}" });
        await _db.SaveChangesAsync(cancellationToken);

        var cid = CorrelationId();
        await _audit.RecordAsync(profile.Id, "PROFESSIONAL_CREATED", "CreateProfile", request.CreatedByUserId, null, profile.ProfessionalNumber, cid, cancellationToken);
        await _audit.RecordTimelineAsync(profile.Id, "Created", "Professional profile created", request.CreatedByUserId, cid, cancellationToken);
        return (await GetByIdAsync(profile.Id, cancellationToken))!;
    }

    public async Task<ProfessionalProfileDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var p = await LoadQuery().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);
        return p == null ? null : MapToDto(p);
    }

    public async Task<ProfessionalProfileDto?> GetByEmploymentIdAsync(Guid employmentId, CancellationToken cancellationToken = default)
    {
        var p = await LoadQuery().FirstOrDefaultAsync(x => x.EmploymentId == employmentId && !x.IsDeleted, cancellationToken);
        return p == null ? null : MapToDto(p);
    }

    public async Task<ProfessionalProfileDto?> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var p = await LoadQuery().FirstOrDefaultAsync(x => x.PersonId == personId && !x.IsDeleted, cancellationToken);
        return p == null ? null : MapToDto(p);
    }

    public async Task<PagedResultDto<ProfessionalListItemDto>> ListAsync(int page, int pageSize, Guid? categoryValueId, Guid? specialtyValueId, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var q = _db.ProfessionalProfiles.AsNoTracking().Where(p => !p.IsDeleted);
        if (categoryValueId.HasValue) q = q.Where(p => p.ProfessionalCategoryValueId == categoryValueId);
        if (specialtyValueId.HasValue) q = q.Where(p => p.PrimarySpecialtyValueId == specialtyValueId);
        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderBy(p => p.PersonSearchName).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProfessionalListItemDto
            {
                Id = p.Id,
                ProfessionalNumber = p.ProfessionalNumber,
                PersonSearchName = p.PersonSearchName,
                PersonId = p.PersonId,
                EmploymentId = p.EmploymentId,
                ProfessionalCategoryValueId = p.ProfessionalCategoryValueId,
                PrimarySpecialtyValueId = p.PrimarySpecialtyValueId
            }).ToListAsync(cancellationToken);
        return new PagedResultDto<ProfessionalListItemDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<PagedResultDto<ProfessionalListItemDto>> SearchAsync(string query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var term = query.Trim().ToUpperInvariant();
        var q = _db.ProfessionalProfiles.AsNoTracking().Where(p => !p.IsDeleted && (
            p.PersonSearchName.Contains(term) || p.ProfessionalNumber.Contains(term)));

        var regIds = await _db.ProfessionalRegistrations.AsNoTracking()
            .Where(r => !r.IsDeleted && r.RegistrationNumber.Contains(term))
            .Select(r => r.ProfessionalProfileId).Distinct().ToListAsync(cancellationToken);
        if (regIds.Count > 0)
            q = q.Where(p => regIds.Contains(p.Id) || p.PersonSearchName.Contains(term) || p.ProfessionalNumber.Contains(term));

        var total = await q.CountAsync(cancellationToken);
        var items = await q.OrderBy(p => p.PersonSearchName).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProfessionalListItemDto { Id = p.Id, ProfessionalNumber = p.ProfessionalNumber, PersonSearchName = p.PersonSearchName, PersonId = p.PersonId, EmploymentId = p.EmploymentId })
            .ToListAsync(cancellationToken);
        return new PagedResultDto<ProfessionalListItemDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<ProfessionalProfileDto?> UpdateAsync(Guid id, UpdateProfessionalProfileRequest request, CancellationToken cancellationToken = default)
    {
        var profile = await _db.ProfessionalProfiles.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (profile == null || !profile.CanMutate()) return null;

        profile.ProfessionalCategoryValueId = request.ProfessionalCategoryValueId ?? profile.ProfessionalCategoryValueId;
        profile.ProfessionalStatusValueId = request.ProfessionalStatusValueId ?? profile.ProfessionalStatusValueId;
        profile.PrimarySpecialtyValueId = request.PrimarySpecialtyValueId ?? profile.PrimarySpecialtyValueId;
        profile.Biography = request.Biography ?? profile.Biography;
        profile.YearsOfExperience = request.YearsOfExperience ?? profile.YearsOfExperience;
        profile.PracticeStartDate = request.PracticeStartDate ?? profile.PracticeStartDate;
        profile.UpdatedAt = DateTime.UtcNow;
        profile.UpdatedByUserId = request.UpdatedByUserId;

        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordTimelineAsync(id, "Updated", "Professional profile updated", request.UpdatedByUserId, CorrelationId(), cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(Guid id, Guid? actorUserId, CancellationToken cancellationToken = default)
    {
        var profile = await _db.ProfessionalProfiles.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, cancellationToken);
        if (profile == null) return false;
        profile.SoftDelete(actorUserId);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordAsync(id, "PROFESSIONAL_DELETED", "DeleteProfile", actorUserId, null, null, CorrelationId(), cancellationToken);
        return true;
    }

    public async Task<QualificationDto?> AddQualificationAsync(Guid profileId, AddQualificationRequest request, CancellationToken cancellationToken = default)
    {
        if (!await ProfileExists(profileId, cancellationToken)) return null;
        var q = new Qualification
        {
            Id = Guid.NewGuid(),
            ProfessionalProfileId = profileId,
            QualificationTypeValueId = request.QualificationTypeValueId,
            Title = request.Title,
            Institution = request.Institution,
            University = request.University,
            CountryCode = request.CountryCode,
            CompletionDate = request.CompletionDate,
            Grade = request.Grade,
            AttachmentUrl = request.AttachmentUrl,
            CreatedAt = DateTime.UtcNow
        };
        _db.Qualifications.Add(q);
        _db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = profileId, OccurredOn = DateTime.UtcNow, Type = "QualificationAdded", Payload = "{}" });
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordTimelineAsync(profileId, "QualificationAdded", $"Added qualification: {request.Title}", request.ActorUserId, CorrelationId(), cancellationToken);
        return new QualificationDto { Id = q.Id, QualificationTypeValueId = q.QualificationTypeValueId, Title = q.Title, Institution = q.Institution, CompletionDate = q.CompletionDate, VerificationStatus = q.VerificationStatus };
    }

    public async Task<RegistrationDto?> AddRegistrationAsync(Guid profileId, AddRegistrationRequest request, CancellationToken cancellationToken = default)
    {
        ValidateDates(request.IssueDate, request.ExpiryDate);
        if (!await ProfileExists(profileId, cancellationToken)) return null;

        var duplicate = await _db.ProfessionalRegistrations.AnyAsync(r =>
            !r.IsDeleted && r.ProfessionalProfileId == profileId &&
            r.RegistrationAuthorityValueId == request.RegistrationAuthorityValueId &&
            r.RegistrationNumber == request.RegistrationNumber, cancellationToken);
        if (duplicate) throw new InvalidOperationException("Registration number already exists for this authority.");

        var reg = new ProfessionalRegistration
        {
            Id = Guid.NewGuid(),
            ProfessionalProfileId = profileId,
            RegistrationAuthorityValueId = request.RegistrationAuthorityValueId,
            RegistrationNumber = request.RegistrationNumber,
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            CreatedAt = DateTime.UtcNow
        };
        _db.ProfessionalRegistrations.Add(reg);
        _db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = profileId, OccurredOn = DateTime.UtcNow, Type = "RegistrationAdded", Payload = "{}" });
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordTimelineAsync(profileId, "RegistrationAdded", $"Registration {request.RegistrationNumber}", request.ActorUserId, CorrelationId(), cancellationToken);
        return MapRegistration(reg);
    }

    public async Task<LicenseDto?> AddLicenseAsync(Guid profileId, AddLicenseRequest request, CancellationToken cancellationToken = default)
    {
        ValidateDates(request.IssueDate, request.ExpiryDate);
        if (!await ProfileExists(profileId, cancellationToken)) return null;

        var activeLicenses = await _db.Licenses.Where(l => !l.IsDeleted && l.ProfessionalProfileId == profileId && l.LicenseTypeValueId == request.LicenseTypeValueId).ToListAsync(cancellationToken);
        var candidate = new License { LicenseTypeValueId = request.LicenseTypeValueId, Status = "Active" };
        if (activeLicenses.Any(l => l.Overlaps(candidate)))
            throw new InvalidOperationException("An active license of this type already exists.");

        var license = new License
        {
            Id = Guid.NewGuid(),
            ProfessionalProfileId = profileId,
            LicenseTypeValueId = request.LicenseTypeValueId,
            LicenseNumber = request.LicenseNumber,
            Authority = request.Authority,
            Jurisdiction = request.Jurisdiction,
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            AttachmentUrl = request.AttachmentUrl,
            CreatedAt = DateTime.UtcNow
        };
        _db.Licenses.Add(license);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordTimelineAsync(profileId, "LicenseAdded", $"License {request.LicenseNumber}", request.ActorUserId, CorrelationId(), cancellationToken);
        return MapLicense(license);
    }

    public async Task<CertificationDto?> AddCertificationAsync(Guid profileId, AddCertificationRequest request, CancellationToken cancellationToken = default)
    {
        ValidateDates(request.IssueDate, request.ExpiryDate);
        if (!await ProfileExists(profileId, cancellationToken)) return null;
        var cert = new Certification
        {
            Id = Guid.NewGuid(),
            ProfessionalProfileId = profileId,
            CertificationTypeValueId = request.CertificationTypeValueId,
            CertificationName = request.CertificationName,
            IssuingOrganization = request.IssuingOrganization,
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            RenewalRequired = request.RenewalRequired,
            CertificateDocumentUrl = request.CertificateDocumentUrl,
            CreatedAt = DateTime.UtcNow
        };
        _db.Certifications.Add(cert);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordTimelineAsync(profileId, "CertificationAdded", request.CertificationName, request.ActorUserId, CorrelationId(), cancellationToken);
        return new CertificationDto { Id = cert.Id, CertificationName = cert.CertificationName, IssuingOrganization = cert.IssuingOrganization, IssueDate = cert.IssueDate, ExpiryDate = cert.ExpiryDate, VerificationStatus = cert.VerificationStatus };
    }

    public async Task<CompetencyDto?> AddCompetencyAsync(Guid profileId, AddCompetencyRequest request, CancellationToken cancellationToken = default)
    {
        if (!await ProfileExists(profileId, cancellationToken)) return null;
        var c = new Competency
        {
            Id = Guid.NewGuid(),
            ProfessionalProfileId = profileId,
            SkillValueId = request.SkillValueId,
            CompetencyLevelValueId = request.CompetencyLevelValueId,
            SkillName = request.SkillName,
            Level = request.Level,
            AssessmentDate = request.AssessmentDate,
            Assessor = request.Assessor,
            ExpiryDate = request.ExpiryDate,
            EvidenceUrl = request.EvidenceUrl,
            CreatedAt = DateTime.UtcNow
        };
        _db.Competencies.Add(c);
        _db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = profileId, OccurredOn = DateTime.UtcNow, Type = "CompetencyUpdated", Payload = "{}" });
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordTimelineAsync(profileId, "CompetencyAdded", request.SkillName, request.ActorUserId, CorrelationId(), cancellationToken);
        return new CompetencyDto { Id = c.Id, SkillName = c.SkillName, Level = c.Level, AssessmentDate = c.AssessmentDate, ExpiryDate = c.ExpiryDate };
    }

    public async Task<ClinicalPrivilegeDto?> GrantPrivilegeAsync(Guid profileId, GrantClinicalPrivilegeRequest request, CancellationToken cancellationToken = default)
    {
        if (!await ProfileExists(profileId, cancellationToken)) return null;
        if (request.MasterDepartmentId.HasValue && !await _orgClient.ValidateDepartmentAsync(request.MasterDepartmentId.Value, cancellationToken))
            throw new InvalidOperationException("Department not found.");

        var status = request.ApprovedByUserId.HasValue ? "Active" : "PendingApproval";

        var priv = new ClinicalPrivilege
        {
            Id = Guid.NewGuid(),
            ProfessionalProfileId = profileId,
            PrivilegeTypeValueId = request.PrivilegeTypeValueId,
            MasterDepartmentId = request.MasterDepartmentId,
            PrivilegeName = request.PrivilegeName,
            EffectiveFrom = request.EffectiveFrom,
            ExpiryDate = request.ExpiryDate,
            ApprovedByUserId = request.ApprovedByUserId,
            Status = status,
            Conditions = request.Conditions,
            CreatedAt = DateTime.UtcNow
        };
        _db.ClinicalPrivileges.Add(priv);
        _db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = profileId, OccurredOn = DateTime.UtcNow, Type = "PrivilegeGranted", Payload = "{}" });
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordTimelineAsync(profileId, "PrivilegeGranted", request.PrivilegeName, request.ActorUserId, CorrelationId(), cancellationToken);
        return MapPrivilege(priv);
    }

    public async Task<CredentialingRecordDto?> SubmitCredentialingAsync(Guid profileId, SubmitCredentialingRequest request, CancellationToken cancellationToken = default)
    {
        if (!await ProfileExists(profileId, cancellationToken)) return null;
        var record = new CredentialingRecord
        {
            Id = Guid.NewGuid(),
            ProfessionalProfileId = profileId,
            Status = CredentialingStatus.Submitted,
            Committee = request.Committee,
            ReviewNotes = request.ReviewNotes,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        _db.CredentialingRecords.Add(record);
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordTimelineAsync(profileId, "CredentialingSubmitted", "Credentialing submitted", request.ActorUserId, CorrelationId(), cancellationToken);
        return MapCredentialing(record);
    }

    public async Task<CredentialingRecordDto?> ApproveCredentialingAsync(Guid profileId, Guid recordId, ApproveCredentialingRequest request, CancellationToken cancellationToken = default)
    {
        var record = await _db.CredentialingRecords.FirstOrDefaultAsync(r => r.Id == recordId && r.ProfessionalProfileId == profileId && !r.IsDeleted, cancellationToken);
        if (record == null) return null;
        if (record.Status is not (CredentialingStatus.Submitted or CredentialingStatus.UnderReview))
            throw new InvalidOperationException("Invalid credentialing state transition.");

        record.Status = CredentialingStatus.Approved;
        record.ApproverUserId = request.ApproverUserId;
        record.ApprovedAt = DateTime.UtcNow;
        record.ExpiryDate = request.ExpiryDate;
        record.ReviewNotes = request.ReviewNotes ?? record.ReviewNotes;
        _db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = profileId, OccurredOn = DateTime.UtcNow, Type = "CredentialApproved", Payload = "{}" });
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordTimelineAsync(profileId, "CredentialApproved", "Credentialing approved", request.ApproverUserId, CorrelationId(), cancellationToken);
        return MapCredentialing(record);
    }

    public async Task<CredentialingRecordDto?> RejectCredentialingAsync(Guid profileId, Guid recordId, RejectCredentialingRequest request, CancellationToken cancellationToken = default)
    {
        var record = await _db.CredentialingRecords.FirstOrDefaultAsync(r => r.Id == recordId && r.ProfessionalProfileId == profileId && !r.IsDeleted, cancellationToken);
        if (record == null) return null;
        if (record.Status is not (CredentialingStatus.Submitted or CredentialingStatus.UnderReview))
            throw new InvalidOperationException("Invalid credentialing state transition.");

        record.Status = CredentialingStatus.Rejected;
        record.ReviewNotes = request.ReviewNotes;
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordTimelineAsync(profileId, "CredentialRejected", "Credentialing rejected", request.ActorUserId, CorrelationId(), cancellationToken);
        return MapCredentialing(record);
    }

    public async Task<TrainingRecordDto?> AddTrainingAsync(Guid profileId, AddTrainingRecordRequest request, CancellationToken cancellationToken = default)
    {
        if (!await ProfileExists(profileId, cancellationToken)) return null;
        var t = new TrainingRecord
        {
            Id = Guid.NewGuid(),
            ProfessionalProfileId = profileId,
            TrainingTypeValueId = request.TrainingTypeValueId,
            CourseName = request.CourseName,
            Provider = request.Provider,
            Hours = request.Hours,
            CompletionDate = request.CompletionDate,
            CertificateUrl = request.CertificateUrl,
            IsMandatory = request.IsMandatory,
            AssessmentScore = request.AssessmentScore,
            CreatedAt = DateTime.UtcNow
        };
        _db.TrainingRecords.Add(t);
        _db.OutboxMessages.Add(new OutboxMessage { Id = Guid.NewGuid(), AggregateId = profileId, OccurredOn = DateTime.UtcNow, Type = "TrainingCompleted", Payload = "{}" });
        await _db.SaveChangesAsync(cancellationToken);
        await _audit.RecordTimelineAsync(profileId, "TrainingCompleted", request.CourseName, request.ActorUserId, CorrelationId(), cancellationToken);
        return new TrainingRecordDto { Id = t.Id, CourseName = t.CourseName, Provider = t.Provider, Hours = t.Hours, CompletionDate = t.CompletionDate, IsMandatory = t.IsMandatory };
    }

    public async Task<IReadOnlyList<ProfessionalTimelineDto>> GetTimelineAsync(Guid profileId, CancellationToken cancellationToken = default)
        => await _db.ProfessionalTimeline.AsNoTracking().Where(t => t.ProfessionalProfileId == profileId)
            .OrderByDescending(t => t.OccurredAt)
            .Select(t => new ProfessionalTimelineDto { Id = t.Id, OccurredAt = t.OccurredAt, EventType = t.EventType, Summary = t.Summary })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<RenewalDashboardItemDto>> GetRenewalsAsync(int withinDays, Guid? organizationId, CancellationToken cancellationToken = default)
    {
        withinDays = Math.Clamp(withinDays, 1, 365);
        var cutoff = DateTime.UtcNow.AddDays(withinDays);
        var profiles = _db.ProfessionalProfiles.AsNoTracking().Where(p => !p.IsDeleted);
        if (organizationId.HasValue) profiles = profiles.Where(p => p.OrganizationId == organizationId);

        var profileList = await profiles.Select(p => new { p.Id, p.ProfessionalNumber, p.PersonSearchName }).ToListAsync(cancellationToken);
        var profileIds = profileList.Select(p => p.Id).ToList();
        var items = new List<RenewalDashboardItemDto>();
        var now = DateTime.UtcNow;

        foreach (var reg in await _db.ProfessionalRegistrations.AsNoTracking().Where(r => profileIds.Contains(r.ProfessionalProfileId) && !r.IsDeleted && r.ExpiryDate != null && r.ExpiryDate <= cutoff).ToListAsync(cancellationToken))
        {
            var p = profileList.First(x => x.Id == reg.ProfessionalProfileId);
            items.Add(new RenewalDashboardItemDto { ProfessionalProfileId = p.Id, ProfessionalNumber = p.ProfessionalNumber, PersonSearchName = p.PersonSearchName, ItemType = "Registration", ItemId = reg.Id, ItemName = reg.RegistrationNumber, ExpiryDate = reg.ExpiryDate, DaysUntilExpiry = (int)(reg.ExpiryDate!.Value - now).TotalDays });
        }
        foreach (var lic in await _db.Licenses.AsNoTracking().Where(l => profileIds.Contains(l.ProfessionalProfileId) && !l.IsDeleted && l.ExpiryDate != null && l.ExpiryDate <= cutoff).ToListAsync(cancellationToken))
        {
            var p = profileList.First(x => x.Id == lic.ProfessionalProfileId);
            items.Add(new RenewalDashboardItemDto { ProfessionalProfileId = p.Id, ProfessionalNumber = p.ProfessionalNumber, PersonSearchName = p.PersonSearchName, ItemType = "License", ItemId = lic.Id, ItemName = lic.LicenseNumber, ExpiryDate = lic.ExpiryDate, DaysUntilExpiry = (int)(lic.ExpiryDate!.Value - now).TotalDays });
        }
        return items.OrderBy(i => i.DaysUntilExpiry).ToList();
    }

    public async Task<IReadOnlyList<LookupValueDto>> GetLookupsAsync(string categoryCode, CancellationToken cancellationToken = default)
        => await _db.ProfessionalLookupValues.AsNoTracking().Where(v => v.IsActive && v.Category!.Code == categoryCode).OrderBy(v => v.SortOrder)
            .Select(v => new LookupValueDto { Id = v.Id, Code = v.Code, Name = v.Name, SortOrder = v.SortOrder }).ToListAsync(cancellationToken);

    private async Task<bool> ProfileExists(Guid id, CancellationToken ct)
        => await _db.ProfessionalProfiles.AnyAsync(p => p.Id == id && !p.IsDeleted, ct);

    private static void ValidateDates(DateTime? issue, DateTime? expiry)
    {
        if (issue.HasValue && expiry.HasValue && expiry <= issue)
            throw new InvalidOperationException("Expiry date must be after issue date.");
    }

    private string? CorrelationId() =>
        _http.HttpContext?.Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? _http.HttpContext?.Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? _http.HttpContext?.TraceIdentifier;

    private IQueryable<ProfessionalProfile> LoadQuery() =>
        _db.ProfessionalProfiles.AsNoTracking()
            .Include(p => p.Qualifications.Where(q => !q.IsDeleted))
            .Include(p => p.Registrations.Where(r => !r.IsDeleted))
            .Include(p => p.Licenses.Where(l => !l.IsDeleted))
            .Include(p => p.Certifications.Where(c => !c.IsDeleted))
            .Include(p => p.Competencies.Where(c => !c.IsDeleted))
            .Include(p => p.ClinicalPrivileges.Where(c => !c.IsDeleted))
            .Include(p => p.CredentialingRecords.Where(c => !c.IsDeleted))
            .Include(p => p.TrainingRecords.Where(t => !t.IsDeleted))
            .Include(p => p.ContinuingEducationRecords.Where(c => !c.IsDeleted))
            .Include(p => p.Memberships.Where(m => !m.IsDeleted))
            .Include(p => p.Skills.Where(s => !s.IsDeleted));

    private static ProfessionalProfileDto MapToDto(ProfessionalProfile p) => new()
    {
        Id = p.Id,
        ProfessionalNumber = p.ProfessionalNumber,
        EmploymentId = p.EmploymentId,
        PersonId = p.PersonId,
        PersonSearchName = p.PersonSearchName,
        OrganizationId = p.OrganizationId,
        HospitalId = p.HospitalId,
        ProfessionalCategoryValueId = p.ProfessionalCategoryValueId,
        ProfessionalStatusValueId = p.ProfessionalStatusValueId,
        PrimarySpecialtyValueId = p.PrimarySpecialtyValueId,
        Biography = p.Biography,
        YearsOfExperience = p.YearsOfExperience,
        PracticeStartDate = p.PracticeStartDate,
        CreatedAt = p.CreatedAt,
        Qualifications = p.Qualifications.Select(q => new QualificationDto { Id = q.Id, QualificationTypeValueId = q.QualificationTypeValueId, Title = q.Title, Institution = q.Institution, CompletionDate = q.CompletionDate, VerificationStatus = q.VerificationStatus }).ToList(),
        Registrations = p.Registrations.Select(MapRegistration).ToList(),
        Licenses = p.Licenses.Select(MapLicense).ToList(),
        Certifications = p.Certifications.Select(c => new CertificationDto { Id = c.Id, CertificationName = c.CertificationName, IssuingOrganization = c.IssuingOrganization, IssueDate = c.IssueDate, ExpiryDate = c.ExpiryDate, VerificationStatus = c.VerificationStatus }).ToList(),
        Competencies = p.Competencies.Select(c => new CompetencyDto { Id = c.Id, SkillName = c.SkillName, Level = c.Level, AssessmentDate = c.AssessmentDate, ExpiryDate = c.ExpiryDate }).ToList(),
        ClinicalPrivileges = p.ClinicalPrivileges.Select(MapPrivilege).ToList(),
        CredentialingRecords = p.CredentialingRecords.Select(MapCredentialing).ToList(),
        TrainingRecords = p.TrainingRecords.Select(t => new TrainingRecordDto { Id = t.Id, CourseName = t.CourseName, Provider = t.Provider, Hours = t.Hours, CompletionDate = t.CompletionDate, IsMandatory = t.IsMandatory }).ToList(),
        ContinuingEducationRecords = p.ContinuingEducationRecords.Select(c => new ContinuingEducationDto { Id = c.Id, ActivityType = c.ActivityType, Title = c.Title, Credits = c.Credits, ActivityDate = c.ActivityDate }).ToList(),
        Memberships = p.Memberships.Select(m => new MembershipDto { Id = m.Id, MembershipNumber = m.MembershipNumber, OrganizationName = m.OrganizationName, ValidTo = m.ValidTo, Status = m.Status }).ToList(),
        Skills = p.Skills.Select(s => new ProfessionalSkillDto { Id = s.Id, SkillValueId = s.SkillValueId, SkillName = $"Skill-{s.SkillValueId:N}", ProficiencyLevel = null, IsCritical = false }).ToList()
    };

    private static RegistrationDto MapRegistration(ProfessionalRegistration r) => new() { Id = r.Id, RegistrationAuthorityValueId = r.RegistrationAuthorityValueId, RegistrationNumber = r.RegistrationNumber, IssueDate = r.IssueDate, ExpiryDate = r.ExpiryDate, Status = r.Status };
    private static LicenseDto MapLicense(License l) => new() { Id = l.Id, LicenseTypeValueId = l.LicenseTypeValueId, LicenseNumber = l.LicenseNumber, Authority = l.Authority, IssueDate = l.IssueDate, ExpiryDate = l.ExpiryDate, Status = l.Status };
    private static ClinicalPrivilegeDto MapPrivilege(ClinicalPrivilege p) => new() { Id = p.Id, PrivilegeName = p.PrivilegeName, MasterDepartmentId = p.MasterDepartmentId, EffectiveFrom = p.EffectiveFrom, ExpiryDate = p.ExpiryDate, Status = p.Status, ApprovedByUserId = p.ApprovedByUserId };
    private static CredentialingRecordDto MapCredentialing(CredentialingRecord r) => new() { Id = r.Id, Status = r.Status, Committee = r.Committee, ReviewNotes = r.ReviewNotes, SubmittedAt = r.SubmittedAt, ApprovedAt = r.ApprovedAt, ExpiryDate = r.ExpiryDate };
}
