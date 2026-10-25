using IdentityService.Application.Configuration;
using IdentityService.Domain.Enums;
using IdentityService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IdentityService.Infrastructure.Background;

/// <summary>
/// Purges expired sessions, revoked JWT records, MFA challenges, and stale login attempts.
/// </summary>
public sealed class IdentityCleanupHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<IdentityCleanupHostedService> _logger;
    private readonly IdentityCleanupOptions _options;

    public IdentityCleanupHostedService(
        IServiceProvider services,
        IOptions<IdentityCleanupOptions> options,
        ILogger<IdentityCleanupHostedService> logger)
    {
        _services = services;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(25), stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Identity cleanup job failed");
            }

            await Task.Delay(TimeSpan.FromHours(_options.IntervalHours), stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var now = DateTime.UtcNow;

        var expiredSessions = await db.UserSessions
            .Where(s => s.Status == SessionStatus.Active && s.ExpiresAt <= now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, SessionStatus.Expired), cancellationToken);

        var oldRevokedSessions = await db.UserSessions
            .Where(s => s.Status != SessionStatus.Active && s.RevokedAt != null
                        && s.RevokedAt < now.AddDays(-_options.RevokedSessionRetentionDays))
            .ExecuteDeleteAsync(cancellationToken);

        var expiredJtis = await db.RevokedAccessTokens
            .Where(x => x.ExpiresAt <= now)
            .ExecuteDeleteAsync(cancellationToken);

        var staleMfa = await db.MfaChallenges
            .Where(c => c.ExpiresAt < now.AddHours(-1))
            .ExecuteDeleteAsync(cancellationToken);

        var staleLoginAttempts = await db.LoginAttempts
            .Where(a => a.CreatedAt < now.AddDays(-_options.LoginAttemptRetentionDays))
            .ExecuteDeleteAsync(cancellationToken);

        var expiredDevices = await db.TrustedDevices
            .Where(d => !d.IsDeleted && d.TrustedUntil < now && d.RevokedAt == null)
            .ExecuteUpdateAsync(d => d
                .SetProperty(x => x.IsDeleted, true)
                .SetProperty(x => x.RevokedAt, now), cancellationToken);

        var staleHistory = await db.SessionHistories
            .Where(h => h.OccurredAt < now.AddDays(-_options.SessionHistoryRetentionDays))
            .ExecuteDeleteAsync(cancellationToken);

        if (expiredSessions + oldRevokedSessions + expiredJtis + staleMfa + staleLoginAttempts + expiredDevices + staleHistory > 0)
        {
            _logger.LogInformation(
                "Identity cleanup: sessions expired={Expired}, sessions deleted={Deleted}, jtis={Jtis}, mfa={Mfa}, loginAttempts={Attempts}, devices={Devices}, history={History}",
                expiredSessions, oldRevokedSessions, expiredJtis, staleMfa, staleLoginAttempts, expiredDevices, staleHistory);
        }
    }
}
