namespace IdentityService.Application.Contracts;

public interface IMfaService
{
    Task<(bool success, string? qrUri, string? sharedKey, string? message)> SetupAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(bool success, string? message)> VerifySetupAsync(Guid userId, string code, CancellationToken cancellationToken = default);
    Task<(bool success, string? message)> DisableAsync(Guid userId, string password, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetRecoveryCodesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<(bool success, string? message)> VerifyLoginCodeAsync(Guid userId, string code, CancellationToken cancellationToken = default);
    Task<string?> CreateLoginChallengeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Guid?> ValidateLoginChallengeAsync(string challengeToken, CancellationToken cancellationToken = default);
}
