using ProfessionalService.Domain.Aggregates;
using ProfessionalService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ProfessionalService.Domain.Entities;

public class Qualification : BaseEntity
{
    public Guid ProfessionalProfileId { get; set; }
    public Guid? QualificationTypeValueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public string? University { get; set; }
    public string? CountryCode { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string? Grade { get; set; }
    public string? AttachmentUrl { get; set; }
    public string VerificationStatus { get; set; } = "Pending";
    public bool IsDeleted { get; set; }
    public ProfessionalProfile? ProfessionalProfile { get; set; }
}

public class ProfessionalRegistration : BaseEntity
{
    public Guid ProfessionalProfileId { get; set; }
    public Guid? RegistrationAuthorityValueId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = "Active";
    public string? VerificationStatus { get; set; }
    public bool IsDeleted { get; set; }
    public ProfessionalProfile? ProfessionalProfile { get; set; }
    public ICollection<RenewalHistoryEntry> RenewalHistory { get; set; } = new List<RenewalHistoryEntry>();

    public bool IsActive(DateTime? asOf = null)
    {
        var at = asOf ?? DateTime.UtcNow;
        return !IsDeleted && Status == "Active" && (ExpiryDate == null || ExpiryDate > at);
    }
}

public class License : BaseEntity
{
    public Guid ProfessionalProfileId { get; set; }
    public Guid? LicenseTypeValueId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public string? Authority { get; set; }
    public string? Jurisdiction { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = "Active";
    public string? AttachmentUrl { get; set; }
    public bool IsDeleted { get; set; }
    public ProfessionalProfile? ProfessionalProfile { get; set; }

    public bool IsActive(DateTime? asOf = null)
    {
        var at = asOf ?? DateTime.UtcNow;
        return !IsDeleted && Status == "Active" && (ExpiryDate == null || ExpiryDate > at);
    }

    public bool Overlaps(License other, DateTime? asOf = null)
    {
        if (LicenseTypeValueId != other.LicenseTypeValueId) return false;
        if (!IsActive(asOf) || !other.IsActive(asOf)) return false;
        return true;
    }
}

public class Certification : BaseEntity
{
    public Guid ProfessionalProfileId { get; set; }
    public Guid? CertificationTypeValueId { get; set; }
    public string CertificationName { get; set; } = string.Empty;
    public string? IssuingOrganization { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool RenewalRequired { get; set; }
    public string VerificationStatus { get; set; } = "Pending";
    public string? CertificateDocumentUrl { get; set; }
    public bool IsDeleted { get; set; }
    public ProfessionalProfile? ProfessionalProfile { get; set; }
}

public class Competency : BaseEntity
{
    public Guid ProfessionalProfileId { get; set; }
    public Guid? SkillValueId { get; set; }
    public Guid? CompetencyLevelValueId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? Level { get; set; }
    public DateTime? AssessmentDate { get; set; }
    public string? Assessor { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? GapAnalysisNotes { get; set; }
    public bool IsDeleted { get; set; }
    public ProfessionalProfile? ProfessionalProfile { get; set; }
}

public class ClinicalPrivilege : BaseEntity
{
    public Guid ProfessionalProfileId { get; set; }
    public Guid? PrivilegeTypeValueId { get; set; }
    public Guid? MasterDepartmentId { get; set; }
    public string PrivilegeName { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string Status { get; set; } = "PendingApproval";
    public string? Conditions { get; set; }
    public bool IsDeleted { get; set; }
    public ProfessionalProfile? ProfessionalProfile { get; set; }

    public bool IsActive(DateTime? asOf = null)
    {
        var at = asOf ?? DateTime.UtcNow;
        return !IsDeleted && Status == "Active" && EffectiveFrom <= at && (ExpiryDate == null || ExpiryDate > at);
    }
}

public class CredentialingRecord : BaseEntity
{
    public Guid ProfessionalProfileId { get; set; }
    public CredentialingStatus Status { get; set; } = CredentialingStatus.Draft;
    public string? Committee { get; set; }
    public Guid? ApproverUserId { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsDeleted { get; set; }
    public ProfessionalProfile? ProfessionalProfile { get; set; }
}

public class TrainingRecord : BaseEntity
{
    public Guid ProfessionalProfileId { get; set; }
    public Guid? TrainingTypeValueId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public decimal? Hours { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string? CertificateUrl { get; set; }
    public bool IsMandatory { get; set; }
    public bool RenewalRequired { get; set; }
    public decimal? AssessmentScore { get; set; }
    public bool IsDeleted { get; set; }
    public ProfessionalProfile? ProfessionalProfile { get; set; }
}

public class ContinuingEducationRecord : BaseEntity
{
    public Guid ProfessionalProfileId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public DateTime? ActivityDate { get; set; }
    public decimal Credits { get; set; }
    public int? AnnualYear { get; set; }
    public string? EvidenceUrl { get; set; }
    public bool IsDeleted { get; set; }
    public ProfessionalProfile? ProfessionalProfile { get; set; }
}

public class ProfessionalMembership : BaseEntity
{
    public Guid ProfessionalProfileId { get; set; }
    public Guid? MembershipOrganizationValueId { get; set; }
    public string MembershipNumber { get; set; } = string.Empty;
    public string? OrganizationName { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string Status { get; set; } = "Active";
    public bool IsDeleted { get; set; }
    public ProfessionalProfile? ProfessionalProfile { get; set; }
}

public class ProfessionalSkill : BaseEntity
{
    public Guid ProfessionalProfileId { get; set; }
    public Guid SkillValueId { get; set; }
    public bool IsDeleted { get; set; }
    public ProfessionalProfile? ProfessionalProfile { get; set; }
}

public class ProfessionalSpecialty : BaseEntity
{
    public Guid ProfessionalProfileId { get; set; }
    public Guid SpecialtyValueId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsDeleted { get; set; }
    public ProfessionalProfile? ProfessionalProfile { get; set; }
}

public class RenewalHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProfessionalRegistrationId { get; set; }
    public DateTime RenewedAt { get; set; } = DateTime.UtcNow;
    public DateTime? NewExpiryDate { get; set; }
    public string? Notes { get; set; }
    public Guid? ActorUserId { get; set; }
    public ProfessionalRegistration? Registration { get; set; }
}

public class ProfessionalHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProfessionalProfileId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public string? PreviousValueJson { get; set; }
    public string? NewValueJson { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class ProfessionalTimelineEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProfessionalProfileId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public string? CorrelationId { get; set; }
}

public class ProfessionalLookupCategory
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; } = true;
    public ICollection<ProfessionalLookupValue> Values { get; set; } = new List<ProfessionalLookupValue>();
}

public class ProfessionalLookupValue
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public ProfessionalLookupCategory? Category { get; set; }
}

public class ProfessionalAuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProfessionalProfileId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string EventCode { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
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
