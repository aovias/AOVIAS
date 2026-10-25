using AccessGovernanceService.Application.Contracts;
using AccessGovernanceService.Domain.Aggregates;
using AccessGovernanceService.Domain.Entities;
using AccessGovernanceService.Domain.Enums;
using AccessGovernanceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AccessGovernanceService.Application.Services;

public class AuthorizationEvaluatorService : IAuthorizationEvaluatorService
{
    private readonly AccessGovernanceDbContext _db;
    private readonly IWorkforceServiceClient _workforce;
    private readonly IProfessionalServiceClient _professional;
    private readonly IOrganizationServiceClient _organization;
    private Dictionary<string, string>? _operatorCodes;

    public AuthorizationEvaluatorService(
        AccessGovernanceDbContext db,
        IWorkforceServiceClient workforce,
        IProfessionalServiceClient professional,
        IOrganizationServiceClient organization)
    {
        _db = db;
        _workforce = workforce;
        _professional = professional;
        _organization = organization;
    }

    public async Task<AuthorizationDecisionDto> EvaluateAsync(EvaluateAuthorizationRequest request, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var permissionCode = BuildPermissionCode(request.Resource, request.Action);
        var trace = new List<EvaluationTraceStepDto>();
        var matchedRoles = new List<string>();
        var matchedPermissions = new List<string>();
        var matchedPolicies = new List<string>();
        var matchedDelegations = new List<string>();
        var matchedResponsibilities = new List<string>();

        EmploymentContextDto? employment = null;
        if (request.EmploymentId.HasValue)
            employment = await _workforce.GetEmploymentAsync(request.EmploymentId.Value, ct);

        ProfessionalContextDto? professional = null;
        if (request.ProfessionalProfileId.HasValue)
            professional = await _professional.GetByProfileIdAsync(request.ProfessionalProfileId.Value, ct);
        else if (request.PersonId.HasValue)
            professional = await _professional.GetByPersonIdAsync(request.PersonId.Value, ct);

        MembershipContextDto? membership = null;
        if (request.SubjectId != Guid.Empty)
            membership = await _organization.GetMembershipContextAsync(request.SubjectId, request.OrganizationId, ct);

        var attributes = BuildAttributes(request, employment, professional, membership);
        await EnsureOperatorCodesAsync(ct);

        // 1. Explicit deny policies
        trace.Add(new EvaluationTraceStepDto { Step = "DenyPolicy", Outcome = "Checking" });
        var denyPolicy = await EvaluatePoliciesAsync(request, attributes, PolicyEffect.Deny, now, ct);
        if (denyPolicy != null)
        {
            matchedPolicies.Add(denyPolicy);
            trace[^1] = trace[^1] with { Outcome = "Matched", Detail = denyPolicy };
            return await PersistAndReturn(request, false, "Explicit deny policy matched", matchedRoles, matchedPermissions, matchedPolicies, matchedDelegations, matchedResponsibilities, trace, null, ct);
        }
        trace[^1] = trace[^1] with { Outcome = "NoMatch" };

        // 2. Active delegations (scoped, permission required)
        trace.Add(new EvaluationTraceStepDto { Step = "Delegation", Outcome = "Checking" });
        var delegations = await _db.Delegations
            .Where(d => d.ToUserId == request.SubjectId && d.Status == DelegationStatus.Active)
            .ToListAsync(ct);
        foreach (var d in delegations.Where(x => x.IsActiveAt(now)))
        {
            if (!MatchesScope(d.OrganizationId, d.HospitalId, d.DepartmentId, request))
                continue;
            if (!d.PermissionId.HasValue)
                continue;
            var perm = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == d.PermissionId && !p.IsDeleted, ct);
            if (perm != null && string.Equals(perm.Code, permissionCode, StringComparison.OrdinalIgnoreCase))
            {
                matchedDelegations.Add(d.Id.ToString());
                matchedPermissions.Add(perm.Code);
                trace[^1] = trace[^1] with { Outcome = "Matched", Detail = d.Id.ToString() };
                return await PersistAndReturn(request, true, "Delegated permission", matchedRoles, matchedPermissions, matchedPolicies, matchedDelegations, matchedResponsibilities, trace, null, ct);
            }
        }
        trace[^1] = trace[^1] with { Outcome = "NoMatch" };

        // 3. Approval authority for approve actions
        if (request.Action.Contains("Approve", StringComparison.OrdinalIgnoreCase))
        {
            trace.Add(new EvaluationTraceStepDto { Step = "ApprovalAuthority", Outcome = "Checking" });
            var authorities = await _db.ApprovalAuthorities
                .Where(a => a.UserId == request.SubjectId && !a.IsDeleted)
                .ToListAsync(ct);
            var active = authorities.FirstOrDefault(a =>
                a.IsActiveAt(now) &&
                MatchesApprovalType(a.ApprovalType, request.Resource) &&
                MatchesScope(a.OrganizationId, a.HospitalId, a.DepartmentId, request));
            if (active != null)
            {
                matchedRoles.Add($"ApprovalAuthority:{active.ApprovalType}");
                trace[^1] = trace[^1] with { Outcome = "Matched", Detail = active.ApprovalType };
                return await PersistAndReturn(request, true, "Approval authority matched", matchedRoles, matchedPermissions, matchedPolicies, matchedDelegations, matchedResponsibilities, trace, null, ct);
            }
            trace[^1] = trace[^1] with { Outcome = "NoMatch" };
        }

        // 4. RBAC via scoped role assignments (with inherited roles)
        trace.Add(new EvaluationTraceStepDto { Step = "RBAC", Outcome = "Checking" });
        var assignments = await _db.RoleAssignments
            .Where(a => a.UserId == request.SubjectId && !a.IsRevoked)
            .ToListAsync(ct);
        var activeAssignments = assignments.Where(a => a.IsActiveAt(now) && MatchesAssignmentScope(a, request)).ToList();
        var roleIds = await ResolveRoleIdsWithInheritanceAsync(activeAssignments.Select(a => a.RoleDefinitionId).Distinct().ToList(), ct);
        var roles = await _db.RoleDefinitions.Where(r => roleIds.Contains(r.Id) && !r.IsDeleted).ToListAsync(ct);
        var grants = await _db.PermissionAssignments
            .Where(pa => roleIds.Contains(pa.RoleDefinitionId) && !pa.IsRevoked)
            .Join(_db.Permissions.Where(p => !p.IsDeleted), pa => pa.PermissionId, p => p.Id, (pa, p) => new { pa.RoleDefinitionId, p.Code })
            .ToListAsync(ct);

        foreach (var role in roles)
        {
            var rolePerms = grants.Where(g => g.RoleDefinitionId == role.Id).Select(g => g.Code).ToList();
            if (rolePerms.Any(c => string.Equals(c, permissionCode, StringComparison.OrdinalIgnoreCase)))
            {
                matchedRoles.Add(role.RoleCode);
                matchedPermissions.AddRange(rolePerms.Where(c => string.Equals(c, permissionCode, StringComparison.OrdinalIgnoreCase)));

                // 5. RLS — resource scope check after permission grant
                trace.Add(new EvaluationTraceStepDto { Step = "RowLevelSecurity", Outcome = "Checking" });
                var rls = EvaluateRowLevelSecurity(activeAssignments, request);
                if (!rls.Allowed)
                {
                    trace[^1] = trace[^1] with { Outcome = "Denied", Detail = rls.Reason };
                    trace[^2] = trace[^2] with { Outcome = "MatchedButRlsDenied", Detail = role.RoleCode };
                    return await PersistAndReturn(request, false, rls.Reason, matchedRoles, matchedPermissions, matchedPolicies, matchedDelegations, matchedResponsibilities, trace, rls.Filter, ct);
                }
                trace[^1] = trace[^1] with { Outcome = "Passed" };
                trace[^2] = trace[^2] with { Outcome = "Matched", Detail = role.RoleCode };
                return await PersistAndReturn(request, true, "Role permission granted", matchedRoles, matchedPermissions, matchedPolicies, matchedDelegations, matchedResponsibilities, trace, rls.Filter, ct);
            }
        }
        trace[^1] = trace[^1] with { Outcome = "NoMatch" };

        // 6. ABAC permit policies
        trace.Add(new EvaluationTraceStepDto { Step = "PermitPolicy", Outcome = "Checking" });
        var permitPolicy = await EvaluatePoliciesAsync(request, attributes, PolicyEffect.Permit, now, ct);
        if (permitPolicy != null)
        {
            matchedPolicies.Add(permitPolicy);
            trace[^1] = trace[^1] with { Outcome = "Matched", Detail = permitPolicy };
            return await PersistAndReturn(request, true, "Policy permit matched", matchedRoles, matchedPermissions, matchedPolicies, matchedDelegations, matchedResponsibilities, trace, null, ct);
        }
        trace[^1] = trace[^1] with { Outcome = "NoMatch" };

        // 7. Responsibilities (informational)
        var responsibilities = await _db.ResponsibilityAssignments
            .Where(r => r.SubjectUserId == request.SubjectId && !r.IsDeleted)
            .ToListAsync(ct);
        foreach (var r in responsibilities.Where(x => x.IsActiveAt(now)))
            matchedResponsibilities.Add(r.Id.ToString());

        trace.Add(new EvaluationTraceStepDto { Step = "DefaultDeny", Outcome = "Denied" });
        return await PersistAndReturn(request, false, "Default deny — no matching grant", matchedRoles, matchedPermissions, matchedPolicies, matchedDelegations, matchedResponsibilities, trace, null, ct);
    }

    public async Task<IReadOnlyList<EffectivePermissionDto>> GetEffectivePermissionsDetailedAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var result = new Dictionary<string, EffectivePermissionDto>(StringComparer.OrdinalIgnoreCase);

        var assignments = await _db.RoleAssignments
            .Where(a => a.UserId == userId && !a.IsRevoked)
            .ToListAsync(ct);
        var activeRoleIds = assignments.Where(a => a.IsActiveAt(now)).Select(a => a.RoleDefinitionId).Distinct().ToList();
        var allRoleIds = await ResolveRoleIdsWithInheritanceAsync(activeRoleIds, ct);
        var roleCodes = await _db.RoleDefinitions.Where(r => allRoleIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.RoleCode, ct);
        var rbacGrants = await _db.PermissionAssignments
            .Where(pa => allRoleIds.Contains(pa.RoleDefinitionId) && !pa.IsRevoked)
            .Join(_db.Permissions.Where(p => !p.IsDeleted), pa => pa.PermissionId, p => p.Id, (pa, p) => new { pa.RoleDefinitionId, p.Code })
            .ToListAsync(ct);
        foreach (var g in rbacGrants)
            AddEffective(result, g.Code, "Role", roleCodes.GetValueOrDefault(g.RoleDefinitionId, g.RoleDefinitionId.ToString()));

        var delegations = await _db.Delegations
            .Where(d => d.ToUserId == userId && d.Status == DelegationStatus.Active && d.PermissionId != null)
            .ToListAsync(ct);
        foreach (var d in delegations.Where(x => x.IsActiveAt(now)))
        {
            var perm = await _db.Permissions.FirstOrDefaultAsync(p => p.Id == d.PermissionId && !p.IsDeleted, ct);
            if (perm != null)
                AddEffective(result, perm.Code, "Delegation", d.Id.ToString());
        }

        return result.Values.OrderBy(v => v.Code).ToList();
    }

    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken ct = default)
    {
        var detailed = await GetEffectivePermissionsDetailedAsync(userId, ct);
        return detailed.Select(d => d.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void AddEffective(Dictionary<string, EffectivePermissionDto> map, string code, string source, string sourceRef)
    {
        if (map.TryGetValue(code, out var existing))
        {
            map[code] = existing with { Sources = existing.Sources.Concat(new[] { $"{source}:{sourceRef}" }).Distinct().ToList() };
        }
        else
        {
            map[code] = new EffectivePermissionDto { Code = code, Sources = new List<string> { $"{source}:{sourceRef}" } };
        }
    }

    private async Task<List<Guid>> ResolveRoleIdsWithInheritanceAsync(List<Guid> seedRoleIds, CancellationToken ct)
    {
        var allRoles = await _db.RoleDefinitions.Where(r => !r.IsDeleted).Select(r => new { r.Id, r.ParentRoleId }).ToListAsync(ct);
        var parentMap = allRoles.ToDictionary(r => r.Id, r => r.ParentRoleId);
        var result = new HashSet<Guid>(seedRoleIds);
        var visiting = new HashSet<Guid>();

        foreach (var id in seedRoleIds)
            CollectParents(id, parentMap, result, visiting);

        return result.ToList();
    }

    private static void CollectParents(Guid roleId, Dictionary<Guid, Guid?> parentMap, HashSet<Guid> result, HashSet<Guid> visiting)
    {
        if (!parentMap.TryGetValue(roleId, out var parentId) || !parentId.HasValue)
            return;
        if (!visiting.Add(roleId))
            throw new InvalidOperationException("Circular role inheritance detected.");
        result.Add(parentId.Value);
        CollectParents(parentId.Value, parentMap, result, visiting);
        visiting.Remove(roleId);
    }

    private async Task<string?> EvaluatePoliciesAsync(EvaluateAuthorizationRequest request, Dictionary<string, string> attributes, PolicyEffect effect, DateTime now, CancellationToken ct)
    {
        var policies = await _db.AuthorizationPolicies
            .Include(p => p.Rules).ThenInclude(r => r.Conditions)
            .Where(p => p.Status == PolicyStatus.Published && !p.IsDeleted)
            .OrderByDescending(p => p.Priority)
            .ToListAsync(ct);

        foreach (var policy in policies)
        {
            foreach (var rule in policy.Rules.Where(r => r.Effect == effect).OrderByDescending(r => r.Priority))
            {
                if (rule.Conditions.All(c => EvaluateCondition(c, attributes, request)))
                    return policy.PolicyCode;
            }
        }
        return null;
    }

    private bool EvaluateCondition(PolicyCondition condition, Dictionary<string, string> attributes, EvaluateAuthorizationRequest request)
    {
        var attr = condition.Attribute.ToLowerInvariant();
        var expected = condition.Value;
        string? actual = null;
        if (attr == "resource")
            actual = request.Resource;
        else if (attr == "action")
            actual = request.Action;
        else
            attributes.TryGetValue(attr, out actual);

        var op = GetOperatorCode(condition.OperatorValueId);
        return EvaluateOperator(op, actual, expected);
    }

    private string GetOperatorCode(Guid operatorValueId)
    {
        if (_operatorCodes != null && _operatorCodes.TryGetValue(operatorValueId.ToString(), out var code))
            return code;
        return "Equals";
    }

    private async Task EnsureOperatorCodesAsync(CancellationToken ct)
    {
        if (_operatorCodes != null) return;
        var ops = await _db.LookupValues
            .Include(v => v.Category)
            .Where(v => v.Category!.Code == "PolicyOperator")
            .ToListAsync(ct);
        _operatorCodes = ops.ToDictionary(v => v.Id.ToString(), v => v.Code, StringComparer.OrdinalIgnoreCase);
    }

    private static bool EvaluateOperator(string op, string? actual, string expected)
    {
        actual ??= string.Empty;
        return op.ToUpperInvariant() switch
        {
            "NOTEQUALS" => !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
            "IN" => expected.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Any(v => string.Equals(actual, v, StringComparison.OrdinalIgnoreCase)),
            "CONTAINS" => actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
            "GREATERTHAN" => double.TryParse(actual, out var a) && double.TryParse(expected, out var e) && a > e,
            "EXISTS" => !string.IsNullOrWhiteSpace(actual),
            _ => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static Dictionary<string, string> BuildAttributes(
        EvaluateAuthorizationRequest request,
        EmploymentContextDto? employment,
        ProfessionalContextDto? professional,
        MembershipContextDto? membership)
    {
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["resource"] = request.Resource,
            ["action"] = request.Action
        };
        if (request.OrganizationId.HasValue) attrs["organization_id"] = request.OrganizationId.Value.ToString();
        if (request.HospitalId.HasValue) attrs["hospital_id"] = request.HospitalId.Value.ToString();
        if (request.DepartmentId.HasValue) attrs["department_id"] = request.DepartmentId.Value.ToString();
        if (employment != null)
        {
            attrs["employment_status"] = employment.Status;
            if (employment.DepartmentId.HasValue) attrs["department_id"] = employment.DepartmentId.Value.ToString();
        }
        if (professional != null)
        {
            attrs["professional_category"] = professional.Category;
            attrs["has_active_registration"] = professional.HasActiveRegistration.ToString();
            attrs["has_active_license"] = professional.HasActiveLicense.ToString();
            attrs["credential_approved"] = (professional.HasActiveRegistration && professional.HasActiveLicense).ToString();
            if (professional.ClinicalPrivileges.Count > 0)
            {
                attrs["clinical_privilege"] = professional.ClinicalPrivileges[0];
                attrs["clinical_privilege_active"] = "True";
            }
        }
        if (membership != null)
        {
            attrs["membership_type"] = membership.MembershipType;
            attrs["membership_status"] = membership.MembershipStatus;
            if (!string.IsNullOrEmpty(membership.Role))
                attrs["membership_role"] = membership.Role;
        }
        foreach (var kv in request.Context)
            attrs[kv.Key] = kv.Value;
        return attrs;
    }

    private static string BuildPermissionCode(string resource, string action)
    {
        if (action.Count(c => c == '.') >= 2)
            return action;
        return $"{resource}.{action}";
    }

    private static bool MatchesApprovalType(string approvalType, string resource) =>
        string.Equals(approvalType, resource, StringComparison.OrdinalIgnoreCase) ||
        resource.Contains(approvalType, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesScope(Guid? orgId, Guid? hospitalId, Guid? departmentId, EvaluateAuthorizationRequest request) =>
        MatchesScopeValues(orgId, hospitalId, departmentId, request.OrganizationId, request.HospitalId, request.DepartmentId);

    private static bool MatchesAssignmentScope(RoleAssignment assignment, EvaluateAuthorizationRequest request)
    {
        if (assignment.EmploymentId.HasValue && request.EmploymentId.HasValue && assignment.EmploymentId != request.EmploymentId)
            return false;
        return MatchesScopeValues(assignment.OrganizationId, assignment.HospitalId, assignment.DepartmentId, request.OrganizationId, request.HospitalId, request.DepartmentId);
    }

    private static bool MatchesScopeValues(Guid? assignOrg, Guid? assignHospital, Guid? assignDept, Guid? reqOrg, Guid? reqHospital, Guid? reqDept)
    {
        if (assignOrg.HasValue && reqOrg.HasValue && assignOrg != reqOrg) return false;
        if (assignOrg.HasValue && !reqOrg.HasValue) return false;
        if (assignHospital.HasValue && reqHospital.HasValue && assignHospital != reqHospital) return false;
        if (assignHospital.HasValue && !reqHospital.HasValue) return false;
        if (assignDept.HasValue && reqDept.HasValue && assignDept != reqDept) return false;
        if (assignDept.HasValue && !reqDept.HasValue) return false;
        return true;
    }

    private static (bool Allowed, string Reason, RowLevelFilterDto? Filter) EvaluateRowLevelSecurity(IReadOnlyList<RoleAssignment> assignments, EvaluateAuthorizationRequest request)
    {
        request.Context.TryGetValue("resource_department_id", out var resourceDept);
        request.Context.TryGetValue("resource_hospital_id", out var resourceHospital);
        request.Context.TryGetValue("resource_organization_id", out var resourceOrg);

        if (string.IsNullOrEmpty(resourceDept) && string.IsNullOrEmpty(resourceHospital) && string.IsNullOrEmpty(resourceOrg))
            return (true, string.Empty, null);

        foreach (var a in assignments)
        {
            if (a.DepartmentId.HasValue && !string.IsNullOrEmpty(resourceDept) && a.DepartmentId.Value.ToString() != resourceDept)
                continue;
            if (a.HospitalId.HasValue && !string.IsNullOrEmpty(resourceHospital) && a.HospitalId.Value.ToString() != resourceHospital)
                continue;
            if (a.OrganizationId.HasValue && !string.IsNullOrEmpty(resourceOrg) && a.OrganizationId.Value.ToString() != resourceOrg)
                continue;

            var filter = new RowLevelFilterDto
            {
                OrganizationId = a.OrganizationId,
                HospitalId = a.HospitalId,
                DepartmentId = a.DepartmentId
            };
            return (true, string.Empty, filter);
        }

        return (false, "Row-level scope denied — resource outside assignment scope", null);
    }

    private async Task<AuthorizationDecisionDto> PersistAndReturn(
        EvaluateAuthorizationRequest request, bool allowed, string reason,
        List<string> matchedRoles, List<string> matchedPermissions, List<string> matchedPolicies,
        List<string> matchedDelegations, List<string> matchedResponsibilities,
        List<EvaluationTraceStepDto> trace, RowLevelFilterDto? rowLevelFilter, CancellationToken ct)
    {
        var decision = new AuthorizationDecisionDto
        {
            Allowed = allowed,
            Reason = reason,
            MatchedRoles = matchedRoles.Distinct().ToList(),
            MatchedPermissions = matchedPermissions.Distinct().ToList(),
            MatchedPolicies = matchedPolicies.Distinct().ToList(),
            MatchedDelegations = matchedDelegations.Distinct().ToList(),
            MatchedResponsibilities = matchedResponsibilities.Distinct().ToList(),
            EvaluationTrace = trace,
            RowLevelFilter = rowLevelFilter
        };
        _db.AccessEvaluations.Add(new AccessEvaluation
        {
            Id = Guid.NewGuid(),
            SubjectUserId = request.SubjectId,
            Resource = request.Resource,
            Action = request.Action,
            Allowed = allowed,
            Reason = reason,
            DecisionJson = JsonSerializer.Serialize(decision),
            EvaluatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return decision;
    }
}
