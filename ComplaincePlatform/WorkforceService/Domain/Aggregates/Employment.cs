namespace WorkforceService.Domain.Aggregates;

public class Employment : Shared.Infrastructure.Domain.BaseEntity
{
    public string EmploymentNumber { get; set; } = string.Empty;
    public Guid PersonId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? OrganizationMemberId { get; set; }
    public Guid? EmploymentTypeValueId { get; set; }
    public Guid? EmploymentStatusValueId { get; set; }
    public string PersonSearchName { get; set; } = string.Empty;

    public DateTime? JoiningDate { get; set; }
    public DateTime? ConfirmationDate { get; set; }
    public DateTime? ProbationEndDate { get; set; }
    public DateTime? SeparationDate { get; set; }
    public int? NoticePeriodDays { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<Entities.PositionAssignment> PositionAssignments { get; set; } = new List<Entities.PositionAssignment>();
    public ICollection<Entities.DepartmentAssignment> DepartmentAssignments { get; set; } = new List<Entities.DepartmentAssignment>();
    public ICollection<Entities.ReportingRelationship> ReportingRelationships { get; set; } = new List<Entities.ReportingRelationship>();
    public ICollection<Entities.EmploymentContract> Contracts { get; set; } = new List<Entities.EmploymentContract>();
    public Entities.AvailabilityProfile? Availability { get; set; }

    public void SoftDelete(Guid? actorUserId)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        UpdatedByUserId = actorUserId;
    }

    public bool CanMutate() => !IsDeleted;
}
