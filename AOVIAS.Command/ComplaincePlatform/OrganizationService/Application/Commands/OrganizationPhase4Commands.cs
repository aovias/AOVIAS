using MediatR;
using OrganizationService.Application.Contracts;

namespace OrganizationService.Application.Commands;

public record CreateMembershipCommand(CreateMembershipRequest Request, Guid ActorUserId) : IRequest<MembershipDto>;
public record UpdateMembershipCommand(Guid Id, UpdateMembershipRequest Request, Guid ActorUserId) : IRequest<MembershipDto?>;
public record TransferMembershipCommand(TransferMembershipRequest Request, Guid ActorUserId) : IRequest<MembershipDto>;
public record CreateInvitationCommand(CreateInvitationRequest Request, Guid ActorUserId) : IRequest<InvitationDto>;
public record BulkCreateInvitationsCommand(BulkCreateInvitationsRequest Request, Guid ActorUserId) : IRequest<IReadOnlyList<InvitationDto>>;
public record AcceptInvitationCommand(Guid InvitationId, Guid UserId) : IRequest<MembershipDto>;
public record RevokeInvitationCommand(Guid InvitationId, Guid ActorUserId) : IRequest<Unit>;
public record ResendInvitationCommand(Guid InvitationId, Guid ActorUserId) : IRequest<InvitationDto>;
public record CancelInvitationCommand(Guid InvitationId, Guid ActorUserId) : IRequest<Unit>;
public record ListInvitationsQuery(Guid OrganizationId, string? Status, int Page, int PageSize) : IRequest<PagedResultDto<InvitationDto>>;
public record CreateOrganizationalUnitCommand(CreateOrganizationalUnitRequest Request, Guid ActorUserId) : IRequest<OrganizationalUnitDto>;
public record UpdateOrganizationalUnitCommand(Guid Id, UpdateOrganizationalUnitRequest Request, Guid ActorUserId) : IRequest<OrganizationalUnitDto>;
public record MoveOrganizationalUnitCommand(Guid Id, MoveOrganizationalUnitRequest Request, Guid ActorUserId) : IRequest<OrganizationalUnitDto>;
public record SetOrganizationalUnitStatusCommand(Guid Id, string Status, Guid ActorUserId) : IRequest<OrganizationalUnitDto>;
public record SoftDeleteOrganizationalUnitCommand(Guid Id, Guid ActorUserId) : IRequest<Unit>;
public record CreateOrgDepartmentCommand(CreateOrgDepartmentRequest Request, Guid ActorUserId) : IRequest<DepartmentTreeNodeDto>;
