namespace ProfessionalService.Domain.Aggregates;

public class ProfessionalProfile : Shared.Infrastructure.Domain.BaseEntity
{
    public string ProfessionalNumber { get; set; } = string.Empty;
    public Guid EmploymentId { get; set; }
    public Guid PersonId { get; set; }
    public string PersonSearchName { get; set; } = string.Empty;
    public Guid? ProfessionalCategoryValueId { get; set; }
    public Guid? ProfessionalStatusValueId { get; set; }
    public Guid? PrimarySpecialtyValueId { get; set; }
    public string? Biography { get; set; }
    public int? YearsOfExperience { get; set; }
    public DateTime? PracticeStartDate { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Entities.Qualification> Qualifications { get; set; } = new List<Entities.Qualification>();
    public ICollection<Entities.ProfessionalRegistration> Registrations { get; set; } = new List<Entities.ProfessionalRegistration>();
    public ICollection<Entities.License> Licenses { get; set; } = new List<Entities.License>();
    public ICollection<Entities.Certification> Certifications { get; set; } = new List<Entities.Certification>();
    public ICollection<Entities.Competency> Competencies { get; set; } = new List<Entities.Competency>();
    public ICollection<Entities.ClinicalPrivilege> ClinicalPrivileges { get; set; } = new List<Entities.ClinicalPrivilege>();
    public ICollection<Entities.CredentialingRecord> CredentialingRecords { get; set; } = new List<Entities.CredentialingRecord>();
    public ICollection<Entities.TrainingRecord> TrainingRecords { get; set; } = new List<Entities.TrainingRecord>();
    public ICollection<Entities.ContinuingEducationRecord> ContinuingEducationRecords { get; set; } = new List<Entities.ContinuingEducationRecord>();
    public ICollection<Entities.ProfessionalMembership> Memberships { get; set; } = new List<Entities.ProfessionalMembership>();
    public ICollection<Entities.ProfessionalSkill> Skills { get; set; } = new List<Entities.ProfessionalSkill>();
    public ICollection<Entities.ProfessionalSpecialty> Specialties { get; set; } = new List<Entities.ProfessionalSpecialty>();

    public void SoftDelete(Guid? actorUserId)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = actorUserId;
    }

    public bool CanMutate() => !IsDeleted;
}
