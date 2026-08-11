using IdentityService.Application.DTOs;

namespace IdentityService.Application.Contracts;

public interface IAuthenticationService
{
    Task<(LoginResultDto? result, AuthOperationResult status)> LoginAsync(
        string email,
        string password,
        string? deviceId,
        HttpContext httpContext,
        bool trustDevice = false,
        CancellationToken cancellationToken = default);

    Task<(LoginResultDto? result, AuthOperationResult status)> CompleteMfaLoginAsync(
        string mfaChallengeToken,
        string code,
        string? deviceId,
        HttpContext httpContext,
        bool trustDevice = false,
        CancellationToken cancellationToken = default);

    Task<(RefreshResultDto? result, AuthOperationResult status)> RefreshAsync(
        string refreshToken,
        HttpContext httpContext,
        Guid? hospitalId = null,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult> LogoutAsync(
        Guid userId,
        Guid? sessionId,
        string? refreshToken,
        string? accessTokenJti,
        DateTime? accessTokenExpiry,
        HttpContext httpContext,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult> LogoutAllAsync(
        Guid userId,
        HttpContext httpContext,
        CancellationToken cancellationToken = default);
}
