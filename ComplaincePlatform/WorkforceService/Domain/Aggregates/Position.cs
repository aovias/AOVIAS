namespace WorkforceService.Domain.Aggregates;

public class Position : Shared.Infrastructure.Domain.BaseEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public string PositionCode { get; set; } = string.Empty;
    public string PositionName { get; set; } = string.Empty;
    public Guid? PositionLevelValueId { get; set; }
    public Guid? GradeValueId { get; set; }
    public Guid? BandValueId { get; set; }
    public string? Description { get; set; }
    public bool IsClinical { get; set; }
    public bool IsAdministrative { get; set; }
    public bool IsLeadership { get; set; }
    public int ApprovalLevel { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
}
