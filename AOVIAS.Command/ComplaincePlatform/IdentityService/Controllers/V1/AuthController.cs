using IdentityService.Application.Contracts;
using IdentityService.Application.Services;
using IdentityService.Application.DTOs;
using IdentityService.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Shared.Infrastructure.Api;
using System.Text.RegularExpressions;

namespace IdentityService.Controllers.V1;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IRegistrationService _registrationService;
    private readonly IPasswordService _passwordService;
    private readonly IMfaService _mfaService;
    private readonly ISessionService _sessionService;
    private readonly ITokenService _tokenService;

    public AuthController(
        IAuthenticationService authenticationService,
        IRegistrationService registrationService,
        IPasswordService passwordService,
        IMfaService mfaService,
        ISessionService sessionService,
        ITokenService tokenService)
    {
        _authenticationService = authenticationService;
        _registrationService = registrationService;
        _passwordService = passwordService;
        _mfaService = mfaService;
        _sessionService = sessionService;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        if (!ValidateEmail(request.Email))
            return AuthApiResponses.Failure<RegisterResponse>(ApiResponseCode.VALIDATION_FAILED, "Invalid email format", requestId);

        var (result, status) = await _registrationService.RegisterSelfAsync(request.Email, request.Password, request.Role, HttpContext, cancellationToken);
        if (!status.Success)
            return MapFailure<RegisterResponse>(status, requestId);

        return Ok(AuthApiResponses.Success(new RegisterResponse
        {
            UserId = result!.UserId,
            Email = result.Email,
            PlatformId = result.PlatformId,
            Message = result.Message,
            RequiresEmailVerification = result.RequiresEmailVerification,
            EmailConfirmed = result.EmailConfirmed,
            EmailVerificationMode = result.EmailVerificationMode
        }, requestId));
    }

    [HttpPost("resend-verification")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> ResendVerification([FromBody] ResendVerificationRequest request, CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var (success, message, status) = await _registrationService.ResendVerificationAsync(request.Email, HttpContext, cancellationToken);
        if (!success)
            return MapFailure<object>(status, requestId);

        return Ok(AuthApiResponses.Success(new { message }, requestId));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var (result, status) = await _authenticationService.LoginAsync(request.Email, request.Password, request.DeviceId, HttpContext, request.TrustDevice, cancellationToken);
        if (!status.Success)
            return MapFailure<LoginResponse>(status, requestId);

        if (result!.RequiresMfa)
        {
            return Ok(AuthApiResponses.Success(new LoginResponse
            {
                RequiresMfa = true,
                MfaChallengeToken = result.MfaChallengeToken,
                UserId = result.UserId,
                PlatformId = result.PlatformId,
                Email = result.Email
            }, requestId));
        }

        return Ok(AuthApiResponses.Success(MapLoginResponse(result), requestId));
    }

    [HttpPost("login/mfa")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> LoginMfa([FromBody] MfaLoginRequest request, CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var (result, status) = await _authenticationService.CompleteMfaLoginAsync(
            request.MfaChallengeToken, request.Code, request.DeviceId, HttpContext, request.TrustDevice, cancellationToken);
        if (!status.Success)
            return MapFailure<LoginResponse>(status, requestId);

        return Ok(AuthApiResponses.Success(MapLoginResponse(result!), requestId));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var (result, status) = await _authenticationService.RefreshAsync(
            request.RefreshToken, HttpContext, request.HospitalId, cancellationToken);
        if (!status.Success)
            return MapFailure<RefreshTokenResponse>(status, requestId);

        return Ok(AuthApiResponses.Success(new RefreshTokenResponse
        {
            Token = result!.AccessToken,
            RefreshToken = result.RefreshToken,
            ExpiresIn = result.ExpiresIn
        }, requestId));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest? request, CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var userId = GetUserId();
        if (userId == null)
            return AuthApiResponses.Failure<LogoutResponse>(ApiResponseCode.UNAUTHORIZED, "Unauthorized", requestId);

        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        string? jti = null;
        DateTime? expiry = null;
        if (!string.IsNullOrWhiteSpace(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            jti = _tokenService.ExtractJti(token);
            expiry = _tokenService.ExtractExpiry(token);
        }

        Guid? sessionId = null;
        if (Guid.TryParse(User.FindFirst(Shared.Infrastructure.Auth.AuthConstants.Claims.SessionId)?.Value, out var sid))
            sessionId = sid;

        var status = await _authenticationService.LogoutAsync(userId.Value, sessionId, request?.RefreshToken, jti, expiry, HttpContext, cancellationToken);
        if (!status.Success)
            return MapFailure<LogoutResponse>(status, requestId);

        return Ok(AuthApiResponses.Success(new LogoutResponse { Message = "Logged out successfully" }, requestId));
    }

    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var userId = GetUserId();
        if (userId == null)
            return AuthApiResponses.Failure<LogoutResponse>(ApiResponseCode.UNAUTHORIZED, "Unauthorized", requestId);

        var status = await _authenticationService.LogoutAllAsync(userId.Value, HttpContext, cancellationToken);
        if (!status.Success)
            return MapFailure<LogoutResponse>(status, requestId);

        return Ok(AuthApiResponses.Success(new LogoutResponse { Message = "All sessions revoked" }, requestId));
    }

    [HttpPost("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request, CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var status = await _registrationService.VerifyEmailAsync(request.Token, cancellationToken);
        if (!status.Success)
            return MapFailure<object>(status, requestId);

        return Ok(AuthApiResponses.Success(new { message = "Email verified successfully" }, requestId));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var (success, message) = await _passwordService.ForgotPasswordAsync(request.Email, HttpContext, cancellationToken);
        return Ok(AuthApiResponses.Success(new { message }, requestId));
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var (success, message, errors) = await _passwordService.ResetPasswordAsync(request.Token, request.NewPassword, HttpContext, cancellationToken);
        if (!success)
            return AuthApiResponses.Failure<object>(ApiResponseCode.VALIDATION_FAILED, message ?? "Reset failed", requestId, errors);

        return Ok(AuthApiResponses.Success(new { message }, requestId));
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var userId = GetUserId();
        if (userId == null)
            return AuthApiResponses.Failure<object>(ApiResponseCode.UNAUTHORIZED, "Unauthorized", requestId);

        var (success, message, errors) = await _passwordService.ChangePasswordAsync(userId.Value, request.CurrentPassword, request.NewPassword, HttpContext, cancellationToken);
        if (!success)
            return AuthApiResponses.Failure<object>(ApiResponseCode.VALIDATION_FAILED, message ?? "Change failed", requestId, errors);

        return Ok(AuthApiResponses.Success(new { message = "Password changed successfully" }, requestId));
    }

    [HttpPost("mfa/setup")]
    [Authorize]
    public async Task<IActionResult> MfaSetup(CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var userId = GetUserId();
        if (userId == null)
            return AuthApiResponses.Failure<object>(ApiResponseCode.UNAUTHORIZED, "Unauthorized", requestId);

        var (success, qrUri, sharedKey, message) = await _mfaService.SetupAsync(userId.Value, cancellationToken);
        if (!success)
            return AuthApiResponses.Failure<object>(ApiResponseCode.VALIDATION_FAILED, message ?? "Setup failed", requestId);

        return Ok(AuthApiResponses.Success(new { qrUri, sharedKey }, requestId));
    }

    [HttpPost("mfa/verify")]
    [Authorize]
    public async Task<IActionResult> MfaVerify([FromBody] MfaVerifyRequest request, CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var userId = GetUserId();
        if (userId == null)
            return AuthApiResponses.Failure<object>(ApiResponseCode.UNAUTHORIZED, "Unauthorized", requestId);

        var (success, message) = await _mfaService.VerifySetupAsync(userId.Value, request.Code, cancellationToken);
        if (!success)
            return AuthApiResponses.Failure<object>(ApiResponseCode.VALIDATION_FAILED, message ?? "Verification failed", requestId);

        return Ok(AuthApiResponses.Success(new { message = "MFA enabled" }, requestId));
    }

    [HttpPost("mfa/disable")]
    [Authorize]
    public async Task<IActionResult> MfaDisable([FromBody] MfaDisableRequest request, CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var userId = GetUserId();
        if (userId == null)
            return AuthApiResponses.Failure<object>(ApiResponseCode.UNAUTHORIZED, "Unauthorized", requestId);

        var (success, message) = await _mfaService.DisableAsync(userId.Value, request.Password, cancellationToken);
        if (!success)
            return AuthApiResponses.Failure<object>(ApiResponseCode.VALIDATION_FAILED, message ?? "Disable failed", requestId);

        return Ok(AuthApiResponses.Success(new { message = "MFA disabled" }, requestId));
    }

    [HttpGet("mfa/recovery-codes")]
    [Authorize]
    public async Task<IActionResult> MfaRecoveryCodes(CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var userId = GetUserId();
        if (userId == null)
            return AuthApiResponses.Failure<object>(ApiResponseCode.UNAUTHORIZED, "Unauthorized", requestId);

        var codes = await _mfaService.GetRecoveryCodesAsync(userId.Value, cancellationToken);
        return Ok(AuthApiResponses.Success(new { codes }, requestId));
    }

    [HttpGet("sessions")]
    [Authorize]
    public async Task<IActionResult> ListSessions(CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var userId = GetUserId();
        if (userId == null)
            return AuthApiResponses.Failure<object>(ApiResponseCode.UNAUTHORIZED, "Unauthorized", requestId);

        Guid? sessionId = null;
        if (Guid.TryParse(User.FindFirst(Shared.Infrastructure.Auth.AuthConstants.Claims.SessionId)?.Value, out var sid))
            sessionId = sid;

        var sessions = await _sessionService.ListSessionsAsync(userId.Value, sessionId, cancellationToken);
        return Ok(AuthApiResponses.Success(sessions, requestId));
    }

    [HttpDelete("sessions/{sessionId:guid}")]
    [Authorize]
    public async Task<IActionResult> RevokeSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var requestId = GetRequestId();
        var userId = GetUserId();
        if (userId == null)
            return AuthApiResponses.Failure<object>(ApiResponseCode.UNAUTHORIZED, "Unauthorized", requestId);

        var session = await _sessionService.GetSessionAsync(sessionId, cancellationToken);
        if (session == null || session.UserId != userId.Value)
            return AuthApiResponses.Failure<object>(ApiResponseCode.RESOURCE_NOT_FOUND, "Session not found", requestId);

        await _sessionService.RevokeSessionAsync(sessionId, cancellationToken);
        return Ok(AuthApiResponses.Success(new { message = "Session revoked" }, requestId));
    }

    private static LoginResponse MapLoginResponse(LoginResultDto result)
    {
        var tokens = result.Tokens!;
        return new LoginResponse
        {
            Token = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            ExpiresIn = tokens.ExpiresIn,
            UserId = result.UserId,
            PlatformId = result.PlatformId,
            Email = result.Email,
            Role = result.Role,
            Roles = result.Roles,
            OrganizationId = result.OrganizationId,
            HospitalId = result.HospitalId,
            SessionId = result.SessionId,
            RequiresMfa = false
        };
    }

    private Guid? GetUserId()
    {
        var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(id, out var guid) ? guid : null;
    }

    private string GetRequestId() =>
        Request.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? Request.Headers["X-Correlation-ID"].FirstOrDefault()
        ?? HttpContext.TraceIdentifier;

    private static IActionResult MapFailure<T>(AuthOperationResult status, string requestId)
    {
        var code = Enum.TryParse<ApiResponseCode>(status.ErrorCode, out var parsed)
            ? parsed
            : ApiResponseCode.INTERNAL_ERROR;
        return AuthApiResponses.Failure<T>(code, status.Message ?? "Operation failed", requestId, status.Details);
    }

    private static bool ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
    }
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public bool TrustDevice { get; set; }
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public Guid UserId { get; set; }
    public string PlatformId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public Guid OrganizationId { get; set; }
    public Guid HospitalId { get; set; }
    public Guid SessionId { get; set; }
    public bool RequiresMfa { get; set; }
    public string? MfaChallengeToken { get; set; }
}

public class RegisterResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PlatformId { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool RequiresEmailVerification { get; set; }
    public bool EmailConfirmed { get; set; }
    public string EmailVerificationMode { get; set; } = "Async";
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    /// <summary>
    /// Optional hospital to embed in the new access token. Used when switching hospital context
    /// so JWT <c>hospital_id</c> stays aligned with <c>X-Hospital-Id</c> / query params.
    /// </summary>
    public Guid? HospitalId { get; set; }
}
public class RefreshTokenResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
}
public class LogoutRequest { public string? RefreshToken { get; set; } }
public class LogoutResponse { public string Message { get; set; } = string.Empty; }
public class VerifyEmailRequest { public string Token { get; set; } = string.Empty; }
public class ResendVerificationRequest { public string Email { get; set; } = string.Empty; }
public class ForgotPasswordRequest { public string Email { get; set; } = string.Empty; }
public class ResetPasswordRequest { public string Token { get; set; } = string.Empty; public string NewPassword { get; set; } = string.Empty; }
public class ChangePasswordRequest { public string CurrentPassword { get; set; } = string.Empty; public string NewPassword { get; set; } = string.Empty; }
public class MfaLoginRequest { public string MfaChallengeToken { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; public string? DeviceId { get; set; } public bool TrustDevice { get; set; } }
public class MfaVerifyRequest { public string Code { get; set; } = string.Empty; }
public class MfaDisableRequest { public string Password { get; set; } = string.Empty; }
