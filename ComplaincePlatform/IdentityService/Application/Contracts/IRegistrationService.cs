using IdentityService.Application.DTOs;

namespace IdentityService.Application.Contracts;

public interface IRegistrationService
{
    Task<(RegisterResultDto? result, AuthOperationResult status)> RegisterSelfAsync(
        string email,
        string password,
        string? role,
        HttpContext httpContext,
        CancellationToken cancellationToken = default);

    Task<(RegisterResultDto? result, AuthOperationResult status)> RegisterInternalAsync(
        string email,
        string password,
        string? role,
        bool emailConfirmed,
        HttpContext? httpContext,
        CancellationToken cancellationToken = default);

    Task<AuthOperationResult> VerifyEmailAsync(string token, CancellationToken cancellationToken = default);

    Task<(bool success, string message, AuthOperationResult status)> ResendVerificationAsync(
        string email,
        HttpContext httpContext,
        CancellationToken cancellationToken = default);
}
