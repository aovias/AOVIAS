using MediatR;

namespace IdentityService.Application.Users;

public sealed record GetUserAdminByIdQuery(Guid UserId) : IRequest<UserAdminDetailDto?>;
public sealed record BatchGetUsersAdminQuery(IReadOnlyList<Guid> UserIds) : IRequest<IReadOnlyList<UserAdminDetailDto>>;
public sealed record CreateUserAdminCommand(CreateUserAdminRequest Request, Guid ActorUserId) : IRequest<(UserAdminDetailDto? User, string? Error)>;
public sealed record UpdateUserAdminCommand(Guid UserId, UpdateUserAdminRequest Request, Guid ActorUserId) : IRequest<(UserAdminDetailDto? User, string? Error)>;
public sealed record SuspendUserAdminCommand(Guid UserId, Guid ActorUserId) : IRequest<(bool Success, string? Error)>;
public sealed record ActivateUserAdminCommand(Guid UserId, Guid ActorUserId) : IRequest<(bool Success, string? Error)>;
public sealed record DisableUserAdminCommand(Guid UserId, Guid ActorUserId) : IRequest<(bool Success, string? Error)>;
public sealed record SoftDeleteUserAdminCommand(Guid UserId, Guid ActorUserId) : IRequest<(bool Success, string? Error)>;
public sealed record UnlockUserAdminCommand(Guid UserId, Guid ActorUserId) : IRequest<(bool Success, string? Error)>;
public sealed record ForcePasswordChangeAdminCommand(Guid UserId, Guid ActorUserId) : IRequest<(bool Success, string? Error)>;
public sealed record AdminResetPasswordCommand(Guid UserId, string NewPassword, Guid ActorUserId) : IRequest<(bool Success, string? Message, IReadOnlyList<string>? Errors)>;
