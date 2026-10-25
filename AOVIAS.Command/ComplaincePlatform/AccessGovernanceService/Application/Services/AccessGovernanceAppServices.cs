using AccessGovernanceService.Application.Contracts;
using AccessGovernanceService.Domain.Aggregates;
using AccessGovernanceService.Domain.Entities;
using AccessGovernanceService.Domain.Enums;
using AccessGovernanceService.Infrastructure.Outbox;
using AccessGovernanceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccessGovernanceService.Application.Services;

public class RoleAppService : IRoleAppService
{
    private readonly AccessGovernanceDbContext _db;
    public RoleAppService(AccessGovernanceDbContext db) => _db = db;

    public async Task<RoleDto> CreateAsync(CreateRoleRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        if (await _db.RoleDefinitions.AnyAsync(r => r.RoleCode == request.RoleCode, ct))
            throw new InvalidOperationException("Role code already exists.");
        if (!Enum.TryParse<ScopeType>(request.ScopeType, true, out var scope))
            scope = ScopeType.Organization;
        var role = RoleDefinition.Create(request.RoleCode, request.RoleName, scope, request.OrganizationId, request.HospitalId, request.DepartmentId, request.RoleCategoryValueId);
        if (request.IsEmergency)
            role.Update(request.RoleName, request.RoleCategoryValueId, null, null, true);
        if (request.ParentRoleId.HasValue)
            role.SetParentRole(request.ParentRoleId);
        _db.RoleDefinitions.Add(role);
        _db.AccessAuditLogs.Add(new AccessAuditLog { Id = Guid.NewGuid(), EntityType = "RoleDefinition", EntityId = role.Id, Action = "Created", ActorUserId = actorUserId, OccurredAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
        return Map(role);
    }

    public async Task UpdateAsync(Guid id, UpdateRoleRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var role = await _db.RoleDefinitions.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct)
            ?? throw new InvalidOperationException("Role not found.");
        role.Update(request.RoleName, request.RoleCategoryValueId, null, null, request.IsEmergency);
        if (request.ParentRoleId.HasValue)
            role.SetParentRole(request.ParentRoleId);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<RoleDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var role = await _db.RoleDefinitions.FirstOrDefaultAsync(r => r.Id == id && !r.IsDeleted, ct);
        return role == null ? null : Map(role);
    }

    public async Task<PagedResultDto<RoleListItemDto>> ListAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var q = _db.RoleDefinitions.Where(r => !r.IsDeleted);
        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(r => r.RoleCode).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(r => new RoleListItemDto { Id = r.Id, RoleCode = r.RoleCode, RoleName = r.RoleName }).ToListAsync(ct);
        return new PagedResultDto<RoleListItemDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task AssignRoleAsync(Guid roleId, AssignRoleRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var role = await _db.RoleDefinitions.FirstOrDefaultAsync(r => r.Id == roleId && !r.IsDeleted, ct)
            ?? throw new InvalidOperationException("Role not found.");
        var now = DateTime.UtcNow;
        var duplicate = await _db.RoleAssignments.AnyAsync(a =>
            a.RoleDefinitionId == roleId && a.UserId == request.UserId && !a.IsRevoked &&
            a.OrganizationId == request.OrganizationId && a.HospitalId == request.HospitalId &&
            a.DepartmentId == request.DepartmentId && a.EmploymentId == request.EmploymentId &&
            (!a.EffectiveFrom.HasValue || a.EffectiveFrom <= now) &&
            (!a.EffectiveTo.HasValue || a.EffectiveTo > now), ct);
        if (duplicate) throw new InvalidOperationException("Duplicate active role assignment.");
        var assignment = RoleAssignment.Create(roleId, request.UserId, request.OrganizationId, request.HospitalId, request.DepartmentId, request.EmploymentId, request.EffectiveFrom, request.EffectiveTo, actorUserId);
        _db.RoleAssignments.Add(assignment);
        AccessGovernanceOutboxWriter.Enqueue(_db, assignment.Id, "RoleAssigned", new { assignment.UserId, roleId, assignment.OrganizationId, assignment.HospitalId, assignment.DepartmentId });
        await _db.SaveChangesAsync(ct);
    }

    public async Task RevokeAssignmentAsync(Guid assignmentId, Guid actorUserId, CancellationToken ct = default)
    {
        var assignment = await _db.RoleAssignments.FirstOrDefaultAsync(a => a.Id == assignmentId, ct)
            ?? throw new InvalidOperationException("Assignment not found.");
        assignment.Revoke();
        AccessGovernanceOutboxWriter.Enqueue(_db, assignment.Id, "RoleRevoked", new { assignment.UserId, assignment.RoleDefinitionId });
        _db.AccessAuditLogs.Add(new AccessAuditLog { Id = Guid.NewGuid(), EntityType = "RoleAssignment", EntityId = assignmentId, Action = "Revoked", ActorUserId = actorUserId, OccurredAt = DateTime.UtcNow });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<RoleAssignmentDto>> ListAssignmentsAsync(Guid? userId, CancellationToken ct = default)
    {
        var q = _db.RoleAssignments.AsQueryable();
        if (userId.HasValue) q = q.Where(a => a.UserId == userId.Value);
        var assignments = await q.OrderByDescending(a => a.AssignedAt).ToListAsync(ct);
        var roleIds = assignments.Select(a => a.RoleDefinitionId).Distinct().ToList();
        var roles = await _db.RoleDefinitions.Where(r => roleIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.RoleCode, ct);
        return assignments.Select(a => new RoleAssignmentDto
        {
            Id = a.Id,
            RoleDefinitionId = a.RoleDefinitionId,
            RoleCode = roles.GetValueOrDefault(a.RoleDefinitionId, string.Empty),
            UserId = a.UserId,
            OrganizationId = a.OrganizationId,
            HospitalId = a.HospitalId,
            DepartmentId = a.DepartmentId,
            EmploymentId = a.EmploymentId,
            EffectiveFrom = a.EffectiveFrom,
            EffectiveTo = a.EffectiveTo,
            IsRevoked = a.IsRevoked
        }).ToList();
    }

    public async Task GrantPermissionAsync(Guid roleId, Guid permissionId, Guid actorUserId, CancellationToken ct = default)
    {
        if (!await _db.RoleDefinitions.AnyAsync(r => r.Id == roleId && !r.IsDeleted, ct))
            throw new InvalidOperationException("Role not found.");
        if (!await _db.Permissions.AnyAsync(p => p.Id == permissionId && !p.IsDeleted, ct))
            throw new InvalidOperationException("Permission not found.");
        if (await _db.PermissionAssignments.AnyAsync(pa => pa.RoleDefinitionId == roleId && pa.PermissionId == permissionId && !pa.IsRevoked, ct))
            return;
        _db.PermissionAssignments.Add(PermissionAssignment.Create(roleId, permissionId, actorUserId));
        AccessGovernanceOutboxWriter.Enqueue(_db, roleId, "PermissionGranted", new { roleId, permissionId });
        await _db.SaveChangesAsync(ct);
    }

    private static RoleDto Map(RoleDefinition r) => new() { Id = r.Id, RoleCode = r.RoleCode, RoleName = r.RoleName, ScopeType = r.ScopeType.ToString() };
}

public class PermissionAppService : IPermissionAppService
{
    private readonly AccessGovernanceDbContext _db;
    public PermissionAppService(AccessGovernanceDbContext db) => _db = db;

    public async Task<PermissionDto> CreateAsync(CreatePermissionRequest request, CancellationToken ct = default)
    {
        if (await _db.Permissions.AnyAsync(p => p.Code == request.Code, ct))
            throw new InvalidOperationException("Permission code already exists.");
        var p = Permission.Create(request.Code, request.Module, request.Feature, request.Action, request.Description, request.PermissionGroupValueId, null, null, request.FieldMask);
        _db.Permissions.Add(p);
        await _db.SaveChangesAsync(ct);
        return Map(p);
    }

    public async Task<IReadOnlyList<PermissionDto>> ListAsync(Guid? permissionGroupValueId = null, CancellationToken ct = default)
    {
        var q = _db.Permissions.Where(p => !p.IsDeleted);
        if (permissionGroupValueId.HasValue)
            q = q.Where(p => p.PermissionGroupValueId == permissionGroupValueId);
        return await q.OrderBy(p => p.Code).Select(p => Map(p)).ToListAsync(ct);
    }

    private static PermissionDto Map(Permission p) => new()
    {
        Id = p.Id, Code = p.Code, Module = p.Module, Feature = p.Feature, Action = p.Action,
        Description = p.Description, PermissionGroupValueId = p.PermissionGroupValueId, FieldMask = p.FieldMask
    };
}

public class PolicyAppService : IPolicyAppService
{
    private readonly AccessGovernanceDbContext _db;
    public PolicyAppService(AccessGovernanceDbContext db) => _db = db;

    public async Task<PolicyDto> CreateAsync(CreatePolicyRequest request, CancellationToken ct = default)
    {
        if (await _db.AuthorizationPolicies.AnyAsync(p => p.PolicyCode == request.PolicyCode, ct))
            throw new InvalidOperationException("Policy code already exists.");
        var policy = AuthorizationPolicy.Create(request.PolicyCode, request.PolicyName, request.Priority, null, request.OrganizationId);
        AddRules(policy, request.Rules);
        _db.AuthorizationPolicies.Add(policy);
        await _db.SaveChangesAsync(ct);
        return Map(policy);
    }

    public async Task<PolicyDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var policy = await _db.AuthorizationPolicies
            .Include(p => p.Rules).ThenInclude(r => r.Conditions)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct);
        if (policy == null) return null;
        return MapDetail(policy);
    }

    public async Task UpdateAsync(Guid id, UpdatePolicyRequest request, CancellationToken ct = default)
    {
        var policy = await _db.AuthorizationPolicies
            .Include(p => p.Rules).ThenInclude(r => r.Conditions)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct)
            ?? throw new InvalidOperationException("Policy not found.");
        if (policy.Status != PolicyStatus.Draft)
            throw new InvalidOperationException("Only draft policies can be updated.");

        policy.UpdateDraft(request.PolicyName, request.Priority);

        var ruleIds = policy.Rules.Select(r => r.Id).ToList();
        if (ruleIds.Count > 0)
        {
            var conditions = await _db.Set<PolicyCondition>().Where(c => ruleIds.Contains(c.PolicyRuleId)).ToListAsync(ct);
            _db.RemoveRange(conditions);
            _db.RemoveRange(await _db.Set<PolicyRule>().Where(r => ruleIds.Contains(r.Id)).ToListAsync(ct));
        }

        AddRules(policy, request.Rules);
        await _db.SaveChangesAsync(ct);
    }

    public async Task PublishAsync(Guid id, CancellationToken ct = default)
    {
        var policy = await _db.AuthorizationPolicies.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct)
            ?? throw new InvalidOperationException("Policy not found.");
        policy.Publish();
        AccessGovernanceOutboxWriter.Enqueue(_db, policy.Id, "PolicyPublished", new { policy.Id, policy.PolicyCode });
        await _db.SaveChangesAsync(ct);
    }

    public async Task ArchiveAsync(Guid id, CancellationToken ct = default)
    {
        var policy = await _db.AuthorizationPolicies.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ct)
            ?? throw new InvalidOperationException("Policy not found.");
        policy.Archive();
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<PolicyDto>> ListAsync(CancellationToken ct = default)
        => await _db.AuthorizationPolicies.Where(p => !p.IsDeleted).OrderBy(p => p.PolicyCode)
            .Select(p => new PolicyDto { Id = p.Id, PolicyCode = p.PolicyCode, PolicyName = p.PolicyName, Status = p.Status.ToString(), Priority = p.Priority }).ToListAsync(ct);

    private static void AddRules(AuthorizationPolicy policy, List<CreatePolicyRuleRequest> rules)
    {
        foreach (var ruleReq in rules)
        {
            var effect = Enum.TryParse<PolicyEffect>(ruleReq.Effect, true, out var e) ? e : PolicyEffect.Permit;
            var rule = policy.AddRule(effect, ruleReq.Priority);
            foreach (var c in ruleReq.Conditions)
                rule.AddCondition(c.Attribute, c.OperatorValueId, c.Value);
        }
    }

    private static PolicyDto Map(AuthorizationPolicy p) => new() { Id = p.Id, PolicyCode = p.PolicyCode, PolicyName = p.PolicyName, Status = p.Status.ToString(), Priority = p.Priority };

    private static PolicyDetailDto MapDetail(AuthorizationPolicy p) => new()
    {
        Id = p.Id,
        PolicyCode = p.PolicyCode,
        PolicyName = p.PolicyName,
        Status = p.Status.ToString(),
        Priority = p.Priority,
        Rules = p.Rules.Select(r => new PolicyRuleDto
        {
            Id = r.Id,
            Effect = r.Effect.ToString(),
            Priority = r.Priority,
            Conditions = r.Conditions.Select(c => new PolicyConditionDto { Attribute = c.Attribute, OperatorValueId = c.OperatorValueId, Value = c.Value }).ToList()
        }).ToList()
    };
}

public class DelegationAppService : IDelegationAppService
{
    private readonly AccessGovernanceDbContext _db;
    public DelegationAppService(AccessGovernanceDbContext db) => _db = db;

    public async Task<DelegationDto> CreateAsync(CreateDelegationRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        await ValidateNoCircularDelegationAsync(request.FromUserId, request.ToUserId, ct);
        if (!await _db.Permissions.AnyAsync(p => p.Id == request.PermissionId && !p.IsDeleted, ct))
            throw new InvalidOperationException("Permission not found.");
        var d = Delegation.Create(request.FromUserId, request.ToUserId, null, null, request.PermissionId, request.OrganizationId, request.HospitalId, request.DepartmentId, request.DelegationReasonValueId, request.EffectiveFrom, request.EffectiveTo, request.RequiresApproval, actorUserId);
        _db.Delegations.Add(d);
        AccessGovernanceOutboxWriter.Enqueue(_db, d.Id, "DelegationCreated", new { d.Id, d.FromUserId, d.ToUserId, request.PermissionId });
        await _db.SaveChangesAsync(ct);
        return Map(d);
    }

    public async Task RevokeAsync(Guid id, Guid actorUserId, CancellationToken ct = default)
    {
        var d = await _db.Delegations.FirstOrDefaultAsync(x => x.Id == id, ct) ?? throw new InvalidOperationException("Delegation not found.");
        d.Revoke(actorUserId);
        AccessGovernanceOutboxWriter.Enqueue(_db, d.Id, "DelegationExpired", new { d.Id, Reason = "Revoked" });
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<DelegationDto>> ListAsync(CancellationToken ct = default)
        => await _db.Delegations.OrderByDescending(d => d.CreatedAt).Select(d => Map(d)).ToListAsync(ct);

    private async Task ValidateNoCircularDelegationAsync(Guid fromUserId, Guid toUserId, CancellationToken ct)
    {
        var reverse = await _db.Delegations.AnyAsync(d =>
            d.FromUserId == toUserId && d.ToUserId == fromUserId && d.Status == DelegationStatus.Active, ct);
        if (reverse) throw new InvalidOperationException("Circular delegation detected.");
    }

    private static DelegationDto Map(Delegation d) => new() { Id = d.Id, FromUserId = d.FromUserId, ToUserId = d.ToUserId, Status = d.Status.ToString(), EffectiveFrom = d.EffectiveFrom, EffectiveTo = d.EffectiveTo };
}

public class ResponsibilityAppService : IResponsibilityAppService
{
    public Task<ResponsibilityDto> AssignAsync(AssignResponsibilityRequest request, Guid actorUserId, CancellationToken ct = default)
        => throw new InvalidOperationException("Responsibility assignments are managed by GovernanceService. Use /api/v1/responsibilities via Gateway.");

    public Task<IReadOnlyList<ResponsibilityDto>> ListAsync(CancellationToken ct = default)
        => throw new InvalidOperationException("Responsibility listings are managed by GovernanceService. Use /api/v1/responsibilities via Gateway.");
}

public class ApprovalAuthorityAppService : IApprovalAuthorityAppService
{
    private readonly AccessGovernanceDbContext _db;
    public ApprovalAuthorityAppService(AccessGovernanceDbContext db) => _db = db;

    public async Task<ApprovalAuthorityDto> SetAsync(SetApprovalAuthorityRequest request, Guid actorUserId, CancellationToken ct = default)
    {
        var a = ApprovalAuthority.Create(request.UserId, request.EmploymentId, request.ApprovalLevelValueId, request.ApprovalType, request.OrganizationId, null, null, request.EffectiveFrom, request.EffectiveTo, actorUserId);
        _db.ApprovalAuthorities.Add(a);
        AccessGovernanceOutboxWriter.Enqueue(_db, a.Id, "ApprovalAuthorityChanged", new { a.UserId, a.ApprovalType });
        await _db.SaveChangesAsync(ct);
        return new ApprovalAuthorityDto { Id = a.Id, UserId = a.UserId, ApprovalType = a.ApprovalType, ApprovalLevelValueId = a.ApprovalLevelValueId };
    }

    public async Task<IReadOnlyList<ApprovalAuthorityDto>> ListAsync(CancellationToken ct = default)
        => await _db.ApprovalAuthorities.Where(a => !a.IsDeleted).Select(a => new ApprovalAuthorityDto { Id = a.Id, UserId = a.UserId, ApprovalType = a.ApprovalType, ApprovalLevelValueId = a.ApprovalLevelValueId }).ToListAsync(ct);
}

public class LookupAppService
{
    private readonly AccessGovernanceDbContext _db;
    public LookupAppService(AccessGovernanceDbContext db) => _db = db;

    public async Task<IReadOnlyList<LookupValueDto>> GetByCategoryAsync(string category, CancellationToken ct = default)
        => await _db.LookupValues
            .Include(v => v.Category)
            .Where(v => v.Category!.Code == category)
            .OrderBy(v => v.SortOrder)
            .Select(v => new LookupValueDto { Id = v.Id, Code = v.Code, Name = v.Name })
            .ToListAsync(ct);
}
