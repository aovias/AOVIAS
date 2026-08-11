namespace IdentityService.Application.Users;

public sealed class UserAdminDetailDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public string? PhoneNumber { get; init; }
    public string PlatformId { get; init; } = string.Empty;
    public Guid? PersonId { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsDeleted { get; init; }
    public bool ForcePasswordChange { get; init; }
    public bool PasswordResetRequired { get; init; }
    public bool LockoutEnabled { get; init; }
    public DateTimeOffset? LockoutEnd { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = Array.Empty<string>();
}

public sealed class CreateUserAdminRequest
{
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string TemporaryPassword { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Role { get; set; }
    public Guid? PersonId { get; set; }
}

public sealed class UpdateUserAdminRequest
{
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Role { get; set; }
    public string? Status { get; set; }
}

public sealed class BatchGetUsersRequest
{
    public IReadOnlyList<Guid> UserIds { get; set; } = Array.Empty<Guid>();
}

public interface IUserAdminAppService
{
    Task<UserAdminDetailDto?> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserAdminDetailDto>> BatchGetAsync(IReadOnlyList<Guid> userIds, CancellationToken ct = default);
    Task<(UserAdminDetailDto? user, string? error)> CreateAsync(CreateUserAdminRequest request, Guid actorUserId, HttpContext? httpContext, CancellationToken ct = default);
    Task<(UserAdminDetailDto? user, string? error)> UpdateAsync(Guid userId, UpdateUserAdminRequest request, Guid actorUserId, CancellationToken ct = default);
    Task<(bool success, string? error)> SuspendAsync(Guid userId, Guid actorUserId, CancellationToken ct = default);
    Task<(bool success, string? error)> ActivateAsync(Guid userId, Guid actorUserId, CancellationToken ct = default);
    Task<(bool success, string? error)> DisableAsync(Guid userId, Guid actorUserId, CancellationToken ct = default);
    Task<(bool success, string? error)> SoftDeleteAsync(Guid userId, Guid actorUserId, CancellationToken ct = default);
    Task<(bool success, string? error)> UnlockAsync(Guid userId, Guid actorUserId, CancellationToken ct = default);
    Task<(bool success, string? error)> ForcePasswordChangeAsync(Guid userId, Guid actorUserId, CancellationToken ct = default);
}
