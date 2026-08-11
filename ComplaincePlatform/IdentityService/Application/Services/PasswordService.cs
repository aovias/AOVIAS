using IdentityService.Domain;
using IdentityService.Application.Contracts;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Logging;

namespace IdentityService.Application.Services;

public class PasswordService : IPasswordService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly ISecurityPolicyService _policyService;
    private readonly ISessionService _sessionService;
    private readonly IIdentityAuditService _auditService;
    private readonly IEmailSender _emailSender;
    private readonly IdentityDbContext _db;
    private readonly IConfiguration _configuration;

    public PasswordService(
        UserManager<AppUser> userManager,
        ISecurityPolicyService policyService,
        ISessionService sessionService,
        IIdentityAuditService auditService,
        IEmailSender emailSender,
        IdentityDbContext db,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _policyService = policyService;
        _sessionService = sessionService;
        _auditService = auditService;
        _emailSender = emailSender;
        _db = db;
        _configuration = configuration;
    }

    public async Task<(bool success, string? message)> ForgotPasswordAsync(string email, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email?.Trim().ToLowerInvariant() ?? string.Empty)
            ?? await _userManager.FindByEmailAsync(email ?? string.Empty);
        if (user == null)
            return (true, "If an account exists, a reset link has been sent.");

        var token = TokenHasher.GenerateOpaqueToken();
        _db.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = TokenHasher.Hash(token),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        var baseUrl = _configuration["Identity:PublicBaseUrl"] ?? "http://localhost:4200";
        var link = $"{baseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}&email={Uri.EscapeDataString(user.Email ?? email!)}";
        var (subject, html) = Shared.Infrastructure.Email.IdentityEmailTemplates.PasswordReset(link);
        await _emailSender.SendEmailAsync(user.Email ?? email!, subject, html);

        await _auditService.RecordSuccessAsync(EventCode.PasswordResetRequested, "ForgotPassword", targetUserId: user.Id, cancellationToken: cancellationToken);
        return (true, "If an account exists, a reset link has been sent.");
    }

    public async Task<(bool success, string? message, List<string>? errors)> ResetPasswordAsync(
        string token,
        string newPassword,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var hash = TokenHasher.Hash(token);
        var record = await _db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UsedAt == null && t.ExpiresAt > DateTime.UtcNow, cancellationToken);
        if (record == null)
            return (false, "Invalid or expired reset token.", null);

        var user = await _userManager.FindByIdAsync(record.UserId.ToString());
        if (user == null)
            return (false, "User account not found.", null);

        var change = await ChangePasswordInternalAsync(user, newPassword, cancellationToken);
        if (!change.success)
            return change;

        record.UsedAt = DateTime.UtcNow;
        user.PasswordResetRequired = false;
        user.ForcePasswordChange = false;
        await _userManager.UpdateAsync(user);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.RecordSuccessAsync(EventCode.PasswordChanged, "ResetPassword", targetUserId: user.Id, cancellationToken: cancellationToken);
        return (true, "Password reset successfully.", null);
    }

    public async Task<(bool success, string? message, List<string>? errors)> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return (false, "User account not found.", null);

        if (!await _userManager.CheckPasswordAsync(user, currentPassword))
            return (false, "Current password is incorrect.", null);

        var change = await ChangePasswordInternalAsync(user, newPassword, cancellationToken);
        if (!change.success)
            return change;

        user.ForcePasswordChange = false;
        user.PasswordResetRequired = false;
        await _userManager.UpdateAsync(user);
        await _auditService.RecordSuccessAsync(EventCode.PasswordChanged, "ChangePassword", actorUserId: userId, targetUserId: userId, cancellationToken: cancellationToken);
        return (true, "Password changed successfully.", null);
    }

    public async Task<(bool success, string? message, List<string>? errors)> AdminResetPasswordAsync(
        Guid userId,
        string newPassword,
        Guid actorUserId,
        HttpContext? httpContext,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null || user.IsDeleted)
            return (false, "User account not found.", null);

        var change = await ChangePasswordInternalAsync(user, newPassword, cancellationToken);
        if (!change.success)
            return change;

        user.ForcePasswordChange = true;
        user.PasswordResetRequired = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _auditService.RecordSuccessAsync(
            EventCode.UserPasswordReset,
            "AdminResetPassword",
            actorUserId: actorUserId,
            targetUserId: userId,
            cancellationToken: cancellationToken);

        return (true, "Password reset successfully.", null);
    }

    private async Task<(bool success, string? message, List<string>? errors)> ChangePasswordInternalAsync(
        Domain.AppUser user,
        string newPassword,
        CancellationToken cancellationToken)
    {
        var historyError = await ValidatePasswordHistoryAsync(user, newPassword, cancellationToken);
        if (historyError != null)
            return (false, historyError, null);

        var previousHash = user.PasswordHash;
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        if (!result.Succeeded)
            return (false, "Password does not meet security requirements.", result.Errors.Select(e => e.Description).ToList());

        if (!string.IsNullOrEmpty(previousHash))
        {
            _db.PasswordHistory.Add(new PasswordHistory
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                PasswordHash = previousHash,
                CreatedAt = DateTime.UtcNow
            });
        }
        user.PasswordChangedAt = DateTime.UtcNow;
        user.TokenVersion++;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _userManager.UpdateSecurityStampAsync(user);
        await _sessionService.RevokeAllSessionsAsync(user.Id, cancellationToken);
        return (true, null, null);
    }

    private async Task<string?> ValidatePasswordHistoryAsync(Domain.AppUser user, string newPassword, CancellationToken cancellationToken)
    {
        var policy = await _policyService.GetEffectivePolicyAsync(cancellationToken);
        if (policy.PasswordHistoryCount <= 0)
            return null;

        var history = await _db.PasswordHistory
            .Where(h => h.UserId == user.Id)
            .OrderByDescending(h => h.CreatedAt)
            .Take(policy.PasswordHistoryCount)
            .Select(h => h.PasswordHash)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrEmpty(user.PasswordHash))
            history.Insert(0, user.PasswordHash);

        var hasher = new PasswordHasher<Domain.AppUser>();
        foreach (var oldHash in history)
        {
            if (hasher.VerifyHashedPassword(user, oldHash, newPassword) != PasswordVerificationResult.Failed)
                return "You cannot reuse a recent password.";
        }

        return null;
    }
}
