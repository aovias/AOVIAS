using ProfessionalService.Domain.Enums;

namespace ProfessionalService.Application.DTOs;

public class ProfessionalProfileDto
{
    public Guid Id { get; set; }
    public string ProfessionalNumber { get; set; } = string.Empty;
    public Guid EmploymentId { get; set; }
    public Guid PersonId { get; set; }
    public string PersonSearchName { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid? ProfessionalCategoryValueId { get; set; }
    public Guid? ProfessionalStatusValueId { get; set; }
    public Guid? PrimarySpecialtyValueId { get; set; }
    public string? Biography { get; set; }
    public int? YearsOfExperience { get; set; }
    public DateTime? PracticeStartDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<QualificationDto> Qualifications { get; set; } = new();
    public List<RegistrationDto> Registrations { get; set; } = new();
    public List<LicenseDto> Licenses { get; set; } = new();
    public List<CertificationDto> Certifications { get; set; } = new();
    public List<CompetencyDto> Competencies { get; set; } = new();
    public List<ClinicalPrivilegeDto> ClinicalPrivileges { get; set; } = new();
    public List<CredentialingRecordDto> CredentialingRecords { get; set; } = new();
    public List<TrainingRecordDto> TrainingRecords { get; set; } = new();
    public List<ContinuingEducationDto> ContinuingEducationRecords { get; set; } = new();
    public List<MembershipDto> Memberships { get; set; } = new();
    public List<ProfessionalSkillDto> Skills { get; set; } = new();
}

public class ProfessionalListItemDto
{
    public Guid Id { get; set; }
    public string ProfessionalNumber { get; set; } = string.Empty;
    public string PersonSearchName { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
    public Guid EmploymentId { get; set; }
    public Guid? ProfessionalCategoryValueId { get; set; }
    public Guid? PrimarySpecialtyValueId { get; set; }
}

public class QualificationDto
{
    public Guid Id { get; set; }
    public Guid? QualificationTypeValueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
}

public class RegistrationDto
{
    public Guid Id { get; set; }
    public Guid? RegistrationAuthorityValueId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class LicenseDto
{
    public Guid Id { get; set; }
    public Guid? LicenseTypeValueId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public string? Authority { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class CertificationDto
{
    public Guid Id { get; set; }
    public string CertificationName { get; set; } = string.Empty;
    public string? IssuingOrganization { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
}

public class CompetencyDto
{
    public Guid Id { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? Level { get; set; }
    public DateTime? AssessmentDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class ClinicalPrivilegeDto
{
    public Guid Id { get; set; }
    public string PrivilegeName { get; set; } = string.Empty;
    public Guid? MasterDepartmentId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ApprovedByUserId { get; set; }
}

public class CredentialingRecordDto
{
    public Guid Id { get; set; }
    public CredentialingStatus Status { get; set; }
    public string? Committee { get; set; }
    public string? ReviewNotes { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public class TrainingRecordDto
{
    public Guid Id { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public decimal? Hours { get; set; }
    public DateTime? CompletionDate { get; set; }
    public bool IsMandatory { get; set; }
}

public class ContinuingEducationDto
{
    public Guid Id { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal Credits { get; set; }
    public DateTime? ActivityDate { get; set; }
}

public class MembershipDto
{
    public Guid Id { get; set; }
    public string MembershipNumber { get; set; } = string.Empty;
    public string? OrganizationName { get; set; }
    public DateTime? ValidTo { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class ProfessionalSkillDto
{
    public Guid Id { get; set; }
    public Guid SkillValueId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? ProficiencyLevel { get; set; }
    public bool IsCritical { get; set; }
}

public class ProfessionalTimelineDto
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public class RenewalDashboardItemDto
{
    public Guid ProfessionalProfileId { get; set; }
    public string ProfessionalNumber { get; set; } = string.Empty;
    public string PersonSearchName { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
}

public class LookupValueDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class CreateProfessionalProfileRequest
{
    public Guid EmploymentId { get; set; }
    public Guid? ProfessionalCategoryValueId { get; set; }
    public Guid? ProfessionalStatusValueId { get; set; }
    public Guid? PrimarySpecialtyValueId { get; set; }
    public string? Biography { get; set; }
    public int? YearsOfExperience { get; set; }
    public DateTime? PracticeStartDate { get; set; }
    public Guid? CreatedByUserId { get; set; }
}

public class UpdateProfessionalProfileRequest
{
    public Guid? ProfessionalCategoryValueId { get; set; }
    public Guid? ProfessionalStatusValueId { get; set; }
    public Guid? PrimarySpecialtyValueId { get; set; }
    public string? Biography { get; set; }
    public int? YearsOfExperience { get; set; }
    public DateTime? PracticeStartDate { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}

public class AddQualificationRequest
{
    public Guid? QualificationTypeValueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Institution { get; set; }
    public string? University { get; set; }
    public string? CountryCode { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string? Grade { get; set; }
    public string? AttachmentUrl { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class AddRegistrationRequest
{
    public Guid? RegistrationAuthorityValueId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class AddLicenseRequest
{
    public Guid? LicenseTypeValueId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public string? Authority { get; set; }
    public string? Jurisdiction { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? AttachmentUrl { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class AddCertificationRequest
{
    public Guid? CertificationTypeValueId { get; set; }
    public string CertificationName { get; set; } = string.Empty;
    public string? IssuingOrganization { get; set; }
    public DateTime? IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool RenewalRequired { get; set; }
    public string? CertificateDocumentUrl { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class AddCompetencyRequest
{
    public Guid? SkillValueId { get; set; }
    public Guid? CompetencyLevelValueId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string? Level { get; set; }
    public DateTime? AssessmentDate { get; set; }
    public string? Assessor { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? EvidenceUrl { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class GrantClinicalPrivilegeRequest
{
    public Guid? PrivilegeTypeValueId { get; set; }
    public Guid? MasterDepartmentId { get; set; }
    public string PrivilegeName { get; set; } = string.Empty;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? Conditions { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class SubmitCredentialingRequest
{
    public string? Committee { get; set; }
    public string? ReviewNotes { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class ReviewCredentialingRequest
{
    public string? ReviewNotes { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class ApproveCredentialingRequest
{
    public Guid ApproverUserId { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? ReviewNotes { get; set; }
}

public class RejectCredentialingRequest
{
    public string? ReviewNotes { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class AddTrainingRecordRequest
{
    public Guid? TrainingTypeValueId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public decimal? Hours { get; set; }
    public DateTime? CompletionDate { get; set; }
    public string? CertificateUrl { get; set; }
    public bool IsMandatory { get; set; }
    public decimal? AssessmentScore { get; set; }
    public Guid? ActorUserId { get; set; }
}

public class EmploymentRefDto
{
    public Guid EmploymentId { get; set; }
    public Guid PersonId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public string PersonSearchName { get; set; } = string.Empty;
}
