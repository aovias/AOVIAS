using AccessGovernanceService.Domain.Entities;
using AccessGovernanceService.Domain.Enums;

namespace AccessGovernanceService.Domain.Aggregates;

public class RoleDefinition
{
    public Guid Id { get; private set; }
    public string RoleCode { get; private set; } = string.Empty;
    public string RoleName { get; private set; } = string.Empty;
    public ScopeType ScopeType { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public Guid? HospitalId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid? ParentRoleId { get; private set; }
    public Guid? RoleCategoryValueId { get; private set; }
    public DateTime? EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public bool IsEmergency { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private readonly List<RoleAssignment> _assignments = new();
    private readonly List<PermissionAssignment> _permissions = new();
    public IReadOnlyCollection<RoleAssignment> Assignments => _assignments.AsReadOnly();
    public IReadOnlyCollection<PermissionAssignment> Permissions => _permissions.AsReadOnly();

    private RoleDefinition() { }

    public static RoleDefinition Create(string roleCode, string roleName, ScopeType scopeType, Guid? organizationId, Guid? hospitalId, Guid? departmentId, Guid? roleCategoryValueId, bool isSystem = false)
    {
        return new RoleDefinition
        {
            Id = Guid.NewGuid(),
            RoleCode = roleCode.Trim(),
            RoleName = roleName.Trim(),
            ScopeType = scopeType,
            OrganizationId = organizationId,
            HospitalId = hospitalId,
            DepartmentId = departmentId,
            RoleCategoryValueId = roleCategoryValueId,
            IsSystem = isSystem,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string roleName, Guid? roleCategoryValueId, DateTime? effectiveFrom, DateTime? effectiveTo, bool isEmergency)
    {
        RoleName = roleName.Trim();
        RoleCategoryValueId = roleCategoryValueId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsEmergency = isEmergency;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetParentRole(Guid? parentRoleId)
    {
        if (parentRoleId == Id)
            throw new InvalidOperationException("Role cannot be its own parent.");
        ParentRoleId = parentRoleId;
        UpdatedAt = DateTime.UtcNow;
    }

    public RoleAssignment AssignToUser(Guid userId, Guid? organizationId, Guid? hospitalId, Guid? departmentId, Guid? employmentId, DateTime? effectiveFrom, DateTime? effectiveTo, Guid assignedByUserId)
    {
        var assignment = RoleAssignment.Create(Id, userId, organizationId, hospitalId, departmentId, employmentId, effectiveFrom, effectiveTo, assignedByUserId);
        _assignments.Add(assignment);
        return assignment;
    }

    public PermissionAssignment GrantPermission(Guid permissionId, Guid grantedByUserId)
    {
        var grant = PermissionAssignment.Create(Id, permissionId, grantedByUserId);
        _permissions.Add(grant);
        return grant;
    }

    public void SoftDelete() { IsDeleted = true; UpdatedAt = DateTime.UtcNow; }
}
