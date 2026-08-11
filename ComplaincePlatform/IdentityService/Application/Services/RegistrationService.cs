using IdentityService.Application.Contracts;
using IdentityService.Application.DTOs;
using IdentityService.Configuration;
using IdentityService.Domain;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Development;
using Shared.Infrastructure.Email;
using Shared.Infrastructure.Services;
using Shared.Logging;

namespace IdentityService.Application.Services;

public class RegistrationService : IRegistrationService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IPlatformIdGenerator _platformIdGenerator;
    private readonly IEmailSender _emailSender;
    private readonly IIdentityAuditService _auditService;
    private readonly IPersonServiceClient _personServiceClient;
    private readonly IdentityDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public RegistrationService(
        UserManager<AppUser> userManager,
        IPlatformIdGenerator platformIdGenerator,
        IEmailSender emailSender,
        IIdentityAuditService auditService,
        IPersonServiceClient personServiceClient,
        IdentityDbContext db,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _userManager = userManager;
        _platformIdGenerator = platformIdGenerator;
        _emailSender = emailSender;
        _auditService = auditService;
        _personServiceClient = personServiceClient;
        _db = db;
        _configuration = configuration;
        _environment = environment;
    }

    public Task<(RegisterResultDto? result, AuthOperationResult status)> RegisterSelfAsync(
        string email,
        string password,
        string? role,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var mode = EmailVerificationModeResolver.Resolve(_configuration);
        var emailConfirmed = EmailVerificationModeResolver.ShouldConfirmEmailOnCreate(mode);
        var sendVerification = EmailVerificationModeResolver.ShouldSendVerificationEmail(mode);
        return RegisterCoreAsync(email, password, role, emailConfirmed, sendVerification, mode, httpContext, cancellationToken);
    }

    public Task<(RegisterResultDto? result, AuthOperationResult status)> RegisterInternalAsync(
        string email,
        string password,
        string? role,
        bool emailConfirmed,
        HttpContext? httpContext,
        CancellationToken cancellationToken = default)
    {
        var mode = EmailVerificationModeResolver.Resolve(_configuration);
        // Internal/signup path: honor explicit emailConfirmed; still send verify email in Async when unconfirmed.
        var sendVerification = !emailConfirmed && EmailVerificationModeResolver.ShouldSendVerificationEmail(mode);
        return RegisterCoreAsync(email, password, role, emailConfirmed, sendVerification, mode, httpContext, cancellationToken);
    }

    public async Task<AuthOperationResult> VerifyEmailAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return AuthOperationResult.Fail("VALIDATION_FAILED", "Verification token is required.");

        var hash = TokenHasher.Hash(token);
        var any = await _db.EmailVerificationTokens
            .Where(t => t.TokenHash == hash)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (any == null)
        {
            await _auditService.RecordFailureAsync(EventCode.EmailVerificationFailed, "VerifyEmail", "InvalidToken", cancellationToken: cancellationToken);
            return AuthOperationResult.Fail("UNAUTHORIZED", "Invalid or expired verification token.");
        }

        if (any.UsedAt != null)
        {
            var userAlready = await _userManager.FindByIdAsync(any.UserId.ToString());
            if (userAlready?.EmailConfirmed == true)
            {
                await _auditService.RecordSuccessAsync(EventCode.EmailVerified, "VerifyEmailAlreadyVerified", targetUserId: any.UserId, cancellationToken: cancellationToken);
                return AuthOperationResult.Ok();
            }

            await _auditService.RecordFailureAsync(EventCode.EmailVerificationFailed, "VerifyEmail", "TokenAlreadyUsed", targetUserId: any.UserId, cancellationToken: cancellationToken);
            return AuthOperationResult.Fail("UNAUTHORIZED", "This verification link has already been used.");
        }

        if (any.ExpiresAt <= DateTime.UtcNow)
        {
            await _auditService.RecordFailureAsync(EventCode.EmailVerificationFailed, "VerifyEmail", "TokenExpired", targetUserId: any.UserId, cancellationToken: cancellationToken);
            return AuthOperationResult.Fail("UNAUTHORIZED", "This verification link has expired. Request a new one.");
        }

        var user = await _userManager.FindByIdAsync(any.UserId.ToString());
        if (user == null)
            return AuthOperationResult.Fail("UNAUTHORIZED", "User account not found.");

        if (user.EmailConfirmed)
        {
            any.UsedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);
            await _auditService.RecordSuccessAsync(EventCode.EmailVerified, "VerifyEmailAlreadyVerified", targetUserId: user.Id, cancellationToken: cancellationToken);
            return AuthOperationResult.Ok();
        }

        user.EmailConfirmed = true;
        user.Status = UserAccountStatus.Active;
        user.UpdatedAt = DateTime.UtcNow;
        any.UsedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);
        await _db.SaveChangesAsync(cancellationToken);

        await _auditService.RecordSuccessAsync(EventCode.EmailVerified, "VerifyEmail", targetUserId: user.Id, cancellationToken: cancellationToken);
        return AuthOperationResult.Ok();
    }

    public async Task<(bool success, string message, AuthOperationResult status)> ResendVerificationAsync(
        string email,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        if (string.IsNullOrWhiteSpace(normalized))
            return (false, "Email is required.", AuthOperationResult.Fail("VALIDATION_FAILED", "Email is required."));

        var mode = EmailVerificationModeResolver.Resolve(_configuration);
        if (mode == EmailVerificationMode.None)
            return (true, "Email verification is not required.", AuthOperationResult.Ok());

        var user = await _userManager.FindByEmailAsync(normalized);
        // Anti-enumeration: always return success-shaped message when user missing
        if (user == null || user.IsDeleted)
            return (true, "If an account exists for this email, a verification link has been sent.", AuthOperationResult.Ok());

        if (user.EmailConfirmed)
        {
            await _auditService.RecordSuccessAsync(EventCode.EmailVerified, "ResendVerificationAlreadyVerified", targetUserId: user.Id, cancellationToken: cancellationToken);
            return (true, "Email is already verified.", AuthOperationResult.Ok());
        }

        var cooldown = _configuration.GetValue("Identity:EmailVerification:ResendCooldownoldownSeconds", 60);
        var recent = await _db.EmailVerificationTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (recent != null && recent.CreatedAt.AddSeconds(cooldown) > DateTime.UtcNow)
            return (false, "Please wait before requesting another verification email.", AuthOperationResult.Fail("VALIDATION_FAILED", "Please wait before requesting another verification email."));

        await SendVerificationEmailAsync(user, supersedeExisting: true, cancellationToken);
        await _auditService.RecordSuccessAsync(EventCode.EmailVerificationRequested, "ResendVerification", targetUserId: user.Id, cancellationToken: cancellationToken);
        return (true, "If an account exists for this email, a verification link has been sent.", AuthOperationResult.Ok());
    }

    private async Task<(RegisterResultDto? result, AuthOperationResult status)> RegisterCoreAsync(
        string email,
        string password,
        string? role,
        bool emailConfirmed,
        bool sendVerification,
        EmailVerificationMode mode,
        HttpContext? httpContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (null, AuthOperationResult.Fail("VALIDATION_FAILED", "Email and password are required"));

        email = NormalizeEmail(email)!;

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser != null)
        {
            if (NonProductionEnvironment.AllowDevelopmentSeeding(_environment) && existingUser.IsDeleted)
            {
                await _userManager.DeleteAsync(existingUser);
            }
            else
            {
                return (null, AuthOperationResult.Fail("RESOURCE_CONFLICT", "An account with this email address already exists"));
            }
        }

        var personLink = await _personServiceClient.CreatePersonForRegistrationAsync(email, displayName: null, cancellationToken);
        if (personLink == null)
            return (null, AuthOperationResult.Fail("SERVICE_UNAVAILABLE", "Unable to create person profile. Registration aborted."));

        var activeOnCreate = emailConfirmed || EmailVerificationModeResolver.IsActiveOnCreate(mode);
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = emailConfirmed,
            PlatformId = await _platformIdGenerator.GenerateAsync("USR", cancellationToken),
            PersonId = personLink.PersonId,
            Status = activeOnCreate ? UserAccountStatus.Active : UserAccountStatus.PendingVerification,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            return (null, AuthOperationResult.Fail("VALIDATION_FAILED", "Registration failed.", errors));
        }

        var roleToAssign = string.IsNullOrWhiteSpace(role) ? ApplicationRoles.USER : RoleLegacyMap.ToCanonical(role);
        if (!ApplicationRoles.IsAssignableRole(roleToAssign))
            return (null, AuthOperationResult.Fail("VALIDATION_FAILED", "Invalid role for registration."));

        await _userManager.AddToRoleAsync(user, roleToAssign);

        if (sendVerification)
            await SendVerificationEmailAsync(user, supersedeExisting: false, cancellationToken);

        if (mode != EmailVerificationMode.None)
        {
            var (welcomeSubject, welcomeHtml) = IdentityEmailTemplates.Welcome(user.Email);
            try
            {
                await _emailSender.SendEmailAsync(user.Email!, welcomeSubject, welcomeHtml);
            }
            catch (Exception)
            {
                // Welcome is best-effort; verification is primary.
            }
        }

        await WriteOutboxAsync(user.Id, "UserRegistered", new { user.Id, user.Email, user.PlatformId }, cancellationToken);
        await _auditService.RecordSuccessAsync(EventCode.UserRegistered, "Register", targetUserId: user.Id, cancellationToken: cancellationToken);

        return (new RegisterResultDto
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            PlatformId = user.PlatformId,
            PersonId = user.PersonId,
            Message = sendVerification
                ? "Account created. Please verify your email."
                : "Account created successfully",
            RequiresEmailVerification = sendVerification && !emailConfirmed,
            EmailConfirmed = emailConfirmed,
            EmailVerificationMode = mode.ToString()
        }, AuthOperationResult.Ok());
    }

    private async Task SendVerificationEmailAsync(AppUser user, bool supersedeExisting, CancellationToken cancellationToken)
    {
        if (supersedeExisting)
        {
            var open = await _db.EmailVerificationTokens
                .Where(t => t.UserId == user.Id && t.UsedAt == null)
                .ToListAsync(cancellationToken);
            foreach (var t in open)
                t.UsedAt = DateTime.UtcNow;
        }

        var lifetimeHours = _configuration.GetValue("Identity:EmailVerification:TokenLifetimeHours", 48);
        var token = TokenHasher.GenerateOpaqueToken();
        _db.EmailVerificationTokens.Add(new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = TokenHasher.Hash(token),
            ExpiresAt = DateTime.UtcNow.AddHours(Math.Max(1, lifetimeHours)),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        var baseUrl = _configuration["Identity:PublicBaseUrl"] ?? "http://localhost:4200";
        var link = $"{baseUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(token)}";
        var (subject, html) = IdentityEmailTemplates.Verification(link, user.Email);
        await _emailSender.SendEmailAsync(user.Email!, subject, html);
        await _auditService.RecordSuccessAsync(EventCode.EmailVerificationRequested, "SendVerificationEmail", targetUserId: user.Id, cancellationToken: cancellationToken);
    }

    private async Task WriteOutboxAsync(Guid aggregateId, string type, object payload, CancellationToken cancellationToken)
    {
        _db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            OccurredOn = DateTime.UtcNow,
            Type = type,
            Payload = System.Text.Json.JsonSerializer.Serialize(payload)
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    internal static string? NormalizeEmail(string? email)
        => string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}
