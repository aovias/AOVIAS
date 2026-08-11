using IdentityService.Application.Configuration;
using IdentityService.Application.Contracts;
using IdentityService.Application.Contracts.Security;
using IdentityService.Application.DTOs;
using IdentityService.Application.Services;
using IdentityService.Configuration;
using IdentityService.Domain;
using IdentityService.Domain.Entities;
using IdentityService.Domain.Enums;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Configuration;
using IdentityService.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Email;
using Shared.Logging;

namespace IdentityService.Application.Services;

public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ISessionService _sessionService;
    private readonly IOrganizationServiceClient _organizationClient;
    private readonly IWorkforceContextClient _workforceContext;
    private readonly IProfessionalContextClient _professionalContext;
    private readonly IMfaService _mfaService;
    private readonly IRevokedAccessTokenService _revokedTokens;
    private readonly IIdentityAuditService _auditService;
    private readonly IdentityDbContext _db;
    private readonly IAdaptiveAuthenticationService _adaptiveAuth;
    private readonly IThreatDetectionService _threatDetection;
    private readonly ITrustedDeviceService _trustedDevices;
    private readonly IRiskScoringService _riskScoring;
    private readonly ISecurityEventService _securityEvents;
    private readonly IConfiguration _configuration;
    private readonly bool _relaxLoginSecurityGates;

    public AuthenticationService(
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager,
        ITokenService tokenService,
        ISessionService sessionService,
        IOrganizationServiceClient organizationClient,
        IWorkforceContextClient workforceContext,
        IProfessionalContextClient professionalContext,
        IMfaService mfaService,
        IRevokedAccessTokenService revokedTokens,
        IIdentityAuditService auditService,
        IdentityDbContext db,
        IAdaptiveAuthenticationService adaptiveAuth,
        IThreatDetectionService threatDetection,
        ITrustedDeviceService trustedDevices,
        IRiskScoringService riskScoring,
        ISecurityEventService securityEvents,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _sessionService = sessionService;
        _organizationClient = organizationClient;
        _workforceContext = workforceContext;
        _professionalContext = professionalContext;
        _mfaService = mfaService;
        _revokedTokens = revokedTokens;
        _auditService = auditService;
        _db = db;
        _adaptiveAuth = adaptiveAuth;
        _threatDetection = threatDetection;
        _trustedDevices = trustedDevices;
        _riskScoring = riskScoring;
        _securityEvents = securityEvents;
        _configuration = configuration;
        _relaxLoginSecurityGates = configuration
            .GetSection(IdentityRuntimeOptions.SectionName)
            .GetValue(nameof(IdentityRuntimeOptions.RelaxLoginSecurityGates), false);
    }

    public async Task<(LoginResultDto? result, AuthOperationResult status)> LoginAsync(
        string email,
        string password,
        string? deviceId,
        HttpContext httpContext,
        bool trustDevice = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return (null, AuthOperationResult.Fail("VALIDATION_FAILED", "Email and password are required"));

        var user = await _userManager.FindByEmailAsync(email.Trim().ToLowerInvariant())
            ?? await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            await RecordLoginAttemptAsync(null, email, false, "UserNotFound", deviceId, httpContext, cancellationToken);
            await _auditService.RecordFailureAsync(EventCode.AuthFailed, "Login", "UserNotFound", targetUserId: null, cancellationToken: cancellationToken);
            return (null, AuthOperationResult.Fail("UNAUTHORIZED", "Invalid email or password"));
        }

        var mode = EmailVerificationModeResolver.Resolve(_configuration);
        var statusCheck = ValidateAccountStatus(user, mode);
        if (statusCheck != null && !(_relaxLoginSecurityGates && statusCheck == "EmailNotVerified"))
        {
            await RecordLoginAttemptAsync(user.Id, email, false, statusCheck, deviceId, httpContext, cancellationToken);
            await _auditService.RecordFailureAsync(EventCode.AuthFailed, "Login", statusCheck, targetUserId: user.Id, cancellationToken: cancellationToken);
            return (null, AuthOperationResult.Fail("FORBIDDEN", GetStatusMessage(user, statusCheck)));
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, password, lockoutOnFailure: true);
        if (signInResult.IsLockedOut)
        {
            await RecordLoginAttemptAsync(user.Id, email, false, "AccountLocked", deviceId, httpContext, cancellationToken);
            await _auditService.RecordFailureAsync(EventCode.AuthFailed, "Login", "AccountLocked", targetUserId: user.Id, cancellationToken: cancellationToken);
            return (null, AuthOperationResult.Fail("FORBIDDEN", "Your account has been temporarily locked due to multiple failed login attempts. Please try again later."));
        }

        if (!signInResult.Succeeded)
        {
            user.LastFailedLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await RecordLoginAttemptAsync(user.Id, email, false, "InvalidPassword", deviceId, httpContext, cancellationToken);
            await _auditService.RecordFailureAsync(EventCode.AuthFailed, "Login", "InvalidPassword", targetUserId: user.Id, cancellationToken: cancellationToken);
            return (null, AuthOperationResult.Fail("UNAUTHORIZED", "Invalid email or password"));
        }

        if (_relaxLoginSecurityGates && user.Status == UserAccountStatus.PendingVerification)
        {
            user.Status = UserAccountStatus.Active;
            user.EmailConfirmed = true;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }

        if (user.ForcePasswordChange || user.PasswordResetRequired)
        {
            return (null, AuthOperationResult.Fail("FORBIDDEN", "Password change is required before you can sign in."));
        }

        if (!_relaxLoginSecurityGates)
        {
            var policyGate = await ValidateSecurityPolicyGatesAsync(user, cancellationToken);
            if (policyGate != null)
                return (null, policyGate);
        }

        var ip = ClientContextParser.GetIpAddress(httpContext);
        var ua = ClientContextParser.GetUserAgent(httpContext);
        var geo = ClientContextParser.ResolveGeoLocation(ip);
        var adaptive = _relaxLoginSecurityGates
            ? AdaptiveAuthDecision.Allow
            : await _adaptiveAuth.EvaluateLoginAsync(new AdaptiveAuthContext
            {
                UserId = user.Id,
                DeviceId = deviceId,
                IPAddress = ip,
                UserAgent = ua,
                GeoLocation = geo
            }, cancellationToken);

        if (adaptive == AdaptiveAuthDecision.Block)
        {
            await RecordLoginAttemptAsync(user.Id, email, false, "RiskBlocked", deviceId, httpContext, cancellationToken);
            await _securityEvents.RecordAsync("LoginBlocked", SecurityEventSeverity.High, "Login blocked by adaptive authentication", user.Id, ipAddress: ip, ct: cancellationToken);
            return (null, AuthOperationResult.Fail("FORBIDDEN", "Sign-in blocked due to security risk. Contact your administrator."));
        }

        var mfaEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        var requireMfa = mfaEnabled || adaptive == AdaptiveAuthDecision.StepUpMfa;

        if (requireMfa)
        {
            if (!mfaEnabled)
            {
                if (_relaxLoginSecurityGates)
                    return await IssueLoginTokensAsync(user, deviceId, httpContext, AuthenticationMethod.Password, trustDevice, cancellationToken);

                return (null, AuthOperationResult.Fail("FORBIDDEN", "Multi-factor authentication is required for this sign-in. Please enable MFA on your account."));
            }

            var challenge = await _mfaService.CreateLoginChallengeAsync(user.Id, cancellationToken);
            return (new LoginResultDto
            {
                RequiresMfa = true,
                MfaChallengeToken = challenge,
                UserId = user.Id,
                PlatformId = user.PlatformId,
                Email = user.Email ?? string.Empty
            }, AuthOperationResult.Ok());
        }

        return await IssueLoginTokensAsync(user, deviceId, httpContext, AuthenticationMethod.Password, trustDevice, cancellationToken);
    }

    public async Task<(LoginResultDto? result, AuthOperationResult status)> CompleteMfaLoginAsync(
        string mfaChallengeToken,
        string code,
        string? deviceId,
        HttpContext httpContext,
        bool trustDevice = false,
        CancellationToken cancellationToken = default)
    {
        var userId = await _mfaService.ValidateLoginChallengeAsync(mfaChallengeToken, cancellationToken);
        if (userId == null)
            return (null, AuthOperationResult.Fail("UNAUTHORIZED", "Invalid or expired MFA challenge."));

        var user = await _userManager.FindByIdAsync(userId.Value.ToString());
        if (user == null)
            return (null, AuthOperationResult.Fail("UNAUTHORIZED", "User account not found."));

        var mode = EmailVerificationModeResolver.Resolve(_configuration);
        var statusCheck = ValidateAccountStatus(user, mode);
        if (statusCheck != null)
            return (null, AuthOperationResult.Fail("FORBIDDEN", GetStatusMessage(user, statusCheck)));

        var verify = await _mfaService.VerifyLoginCodeAsync(user.Id, code, cancellationToken);
        if (!verify.success)
            return (null, AuthOperationResult.Fail("UNAUTHORIZED", verify.message ?? "Invalid MFA code."));

        return await IssueLoginTokensAsync(user, deviceId, httpContext, AuthenticationMethod.MfaTotp, trustDevice, cancellationToken);
    }

    public async Task<(RefreshResultDto? result, AuthOperationResult status)> RefreshAsync(
        string refreshToken,
        HttpContext httpContext,
        Guid? hospitalId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return (null, AuthOperationResult.Fail("VALIDATION_FAILED", "Refresh token is required"));

        var rotationResult = await _sessionService.RotateRefreshTokenAsync(refreshToken, cancellationToken);
        if (rotationResult == null)
            return (null, AuthOperationResult.Fail("UNAUTHORIZED", "Invalid or expired refresh token. Please log in again."));

        var (rotation, userId, sessionId) = rotationResult.Value;
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            return (null, AuthOperationResult.Fail("UNAUTHORIZED", "User account not found. Please log in again."));

        var mode = EmailVerificationModeResolver.Resolve(_configuration);
        var statusCheck = ValidateAccountStatus(user, mode);
        if (statusCheck != null)
            return (null, AuthOperationResult.Fail("FORBIDDEN", GetStatusMessage(user, statusCheck)));

        var tokenPair = await BuildTokenPairAsync(
            user, rotation.RefreshToken, AuthenticationMethod.RefreshToken, cancellationToken, sessionId, hospitalId);
        var session = await _sessionService.GetSessionAsync(sessionId, cancellationToken);
        if (session != null)
        {
            session.AccessTokenJti = tokenPair.AccessTokenJti;
            await _db.SaveChangesAsync(cancellationToken);
        }

        await _auditService.RecordSuccessAsync(EventCode.TokenRefreshed, "RefreshToken", targetUserId: user.Id,
            sessionId: sessionId, cancellationToken: cancellationToken);

        return (new RefreshResultDto
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            ExpiresIn = tokenPair.ExpiresIn,
            UserId = user.Id,
            SessionId = sessionId
        }, AuthOperationResult.Ok());
    }

    public async Task<AuthOperationResult> LogoutAsync(
        Guid userId,
        Guid? sessionId,
        string? refreshToken,
        string? accessTokenJti,
        DateTime? accessTokenExpiry,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
            await _sessionService.RevokeSessionByRefreshTokenAsync(refreshToken, cancellationToken);
        else if (sessionId.HasValue)
            await _sessionService.RevokeSessionAsync(sessionId.Value, cancellationToken);

        if (!string.IsNullOrWhiteSpace(accessTokenJti) && accessTokenExpiry.HasValue)
            await _revokedTokens.RevokeAsync(accessTokenJti, userId, accessTokenExpiry.Value, cancellationToken);

        await _auditService.RecordSuccessAsync(EventCode.UserLogout, "Logout", actorUserId: userId, targetUserId: userId,
            sessionId: sessionId, cancellationToken: cancellationToken);
        return AuthOperationResult.Ok();
    }

    public async Task<AuthOperationResult> LogoutAllAsync(Guid userId, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        await _sessionService.RevokeAllSessionsAsync(userId, cancellationToken);
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user != null)
        {
            user.TokenVersion++;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);
        }

        await _auditService.RecordSuccessAsync(EventCode.UserLogout, "LogoutAll", actorUserId: userId, targetUserId: userId,
            cancellationToken: cancellationToken);
        return AuthOperationResult.Ok();
    }

    private async Task<(LoginResultDto? result, AuthOperationResult status)> IssueLoginTokensAsync(
        AppUser user,
        string? deviceId,
        HttpContext httpContext,
        AuthenticationMethod authMethod,
        bool trustDevice,
        CancellationToken cancellationToken)
    {
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var ip = ClientContextParser.GetIpAddress(httpContext);
        var ua = ClientContextParser.GetUserAgent(httpContext);
        var geo = ClientContextParser.ResolveGeoLocation(ip);
        var (session, refreshToken) = await _sessionService.CreateSessionAsync(user.Id, deviceId, ip, ua, cancellationToken);

        var risk = await _riskScoring.CalculateAndPersistAsync(user.Id, cancellationToken);
        session.RiskScore = risk.Score;
        if (Enum.TryParse<RiskLevel>(risk.Level, out var level) && level >= RiskLevel.High)
            session.IsSuspicious = true;

        var tokenPair = await BuildTokenPairAsync(user, refreshToken, authMethod, cancellationToken, session.SessionId);

        session.AccessTokenJti = tokenPair.AccessTokenJti;
        await _db.SaveChangesAsync(cancellationToken);

        await _trustedDevices.RegisterOrUpdateAsync(user.Id, deviceId ?? string.Empty, ua, ip, trustDevice, cancellationToken);
        await _threatDetection.EvaluateLoginAsync(user.Id, ip, geo, session.SessionId, cancellationToken);
        await _securityEvents.RecordAsync("UserLogin", SecurityEventSeverity.Info, "Successful login", user.Id, session.SessionId, deviceId, ip, ct: cancellationToken);

        var roles = RoleLegacyMap.NormalizeRoleNames(await _userManager.GetRolesAsync(user));
        var authContext = await _organizationClient.GetUserAuthContextAsync(user.Id, cancellationToken: cancellationToken);

        await RecordLoginAttemptAsync(user.Id, user.Email, true, null, deviceId, httpContext, cancellationToken, session.SessionId);
        await _auditService.RecordSuccessAsync(EventCode.UserLogin, "Login", targetUserId: user.Id, sessionId: session.SessionId, cancellationToken: cancellationToken);
        await _auditService.RecordSuccessAsync(EventCode.TokenIssued, "IssueAccessToken", targetUserId: user.Id, sessionId: session.SessionId, cancellationToken: cancellationToken);

        return (new LoginResultDto
        {
            RequiresMfa = false,
            Tokens = tokenPair,
            UserId = user.Id,
            PlatformId = user.PlatformId,
            Email = user.Email ?? string.Empty,
            Role = roles.FirstOrDefault() ?? ApplicationRoles.USER,
            Roles = roles.ToList(),
            OrganizationId = authContext.OrganizationId,
            HospitalId = authContext.HospitalId,
            SessionId = session.SessionId
        }, AuthOperationResult.Ok());
    }

    private async Task<TokenPairResult> BuildTokenPairAsync(
        AppUser user,
        string refreshToken,
        AuthenticationMethod authMethod,
        CancellationToken cancellationToken,
        Guid? sessionId = null,
        Guid? hospitalId = null)
    {
        UserSession? session;
        if (sessionId.HasValue)
            session = await _sessionService.GetSessionAsync(sessionId.Value, cancellationToken);
        else
        {
            var hash = TokenHasher.Hash(refreshToken);
            session = await _db.UserSessions.FirstOrDefaultAsync(s => s.RefreshTokenHash == hash, cancellationToken);
        }

        if (session == null)
            throw new InvalidOperationException("Session not found for token issuance.");

        var roles = await _userManager.GetRolesAsync(user);
        var authContext = await _organizationClient.GetUserAuthContextAsync(user.Id, hospitalId, cancellationToken);
        var stamp = await _userManager.GetSecurityStampAsync(user) ?? string.Empty;

        Guid? employmentId = null;
        Guid? professionalProfileId = null;
        if (user.PersonId.HasValue)
        {
            employmentId = await _workforceContext.GetPrimaryEmploymentIdAsync(user.PersonId.Value, cancellationToken);
            professionalProfileId = await _professionalContext.GetProfessionalProfileIdAsync(user.PersonId.Value, cancellationToken);
        }

        var context = new TokenClaimsContext
        {
            UserId = user.Id,
            Email = user.Email ?? string.Empty,
            PlatformId = user.PlatformId,
            PersonId = user.PersonId,
            EmploymentId = employmentId,
            ProfessionalProfileId = professionalProfileId,
            UserName = user.UserName ?? user.Email ?? string.Empty,
            Roles = roles.ToList(),
            AuthContext = authContext,
            SessionId = session.SessionId,
            TokenVersion = user.TokenVersion,
            SecurityStamp = stamp,
            AuthenticationMethod = authMethod,
            Scopes = new List<string>
            {
                AuthConstants.Scopes.ComplianceApi,
                AuthConstants.Scopes.OrganizationApi,
                AuthConstants.Scopes.AiApi
            }
        };

        return await _tokenService.GenerateTokenPairAsync(context, refreshToken, cancellationToken);
    }

    private string? ValidateAccountStatus(AppUser user, EmailVerificationMode mode)
    {
        if (user.IsDeleted || user.Status == UserAccountStatus.Deleted)
            return "AccountDeleted";
        if (user.Status == UserAccountStatus.Suspended)
            return "AccountSuspended";
        if (user.Status == UserAccountStatus.Disabled)
            return "AccountDisabled";
        if (user.Status == UserAccountStatus.Archived)
            return "AccountArchived";

        // Mode-driven email verification gate (Phase 0/1).
        if (!_relaxLoginSecurityGates
            && EmailVerificationModeResolver.BlocksPasswordLoginWhenUnconfirmed(mode)
            && !user.EmailConfirmed)
        {
            return "EmailNotVerified";
        }

        // PendingVerification only blocks when mode requires confirmation before login.
        if (user.Status == UserAccountStatus.PendingVerification
            && EmailVerificationModeResolver.BlocksPasswordLoginWhenUnconfirmed(mode)
            && !_relaxLoginSecurityGates)
        {
            return "EmailNotVerified";
        }

        return null;
    }

    private async Task<AuthOperationResult?> ValidateSecurityPolicyGatesAsync(AppUser user, CancellationToken cancellationToken)
    {
        var policy = IdentitySecurityPolicyState.Current;

        if (policy.PasswordMaxAgeDays.HasValue)
        {
            var changedAt = user.PasswordChangedAt ?? user.CreatedAt;
            if (changedAt != default && changedAt.AddDays(policy.PasswordMaxAgeDays.Value) <= DateTime.UtcNow)
            {
                if (!user.ForcePasswordChange)
                {
                    user.ForcePasswordChange = true;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                }
                return AuthOperationResult.Fail("FORBIDDEN", "Password change is required before you can sign in.");
            }
        }

        if (policy.RequireMfaForAdminRoles)
        {
            var roles = RoleLegacyMap.NormalizeRoleNames(await _userManager.GetRolesAsync(user));
            var adminRoles = new HashSet<string>(StringComparer.Ordinal)
            {
                ApplicationRoles.SUPER_ADMIN,
                ApplicationRoles.ORG_ADMIN,
                ApplicationRoles.GROUP_ADMIN,
                ApplicationRoles.REGULATORY_ADMIN
            };

            if (roles.Any(adminRoles.Contains) && !await _userManager.GetTwoFactorEnabledAsync(user))
            {
                return AuthOperationResult.Fail("FORBIDDEN", "Multi-factor authentication is required for administrator accounts.");
            }
        }

        return null;
    }

    private static string GetStatusMessage(AppUser user, string reason) => reason switch
    {
        "AccountLocked" => "Your account has been temporarily locked due to multiple failed login attempts. Please try again later.",
        "AccountSuspended" => "Your account has been suspended. Contact your administrator.",
        "AccountDisabled" => "Your account has been disabled.",
        "AccountArchived" => "Your account has been archived.",
        "EmailNotVerified" => "Please verify your email address before signing in.",
        "AccountDeleted" => "This account is no longer available.",
        _ => "Sign-in is not allowed for this account."
    };

    private async Task RecordLoginAttemptAsync(
        Guid? userId,
        string? email,
        bool success,
        string? failureReason,
        string? deviceId,
        HttpContext httpContext,
        CancellationToken cancellationToken,
        Guid? sessionId = null)
    {
        var ua = ClientContextParser.GetUserAgent(httpContext);
        _db.LoginAttempts.Add(new LoginAttempt
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Email = email,
            Success = success,
            FailureReason = failureReason,
            IPAddress = ClientContextParser.GetIpAddress(httpContext),
            UserAgent = ua,
            OperatingSystem = ClientContextParser.ParseOperatingSystem(ua),
            DeviceId = deviceId,
            CorrelationId = ClientContextParser.GetCorrelationId(httpContext),
            SessionId = sessionId,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
