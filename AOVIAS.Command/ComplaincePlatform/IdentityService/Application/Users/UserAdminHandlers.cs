using IdentityService.Application.Contracts;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace IdentityService.Application.Users;

public sealed class GetUserAdminByIdHandler(IUserAdminAppService svc) : IRequestHandler<GetUserAdminByIdQuery, UserAdminDetailDto?>
{
    public Task<UserAdminDetailDto?> Handle(GetUserAdminByIdQuery request, CancellationToken ct) => svc.GetByIdAsync(request.UserId, ct);
}

public sealed class BatchGetUsersAdminHandler(IUserAdminAppService svc) : IRequestHandler<BatchGetUsersAdminQuery, IReadOnlyList<UserAdminDetailDto>>
{
    public Task<IReadOnlyList<UserAdminDetailDto>> Handle(BatchGetUsersAdminQuery request, CancellationToken ct) => svc.BatchGetAsync(request.UserIds, ct);
}

public sealed class CreateUserAdminHandler(IUserAdminAppService svc, IHttpContextAccessor http) : IRequestHandler<CreateUserAdminCommand, (UserAdminDetailDto? User, string? Error)>
{
    public Task<(UserAdminDetailDto? User, string? Error)> Handle(CreateUserAdminCommand request, CancellationToken ct)
        => svc.CreateAsync(request.Request, request.ActorUserId, http.HttpContext, ct);
}

public sealed class UpdateUserAdminHandler(IUserAdminAppService svc) : IRequestHandler<UpdateUserAdminCommand, (UserAdminDetailDto? User, string? Error)>
{
    public Task<(UserAdminDetailDto? User, string? Error)> Handle(UpdateUserAdminCommand request, CancellationToken ct)
        => svc.UpdateAsync(request.UserId, request.Request, request.ActorUserId, ct);
}

public sealed class SuspendUserAdminHandler(IUserAdminAppService svc) : IRequestHandler<SuspendUserAdminCommand, (bool Success, string? Error)>
{
    public Task<(bool Success, string? Error)> Handle(SuspendUserAdminCommand request, CancellationToken ct) => svc.SuspendAsync(request.UserId, request.ActorUserId, ct);
}

public sealed class ActivateUserAdminHandler(IUserAdminAppService svc) : IRequestHandler<ActivateUserAdminCommand, (bool Success, string? Error)>
{
    public Task<(bool Success, string? Error)> Handle(ActivateUserAdminCommand request, CancellationToken ct) => svc.ActivateAsync(request.UserId, request.ActorUserId, ct);
}

public sealed class DisableUserAdminHandler(IUserAdminAppService svc) : IRequestHandler<DisableUserAdminCommand, (bool Success, string? Error)>
{
    public Task<(bool Success, string? Error)> Handle(DisableUserAdminCommand request, CancellationToken ct) => svc.DisableAsync(request.UserId, request.ActorUserId, ct);
}

public sealed class SoftDeleteUserAdminHandler(IUserAdminAppService svc) : IRequestHandler<SoftDeleteUserAdminCommand, (bool Success, string? Error)>
{
    public Task<(bool Success, string? Error)> Handle(SoftDeleteUserAdminCommand request, CancellationToken ct) => svc.SoftDeleteAsync(request.UserId, request.ActorUserId, ct);
}

public sealed class UnlockUserAdminHandler(IUserAdminAppService svc) : IRequestHandler<UnlockUserAdminCommand, (bool Success, string? Error)>
{
    public Task<(bool Success, string? Error)> Handle(UnlockUserAdminCommand request, CancellationToken ct) => svc.UnlockAsync(request.UserId, request.ActorUserId, ct);
}

public sealed class ForcePasswordChangeAdminHandler(IUserAdminAppService svc) : IRequestHandler<ForcePasswordChangeAdminCommand, (bool Success, string? Error)>
{
    public Task<(bool Success, string? Error)> Handle(ForcePasswordChangeAdminCommand request, CancellationToken ct) => svc.ForcePasswordChangeAsync(request.UserId, request.ActorUserId, ct);
}

public sealed class AdminResetPasswordHandler(IPasswordService passwords, IHttpContextAccessor http) : IRequestHandler<AdminResetPasswordCommand, (bool Success, string? Message, IReadOnlyList<string>? Errors)>
{
    public async Task<(bool Success, string? Message, IReadOnlyList<string>? Errors)> Handle(AdminResetPasswordCommand request, CancellationToken ct)
    {
        var result = await passwords.AdminResetPasswordAsync(request.UserId, request.NewPassword, request.ActorUserId, http.HttpContext, ct);
        return (result.success, result.message, result.errors);
    }
}
