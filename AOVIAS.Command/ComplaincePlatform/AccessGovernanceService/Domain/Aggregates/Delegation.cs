using AccessGovernanceService.Domain.Entities;
using AccessGovernanceService.Domain.Enums;

namespace AccessGovernanceService.Domain.Aggregates;

public class Delegation
{
    public Guid Id { get; private set; }
    public Guid FromUserId { get; private set; }
    public Guid ToUserId { get; private set; }
    public Guid? FromEmploymentId { get; private set; }
    public Guid? ToEmploymentId { get; private set; }
    public Guid? PermissionId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public Guid? HospitalId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid? DelegationReasonValueId { get; private set; }
    public DelegationStatus Status { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public bool RequiresApproval { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private readonly List<DelegationHistory> _history = new();
    public IReadOnlyCollection<DelegationHistory> History => _history.AsReadOnly();

    private Delegation() { }

    public static Delegation Create(Guid fromUserId, Guid toUserId, Guid? fromEmploymentId, Guid? toEmploymentId, Guid? permissionId, Guid? organizationId, Guid? hospitalId, Guid? departmentId, Guid? reasonValueId, DateTime effectiveFrom, DateTime? effectiveTo, bool requiresApproval, Guid createdByUserId)
    {
        if (fromUserId == toUserId)
            throw new InvalidOperationException("Cannot delegate to self.");
        if (!permissionId.HasValue)
            throw new InvalidOperationException("PermissionId is required for delegations.");
        if (effectiveTo.HasValue && effectiveTo <= effectiveFrom)
            throw new InvalidOperationException("EffectiveTo must be after EffectiveFrom.");

        var d = new Delegation
        {
            Id = Guid.NewGuid(),
            FromUserId = fromUserId,
            ToUserId = toUserId,
            FromEmploymentId = fromEmploymentId,
            ToEmploymentId = toEmploymentId,
            PermissionId = permissionId,
            OrganizationId = organizationId,
            HospitalId = hospitalId,
            DepartmentId = departmentId,
            DelegationReasonValueId = reasonValueId,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            RequiresApproval = requiresApproval,
            CreatedByUserId = createdByUserId,
            Status = requiresApproval ? DelegationStatus.PendingApproval : DelegationStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        d._history.Add(DelegationHistory.Create(d.Id, "Created", createdByUserId));
        return d;
    }

    public void Approve(Guid approvedByUserId)
    {
        if (Status != DelegationStatus.PendingApproval)
            throw new InvalidOperationException("Delegation is not pending approval.");
        Status = DelegationStatus.Active;
        _history.Add(DelegationHistory.Create(Id, "Approved", approvedByUserId));
    }

    public void Revoke(Guid revokedByUserId)
    {
        Status = DelegationStatus.Revoked;
        RevokedAt = DateTime.UtcNow;
        _history.Add(DelegationHistory.Create(Id, "Revoked", revokedByUserId));
    }

    public bool IsActiveAt(DateTime utcNow) =>
        Status == DelegationStatus.Active &&
        EffectiveFrom <= utcNow &&
        (!EffectiveTo.HasValue || EffectiveTo > utcNow);
}
