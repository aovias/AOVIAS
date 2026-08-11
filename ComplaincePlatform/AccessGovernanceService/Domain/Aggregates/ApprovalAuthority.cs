namespace AccessGovernanceService.Domain.Aggregates;

public class ApprovalAuthority
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? EmploymentId { get; private set; }
    public Guid ApprovalLevelValueId { get; private set; }
    public string ApprovalType { get; private set; } = string.Empty;
    public Guid? OrganizationId { get; private set; }
    public Guid? HospitalId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public bool IsDeleted { get; private set; }
    public Guid SetByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private ApprovalAuthority() { }

    public static ApprovalAuthority Create(Guid userId, Guid? employmentId, Guid approvalLevelValueId, string approvalType, Guid? organizationId, Guid? hospitalId, Guid? departmentId, DateTime effectiveFrom, DateTime? effectiveTo, Guid setByUserId)
    {
        return new ApprovalAuthority
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            EmploymentId = employmentId,
            ApprovalLevelValueId = approvalLevelValueId,
            ApprovalType = approvalType.Trim(),
            OrganizationId = organizationId,
            HospitalId = hospitalId,
            DepartmentId = departmentId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            SetByUserId = setByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SoftDelete() => IsDeleted = true;

    public bool IsActiveAt(DateTime utcNow) =>
        !IsDeleted && EffectiveFrom <= utcNow && (!EffectiveTo.HasValue || EffectiveTo > utcNow);
}
