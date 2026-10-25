namespace AccessGovernanceService.Domain.Aggregates;

public class ResponsibilityAssignment
{
    public Guid Id { get; private set; }
    public Guid SubjectUserId { get; private set; }
    public Guid? EmploymentId { get; private set; }
    public Guid ResponsibilityTypeValueId { get; private set; }
    public Guid? ResourceTypeValueId { get; private set; }
    public Guid? ResourceId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public Guid? HospitalId { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public bool IsDeleted { get; private set; }
    public Guid AssignedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private ResponsibilityAssignment() { }

    public static ResponsibilityAssignment Create(Guid subjectUserId, Guid? employmentId, Guid responsibilityTypeValueId, Guid? resourceTypeValueId, Guid? resourceId, Guid? organizationId, Guid? hospitalId, DateTime effectiveFrom, DateTime? effectiveTo, Guid assignedByUserId)
    {
        return new ResponsibilityAssignment
        {
            Id = Guid.NewGuid(),
            SubjectUserId = subjectUserId,
            EmploymentId = employmentId,
            ResponsibilityTypeValueId = responsibilityTypeValueId,
            ResourceTypeValueId = resourceTypeValueId,
            ResourceId = resourceId,
            OrganizationId = organizationId,
            HospitalId = hospitalId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            AssignedByUserId = assignedByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Remove() => IsDeleted = true;

    public bool IsActiveAt(DateTime utcNow) =>
        !IsDeleted && EffectiveFrom <= utcNow && (!EffectiveTo.HasValue || EffectiveTo > utcNow);
}
