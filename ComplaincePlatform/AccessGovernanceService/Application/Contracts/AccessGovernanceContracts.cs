namespace AccessGovernanceService.Application.Contracts;

public interface IWorkforceServiceClient
{
    Task<EmploymentContextDto?> GetEmploymentAsync(Guid employmentId, CancellationToken cancellationToken = default);
}

public interface IProfessionalServiceClient
{
    Task<ProfessionalContextDto?> GetByPersonIdAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<ProfessionalContextDto?> GetByProfileIdAsync(Guid profileId, CancellationToken cancellationToken = default);
}

public interface IOrganizationServiceClient
{
    Task<MembershipContextDto?> GetMembershipContextAsync(Guid userId, Guid? organizationId = null, CancellationToken cancellationToken = default);
}

public sealed class MembershipContextDto
{
    public Guid OrganizationId { get; init; }
    public Guid HospitalId { get; init; }
    public Guid MembershipId { get; init; }
    public Guid? DepartmentId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string MembershipType { get; init; } = string.Empty;
    public string MembershipStatus { get; init; } = string.Empty;
}

public interface IIdentityServiceClient
{
    Task<bool> UserExistsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class EmploymentContextDto
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
    public Guid? OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid PersonId { get; init; }
}

public sealed class ProfessionalContextDto
{
    public Guid ProfileId { get; init; }
    public Guid PersonId { get; init; }
    public string Category { get; init; } = string.Empty;
    public bool HasActiveRegistration { get; init; }
    public bool HasActiveLicense { get; init; }
    public IReadOnlyList<string> ClinicalPrivileges { get; init; } = Array.Empty<string>();
}

public interface IRoleAppService
{
    Task<RoleDto> CreateAsync(CreateRoleRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<RoleDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResultDto<RoleListItemDto>> ListAsync(int page, int pageSize, CancellationToken ct = default);
    Task AssignRoleAsync(Guid roleId, AssignRoleRequest request, Guid actorUserId, CancellationToken ct = default);
    Task RevokeAssignmentAsync(Guid assignmentId, Guid actorUserId, CancellationToken ct = default);
    Task<IReadOnlyList<RoleAssignmentDto>> ListAssignmentsAsync(Guid? userId, CancellationToken ct = default);
    Task GrantPermissionAsync(Guid roleId, Guid permissionId, Guid actorUserId, CancellationToken ct = default);
    Task UpdateAsync(Guid id, UpdateRoleRequest request, Guid actorUserId, CancellationToken ct = default);
}

public interface IPermissionAppService
{
    Task<PermissionDto> CreateAsync(CreatePermissionRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PermissionDto>> ListAsync(Guid? permissionGroupValueId = null, CancellationToken ct = default);
}

public interface IPolicyAppService
{
    Task<PolicyDto> CreateAsync(CreatePolicyRequest request, CancellationToken ct = default);
    Task<PolicyDetailDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task UpdateAsync(Guid id, UpdatePolicyRequest request, CancellationToken ct = default);
    Task PublishAsync(Guid id, CancellationToken ct = default);
    Task ArchiveAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<PolicyDto>> ListAsync(CancellationToken ct = default);
}

public interface IDelegationAppService
{
    Task<DelegationDto> CreateAsync(CreateDelegationRequest request, Guid actorUserId, CancellationToken ct = default);
    Task RevokeAsync(Guid id, Guid actorUserId, CancellationToken ct = default);
    Task<IReadOnlyList<DelegationDto>> ListAsync(CancellationToken ct = default);
}

public interface IResponsibilityAppService
{
    Task<ResponsibilityDto> AssignAsync(AssignResponsibilityRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<IReadOnlyList<ResponsibilityDto>> ListAsync(CancellationToken ct = default);
}

public interface IApprovalAuthorityAppService
{
    Task<ApprovalAuthorityDto> SetAsync(SetApprovalAuthorityRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<IReadOnlyList<ApprovalAuthorityDto>> ListAsync(CancellationToken ct = default);
}

public interface IAuthorizationEvaluatorService
{
    Task<AuthorizationDecisionDto> EvaluateAsync(EvaluateAuthorizationRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<EffectivePermissionDto>> GetEffectivePermissionsDetailedAsync(Guid userId, CancellationToken ct = default);
}

public sealed class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class RoleDto
{
    public Guid Id { get; init; }
    public string RoleCode { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public string ScopeType { get; init; } = string.Empty;
}

public sealed class RoleListItemDto
{
    public Guid Id { get; init; }
    public string RoleCode { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
}

public sealed class CreateRoleRequest
{
    public string RoleCode { get; init; } = string.Empty;
    public string RoleName { get; init; } = string.Empty;
    public string ScopeType { get; init; } = "Organization";
    public Guid? OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? ParentRoleId { get; init; }
    public Guid? RoleCategoryValueId { get; init; }
    public bool IsEmergency { get; init; }
}

public sealed class UpdateRoleRequest
{
    public string RoleName { get; init; } = string.Empty;
    public Guid? ParentRoleId { get; init; }
    public Guid? RoleCategoryValueId { get; init; }
    public bool IsEmergency { get; init; }
}

public sealed class AssignRoleRequest
{
    public Guid UserId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? EmploymentId { get; init; }
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
}

public sealed class RoleAssignmentDto
{
    public Guid Id { get; init; }
    public Guid RoleDefinitionId { get; init; }
    public string RoleCode { get; init; } = string.Empty;
    public Guid UserId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? EmploymentId { get; init; }
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public bool IsRevoked { get; init; }
}

public sealed class PermissionDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Module { get; init; } = string.Empty;
    public string Feature { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? PermissionGroupValueId { get; init; }
    public string? FieldMask { get; init; }
}

public sealed class CreatePermissionRequest
{
    public string Code { get; init; } = string.Empty;
    public string Module { get; init; } = string.Empty;
    public string Feature { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public Guid? PermissionGroupValueId { get; init; }
    public string? FieldMask { get; init; }
}

public sealed class PolicyDto
{
    public Guid Id { get; init; }
    public string PolicyCode { get; init; } = string.Empty;
    public string PolicyName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Priority { get; init; }
}

public sealed class PolicyDetailDto
{
    public Guid Id { get; init; }
    public string PolicyCode { get; init; } = string.Empty;
    public string PolicyName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int Priority { get; init; }
    public IReadOnlyList<PolicyRuleDto> Rules { get; init; } = Array.Empty<PolicyRuleDto>();
}

public sealed class PolicyRuleDto
{
    public Guid Id { get; init; }
    public string Effect { get; init; } = string.Empty;
    public int Priority { get; init; }
    public IReadOnlyList<PolicyConditionDto> Conditions { get; init; } = Array.Empty<PolicyConditionDto>();
}

public sealed class PolicyConditionDto
{
    public string Attribute { get; init; } = string.Empty;
    public Guid OperatorValueId { get; init; }
    public string Value { get; init; } = string.Empty;
}

public sealed class UpdatePolicyRequest
{
    public string PolicyName { get; init; } = string.Empty;
    public int Priority { get; init; }
    public List<CreatePolicyRuleRequest> Rules { get; init; } = new();
}

public sealed class CreatePolicyRequest
{
    public string PolicyCode { get; init; } = string.Empty;
    public string PolicyName { get; init; } = string.Empty;
    public int Priority { get; init; }
    public Guid? OrganizationId { get; init; }
    public List<CreatePolicyRuleRequest> Rules { get; init; } = new();
}

public sealed class CreatePolicyRuleRequest
{
    public string Effect { get; init; } = "Permit";
    public int Priority { get; init; }
    public List<CreatePolicyConditionRequest> Conditions { get; init; } = new();
}

public sealed class CreatePolicyConditionRequest
{
    public string Attribute { get; init; } = string.Empty;
    public Guid OperatorValueId { get; init; }
    public string Value { get; init; } = string.Empty;
}

public sealed class DelegationDto
{
    public Guid Id { get; init; }
    public Guid FromUserId { get; init; }
    public Guid ToUserId { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
}

public sealed class CreateDelegationRequest
{
    public Guid FromUserId { get; init; }
    public Guid ToUserId { get; init; }
    public Guid PermissionId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? DepartmentId { get; init; }
    public Guid? DelegationReasonValueId { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public bool RequiresApproval { get; init; }
}

public sealed class ResponsibilityDto
{
    public Guid Id { get; init; }
    public Guid SubjectUserId { get; init; }
    public Guid ResponsibilityTypeValueId { get; init; }
    public Guid? ResourceId { get; init; }
}

public sealed class AssignResponsibilityRequest
{
    public Guid SubjectUserId { get; init; }
    public Guid? EmploymentId { get; init; }
    public Guid ResponsibilityTypeValueId { get; init; }
    public Guid? ResourceTypeValueId { get; init; }
    public Guid? ResourceId { get; init; }
    public Guid? OrganizationId { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
}

public sealed class ApprovalAuthorityDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string ApprovalType { get; init; } = string.Empty;
    public Guid ApprovalLevelValueId { get; init; }
}

public sealed class SetApprovalAuthorityRequest
{
    public Guid UserId { get; init; }
    public Guid? EmploymentId { get; init; }
    public Guid ApprovalLevelValueId { get; init; }
    public string ApprovalType { get; init; } = string.Empty;
    public Guid? OrganizationId { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
}

public sealed class EvaluateAuthorizationRequest
{
    public Guid SubjectId { get; init; }
    public Guid? PersonId { get; init; }
    public Guid? EmploymentId { get; init; }
    public Guid? ProfessionalProfileId { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? DepartmentId { get; init; }
    public string Resource { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public Dictionary<string, string> Context { get; init; } = new();
}

public sealed class AuthorizationDecisionDto
{
    public bool Allowed { get; init; }
    public string Reason { get; init; } = string.Empty;
    public IReadOnlyList<string> MatchedRoles { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MatchedPermissions { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MatchedPolicies { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MatchedDelegations { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> MatchedResponsibilities { get; init; } = Array.Empty<string>();
    public IReadOnlyList<EvaluationTraceStepDto> EvaluationTrace { get; init; } = Array.Empty<EvaluationTraceStepDto>();
    public RowLevelFilterDto? RowLevelFilter { get; init; }
}

public sealed record EvaluationTraceStepDto
{
    public string Step { get; init; } = string.Empty;
    public string Outcome { get; init; } = string.Empty;
    public string? Detail { get; init; }
}

public sealed class RowLevelFilterDto
{
    public Guid? OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? DepartmentId { get; init; }
}

public sealed record EffectivePermissionDto
{
    public string Code { get; init; } = string.Empty;
    public IReadOnlyList<string> Sources { get; init; } = Array.Empty<string>();
}

public sealed class LookupValueDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
