using IdentityService.Domain.Enums;

namespace IdentityService.Application.DTOs;

public sealed class TokenClaimsContext
{
    public required Guid UserId { get; init; }
    public required string Email { get; init; }
    public required string PlatformId { get; init; }
    public required string UserName { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
    public required UserAuthContextDto AuthContext { get; init; }
    public required Guid SessionId { get; init; }
    public required int TokenVersion { get; init; }
    public required string SecurityStamp { get; init; }
    public AuthenticationMethod AuthenticationMethod { get; init; } = AuthenticationMethod.Password;
    public IReadOnlyList<string> Scopes { get; init; } = Array.Empty<string>();
    public Guid? PersonId { get; init; }
    public Guid? EmploymentId { get; init; }
    public Guid? ProfessionalProfileId { get; init; }
}

public sealed class TokenPairResult
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required int ExpiresIn { get; init; }
    public required Guid SessionId { get; init; }
    public required string AccessTokenJti { get; init; }
}

public sealed class LoginResultDto
{
    public bool RequiresMfa { get; init; }
    public string? MfaChallengeToken { get; init; }
    public TokenPairResult? Tokens { get; init; }
    public Guid UserId { get; init; }
    public string PlatformId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public List<string> Roles { get; init; } = new();
    public Guid OrganizationId { get; init; }
    public Guid HospitalId { get; init; }
    public Guid SessionId { get; init; }
}

public sealed class RefreshResultDto
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required int ExpiresIn { get; init; }
    public Guid UserId { get; init; }
    public Guid SessionId { get; init; }
}

public sealed class SessionDto
{
    public Guid SessionId { get; init; }
    public string? DeviceId { get; init; }
    public string? IPAddress { get; init; }
    public string? UserAgent { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? LastActivityAt { get; init; }
    public bool IsCurrent { get; init; }
}

public sealed class RegisterResultDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string PlatformId { get; init; } = string.Empty;
    public Guid? PersonId { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool RequiresEmailVerification { get; init; }
    public bool EmailConfirmed { get; init; }
    public string EmailVerificationMode { get; init; } = "Async";
}

public sealed class AuthOperationResult
{
    public bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }
    public List<string> Details { get; init; } = new();

    public static AuthOperationResult Ok() => new() { Success = true };
    public static AuthOperationResult Fail(string code, string message, List<string>? details = null) =>
        new() { Success = false, ErrorCode = code, Message = message, Details = details ?? new() };
}
