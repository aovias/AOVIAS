namespace OrganizationService.Application.Contracts;

public sealed class PagedResultDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public sealed class MembershipDto
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public Guid? PersonId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? EmploymentId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string MembershipStatus { get; init; } = string.Empty;
    public bool IsPrimary { get; init; }
    public DateTime EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? DepartmentName { get; init; }
    public string? Designation { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? ProfilePicUrl { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class CreateMembershipRequest
{
    public Guid OrganizationId { get; init; }
    public Guid UserId { get; init; }
    public Guid? PersonId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? EmploymentId { get; init; }
    public Guid? MembershipTypeValueId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string MembershipStatus { get; init; } = "Active";
    public bool IsPrimary { get; init; } = true;
    public DateTime? EffectiveFrom { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? Designation { get; init; }
    public string? Email { get; init; }
}

public sealed class UpdateMembershipRequest
{
    public string? Role { get; init; }
    public string? MembershipStatus { get; init; }
    public bool? IsPrimary { get; init; }
    public DateTime? EffectiveTo { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? Designation { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
}

public sealed class TransferMembershipRequest
{
    public Guid MembershipId { get; init; }
    public Guid TargetOrganizationId { get; init; }
    public Guid? TargetHospitalId { get; init; }
    public string? Reason { get; init; }
}

public sealed class MembershipHistoryDto
{
    public Guid Id { get; init; }
    public Guid MembershipId { get; init; }
    public string FromStatus { get; init; } = string.Empty;
    public string ToStatus { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public DateTime RecordedAt { get; init; }
}

public sealed class CreateInvitationRequest
{
    public Guid OrganizationId { get; init; }
    public Guid HospitalId { get; init; }
    public Guid? DepartmentId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime? ExpiresAt { get; init; }
    public string? Designation { get; init; }
    public string? EmploymentType { get; init; }
    public Guid? ReportsToUserId { get; init; }
    public Guid? ManagerEmploymentId { get; init; }
    public bool? RequireSeat { get; init; }
    public Guid? PositionId { get; init; }
}

public sealed class BulkCreateInvitationsRequest
{
    public Guid OrganizationId { get; init; }
    public Guid HospitalId { get; init; }
    public IReadOnlyList<CreateInvitationRequest> Items { get; init; } = Array.Empty<CreateInvitationRequest>();
}

public sealed class InvitationDto
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid HospitalId { get; init; }
    public Guid? DepartmentId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public string? Designation { get; init; }
    public string EmploymentType { get; init; } = "Permanent";
    public bool RequireSeat { get; init; }
    public Guid? PositionId { get; init; }
    public Guid? ReportsToUserId { get; init; }
    public Guid? ManagerEmploymentId { get; init; }
    public string? PipelineStep { get; init; }
    public string? PipelineError { get; init; }
    public Guid? MembershipId { get; init; }
    public Guid? EmploymentId { get; init; }
    public Guid? PersonId { get; init; }
    public bool SeatAssigned { get; init; }
    public DateTime? AcceptedAt { get; init; }
    public Guid? AcceptedUserId { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed class InvitationRedeemPreviewDto
{
    public Guid InvitationId { get; init; }
    public string Email { get; init; } = string.Empty;
    public Guid OrganizationId { get; init; }
    public string? OrganizationName { get; init; }
    public Guid HospitalId { get; init; }
    public string Role { get; init; } = string.Empty;
    public string? Designation { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public bool RequireSeat { get; init; }
    public string? PipelineStep { get; init; }
}

public sealed class InvitationPipelineUpdateRequest
{
    public string? PipelineStep { get; init; }
    public string? PipelineError { get; init; }
    public Guid? MembershipId { get; init; }
    public Guid? EmploymentId { get; init; }
    public Guid? PersonId { get; init; }
    public bool? SeatAssigned { get; init; }
    public Guid? AcceptedUserId { get; init; }
    public bool MarkAccepted { get; init; }
}

public sealed class CreateOrganizationalUnitRequest
{
    public Guid OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? ParentUnitId { get; init; }
    public Guid UnitTypeValueId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class UpdateOrganizationalUnitRequest
{
    public string? Code { get; init; }
    public string? Name { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? UnitTypeValueId { get; init; }
}

public sealed class MoveOrganizationalUnitRequest
{
    public Guid? NewParentUnitId { get; init; }
}

public sealed class OrganizationalUnitDto
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid? ParentUnitId { get; init; }
    public Guid UnitTypeValueId { get; init; }
    public string UnitTypeCode { get; init; } = string.Empty;
    public string UnitTypeName { get; init; } = string.Empty;
    public string Status { get; init; } = "Active";
    public bool IsActive => string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase);
    public IReadOnlyList<OrganizationalUnitDto> Children { get; init; } = Array.Empty<OrganizationalUnitDto>();
}

public sealed class OrganizationalUnitTypeDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

public sealed class CreateOrgDepartmentRequest
{
    public Guid OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? ParentDepartmentId { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public Guid? DepartmentTypeValueId { get; init; }
}

public sealed class DepartmentTreeNodeDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
    public Guid? HospitalId { get; init; }
    public Guid? ParentDepartmentId { get; init; }
    public IReadOnlyList<DepartmentTreeNodeDto> Children { get; init; } = Array.Empty<DepartmentTreeNodeDto>();
}

public sealed class OrganizationTreeNodeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string NodeType { get; init; } = string.Empty;
    public IReadOnlyList<OrganizationTreeNodeDto> Children { get; init; } = Array.Empty<OrganizationTreeNodeDto>();
}

public sealed class GraphNodeDto
{
    public string Id { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string NodeType { get; init; } = string.Empty;
}

public sealed class GraphEdgeDto
{
    public string From { get; init; } = string.Empty;
    public string To { get; init; } = string.Empty;
    public string Relation { get; init; } = string.Empty;
}

public sealed class OrganizationGraphDto
{
    public IReadOnlyList<GraphNodeDto> Nodes { get; init; } = Array.Empty<GraphNodeDto>();
    public IReadOnlyList<GraphEdgeDto> Edges { get; init; } = Array.Empty<GraphEdgeDto>();
}

public interface IOrganizationTimelineService
{
    Task AppendAsync(string eventType, string summary, Guid? organizationId, string? resourceType, Guid? resourceId, Guid? actorUserId, object? payload = null, CancellationToken ct = default);
}

public interface IMembershipAppService
{
    Task<MembershipDto> CreateAsync(CreateMembershipRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<MembershipDto?> UpdateAsync(Guid id, UpdateMembershipRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<PagedResultDto<MembershipDto>> ListAsync(
        Guid? organizationId,
        Guid? userId,
        int page,
        int pageSize,
        string? search = null,
        string? role = null,
        Guid? departmentId = null,
        string? membershipStatus = null,
        bool includeInactive = false,
        CancellationToken ct = default);
    Task<IReadOnlyList<MembershipHistoryDto>> GetHistoryAsync(Guid membershipId, CancellationToken ct = default);
    Task<MembershipDto> TransferAsync(TransferMembershipRequest request, Guid actorUserId, CancellationToken ct = default);
}

public interface IInvitationAppService
{
    Task<InvitationDto> CreateAsync(CreateInvitationRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<IReadOnlyList<InvitationDto>> BulkCreateAsync(BulkCreateInvitationsRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<PagedResultDto<InvitationDto>> ListAsync(Guid organizationId, string? status, int page, int pageSize, CancellationToken ct = default);
    Task<InvitationDto?> GetAsync(Guid invitationId, CancellationToken ct = default);
    Task<InvitationRedeemPreviewDto?> PreviewByTokenAsync(string rawToken, CancellationToken ct = default);
    Task<InvitationDto?> GetByTokenAsync(string rawToken, CancellationToken ct = default);
    Task<InvitationDto> ResendAsync(Guid invitationId, Guid actorUserId, CancellationToken ct = default);
    Task CancelAsync(Guid invitationId, Guid actorUserId, CancellationToken ct = default);
    Task<MembershipDto> AcceptAsync(Guid invitationId, Guid userId, CancellationToken ct = default);
    Task RevokeAsync(Guid invitationId, Guid actorUserId, CancellationToken ct = default);
    Task<InvitationDto> UpdatePipelineAsync(Guid invitationId, InvitationPipelineUpdateRequest request, CancellationToken ct = default);
    Task LinkEmploymentAsync(Guid membershipId, Guid employmentId, Guid? personId, CancellationToken ct = default);
}

public interface IOrganizationalUnitAppService
{
    Task<OrganizationalUnitDto> CreateAsync(CreateOrganizationalUnitRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<OrganizationalUnitDto> UpdateAsync(Guid id, UpdateOrganizationalUnitRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<OrganizationalUnitDto> MoveAsync(Guid id, MoveOrganizationalUnitRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<OrganizationalUnitDto> SetStatusAsync(Guid id, string status, Guid actorUserId, CancellationToken ct = default);
    Task SoftDeleteAsync(Guid id, Guid actorUserId, CancellationToken ct = default);
    Task<IReadOnlyList<OrganizationalUnitDto>> GetTreeAsync(Guid organizationId, Guid? hospitalId, bool includeInactive = false, CancellationToken ct = default);
    Task<IReadOnlyList<OrganizationalUnitTypeDto>> GetUnitTypesAsync(CancellationToken ct = default);
}

public interface IDepartmentHierarchyAppService
{
    Task<DepartmentTreeNodeDto> CreateAsync(CreateOrgDepartmentRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<Shared.Infrastructure.DTO.IntegrationUpsertResponse> UpsertFromIntegrationAsync(Shared.Infrastructure.DTO.IntegrationUpsertRequest request, CancellationToken ct = default);
    Task<bool> RollbackIntegrationAsync(Guid organizationId, Guid departmentId, CancellationToken ct = default);
    Task<IReadOnlyList<DepartmentTreeNodeDto>> GetTreeAsync(Guid organizationId, Guid? hospitalId, CancellationToken ct = default);
}

public interface IOrganizationGraphAppService
{
    Task<OrganizationGraphDto> GetGraphAsync(Guid organizationId, CancellationToken ct = default);
    Task<OrganizationTreeNodeDto?> GetOrganizationTreeAsync(Guid organizationId, CancellationToken ct = default);
}
