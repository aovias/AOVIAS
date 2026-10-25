using IdentityService.Application.DTOs;
using IdentityService.Domain.Entities;

namespace IdentityService.Application.Contracts;

public interface ISessionService
{
    Task<(UserSession session, string refreshToken)> CreateSessionAsync(
        Guid userId,
        string? deviceId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<(RefreshResultDto? rotation, Guid userId, Guid sessionId)?> RotateRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    Task RevokeSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task RevokeSessionByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionDto>> ListSessionsAsync(Guid userId, Guid? currentSessionId, CancellationToken cancellationToken = default);
    Task<UserSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
