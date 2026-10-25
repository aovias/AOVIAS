using System.Text.Json;
using IdentityService.Application.Contracts;
using Microsoft.AspNetCore.Http;
using IdentityService.Application.DTOs;
using IdentityService.Domain;
using IdentityService.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Shared.Infrastructure.Auth;
using Shared.Logging;

namespace IdentityService.Application.Users;

public class UserAdminAppService : IUserAdminAppService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly UserManager<AppUser> _userManager;
    private readonly IRegistrationService _registrationService;
    private readonly ISessionService _sessionService;
    private readonly IIdentityAuditService _auditService;

    public UserAdminAppService(
        UserManager<AppUser> userManager,
        IRegistrationService registrationService,
        ISessionService sessionService,
        IIdentityAuditService auditService)
    {
        _userManager = userManager;
        _registrationService = registrationService;
        _sessionService = sessionService;
        _auditService = auditService;
    }

    public async Task<UserAdminDetailDto?> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user == null ? null : await MapAsync(user);
    }

    public async Task<IReadOnlyList<UserAdminDetailDto>> BatchGetAsync(IReadOnlyList<Guid> userIds, CancellationToken ct = default)
    {
        if (userIds.Count == 0) return Array.Empty<UserAdminDetailDto>();
        var results = new List<UserAdminDetailDto>();
        foreach (var id in userIds.Distinct())
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user != null)
                results.Add(await MapAsync(user));
        }
        return results;
    }

    public async Task<(UserAdminDetailDto? user, string? error)> CreateAsync(
        CreateUserAdminRequest request,
        Guid actorUserId,
        HttpContext? httpContext,
        CancellationToken ct = default)
    {
        var email = request.Email.Trim();
        var userName = string.IsNullOrWhiteSpace(request.UserName) ? email : request.UserName.Trim();

        if (await _userManager.FindByEmailAsync(email) != null)
            return (null, "An account with this email already exists.");
        if (await _userManager.FindByNameAsync(userName) != null)
            return (null, "Username is already taken.");

        var (result, status) = await _registrationService.RegisterInternalAsync(
            email, request.TemporaryPassword, request.Role, emailConfirmed: true, httpContext, ct);
        if (!status.Success)
            return (null, status.Message ?? "Registration failed.");

        var user = await _userManager.FindByIdAsync(result!.UserId.ToString());
        if (user == null)
            return (null, "User was created but could not be loaded.");

        if (!string.Equals(user.UserName, userName, StringComparison.Ordinal))
        {
            user.UserName = userName;
            await _userManager.UpdateAsync(user);
        }

        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            user.PhoneNumber = request.PhoneNumber.Trim();
            await _userManager.UpdateAsync(user);
        }

        if (request.PersonId.HasValue && request.PersonId != Guid.Empty)
        {
            user.PersonId = request.PersonId;
            await _userManager.UpdateAsync(user);
        }

        user.ForcePasswordChange = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _auditService.RecordSuccessAsync(
            EventCode.UserRegistered, "AdminCreateUser", actorUserId: actorUserId, targetUserId: user.Id, cancellationToken: ct);

        return (await MapAsync(user), null);
    }

    public async Task<(UserAdminDetailDto? user, string? error)> UpdateAsync(
        Guid userId,
        UpdateUserAdminRequest request,
        Guid actorUserId,
        CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || user.IsDeleted)
            return (null, "User not found.");

        var before = SerializeSnapshot(user);
        var rolesBefore = await _userManager.GetRolesAsync(user);

        if (!string.IsNullOrWhiteSpace(request.Email) && !string.Equals(request.Email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (existing != null && existing.Id != user.Id)
                return (null, "Email is already in use.");
            user.Email = request.Email.Trim();
            user.NormalizedEmail = _userManager.NormalizeEmail(user.Email);
        }

        if (!string.IsNullOrWhiteSpace(request.UserName) && !string.Equals(request.UserName, user.UserName, StringComparison.Ordinal))
        {
            var existing = await _userManager.FindByNameAsync(request.UserName.Trim());
            if (existing != null && existing.Id != user.Id)
                return (null, "Username is already taken.");
            user.UserName = request.UserName.Trim();
            user.NormalizedUserName = _userManager.NormalizeName(user.UserName);
        }

        if (request.PhoneNumber != null)
            user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

        if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<UserAccountStatus>(request.Status, true, out var status))
            user.Status = status;

        user.UpdatedAt = DateTime.UtcNow;
        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return (null, string.Join("; ", updateResult.Errors.Select(e => e.Description)));

        if (!string.IsNullOrWhiteSpace(request.Role))
        {
            var canonical = RoleLegacyMap.ToCanonical(request.Role);
            if (!ApplicationRoles.IsAssignableRole(canonical))
                return (null, "Invalid role.");
            var current = await _userManager.GetRolesAsync(user);
            if (current.Count > 0)
                await _userManager.RemoveFromRolesAsync(user, current);
            await _userManager.AddToRoleAsync(user, canonical);
        }

        var afterUser = await _userManager.FindByIdAsync(userId.ToString());
        var rolesAfter = await _userManager.GetRolesAsync(afterUser!);
        await _auditService.RecordSuccessAsync(
            EventCode.UserUpdated,
            "AdminUpdateUser",
            actorUserId: actorUserId,
            targetUserId: userId,
            oldValue: JsonSerializer.Serialize(new { user = before, roles = rolesBefore }, JsonOptions),
            newValue: JsonSerializer.Serialize(new { user = SerializeSnapshot(afterUser!), roles = rolesAfter }, JsonOptions),
            cancellationToken: ct);

        return (await MapAsync(afterUser!), null);
    }

    public async Task<(bool success, string? error)> SuspendAsync(Guid userId, Guid actorUserId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.");
        var before = user.Status.ToString();
        user.Status = UserAccountStatus.Suspended;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _sessionService.RevokeAllSessionsAsync(userId, ct);
        await _auditService.RecordSuccessAsync(EventCode.UserSuspended, "SuspendUser", actorUserId, userId, before, UserAccountStatus.Suspended.ToString(), cancellationToken: ct);
        return (true, null);
    }

    public async Task<(bool success, string? error)> ActivateAsync(Guid userId, Guid actorUserId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.");
        var before = user.Status.ToString();
        user.Status = UserAccountStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _auditService.RecordSuccessAsync(EventCode.UserActivated, "ActivateUser", actorUserId, userId, before, UserAccountStatus.Active.ToString(), cancellationToken: ct);
        return (true, null);
    }

    public async Task<(bool success, string? error)> DisableAsync(Guid userId, Guid actorUserId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.");
        var before = user.Status.ToString();
        user.Status = UserAccountStatus.Disabled;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _sessionService.RevokeAllSessionsAsync(userId, ct);
        await _auditService.RecordSuccessAsync(EventCode.UserDisabled, "DisableUser", actorUserId, userId, before, UserAccountStatus.Disabled.ToString(), cancellationToken: ct);
        return (true, null);
    }

    public async Task<(bool success, string? error)> SoftDeleteAsync(Guid userId, Guid actorUserId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.");
        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;
        user.Status = UserAccountStatus.Deleted;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _sessionService.RevokeAllSessionsAsync(userId, ct);
        await _auditService.RecordSuccessAsync(EventCode.UserDeleted, "SoftDeleteUser", actorUserId, userId, cancellationToken: ct);
        return (true, null);
    }

    public async Task<(bool success, string? error)> UnlockAsync(Guid userId, Guid actorUserId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.");
        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _auditService.RecordSuccessAsync(EventCode.UserUnlocked, "UnlockUser", actorUserId, userId, cancellationToken: ct);
        return (true, null);
    }

    public async Task<(bool success, string? error)> ForcePasswordChangeAsync(Guid userId, Guid actorUserId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return (false, "User not found.");
        user.ForcePasswordChange = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _auditService.RecordSuccessAsync(EventCode.UserForcePasswordChange, "ForcePasswordChange", actorUserId, userId, cancellationToken: ct);
        return (true, null);
    }

    private async Task<UserAdminDetailDto> MapAsync(AppUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserAdminDetailDto
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            UserName = user.UserName,
            PhoneNumber = user.PhoneNumber,
            PlatformId = user.PlatformId,
            PersonId = user.PersonId,
            Status = user.Status.ToString(),
            IsDeleted = user.IsDeleted,
            ForcePasswordChange = user.ForcePasswordChange,
            PasswordResetRequired = user.PasswordResetRequired,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd,
            LastLoginAt = user.LastLoginAt,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        };
    }

    private static object SerializeSnapshot(AppUser user) => new
    {
        user.Email,
        user.UserName,
        user.PhoneNumber,
        Status = user.Status.ToString(),
        user.IsDeleted,
        user.ForcePasswordChange
    };
}
