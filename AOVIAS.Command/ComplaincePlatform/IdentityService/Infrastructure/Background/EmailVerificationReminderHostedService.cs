using IdentityService.Configuration;
using IdentityService.Domain;
using IdentityService.Domain.Enums;
using IdentityService.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Email;
using Shared.Logging;

namespace IdentityService.Infrastructure.Background;

/// <summary>Sends verification reminder emails for PendingVerification / unconfirmed accounts.</summary>
public sealed class EmailVerificationReminderHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailVerificationReminderHostedService> _logger;

    public EmailVerificationReminderHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<EmailVerificationReminderHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue("Identity:EnableBackgroundJobs", true))
            return;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email verification reminder job failed");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        var mode = EmailVerificationModeResolver.Resolve(_configuration);
        if (mode == EmailVerificationMode.None)
            return;

        var afterDays = _configuration.GetValue("Identity:EmailVerification:ReminderAfterDays", 3);
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, afterDays));

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var audit = scope.ServiceProvider.GetRequiredService<Application.Contracts.IIdentityAuditService>();

        var candidates = await db.Users
            .Where(u => !u.IsDeleted && !u.EmailConfirmed && u.CreatedAt <= cutoff
                && (u.Status == UserAccountStatus.PendingVerification || u.Status == UserAccountStatus.Active))
            .OrderBy(u => u.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        foreach (var user in candidates)
        {
            var recentReminder = await db.IdentityAuditLogs
                .AnyAsync(a => a.TargetUserId == user.Id
                    && a.Action == "VerificationReminder"
                    && a.OccurredAt > DateTime.UtcNow.AddDays(-2), ct);
            if (recentReminder)
                continue;

            var lifetimeHours = _configuration.GetValue("Identity:EmailVerification:TokenLifetimeHours", 48);
            var token = Infrastructure.Security.TokenHasher.GenerateOpaqueToken();
            db.EmailVerificationTokens.Add(new Domain.Entities.EmailVerificationToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = Infrastructure.Security.TokenHasher.Hash(token),
                ExpiresAt = DateTime.UtcNow.AddHours(Math.Max(1, lifetimeHours)),
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync(ct);

            var baseUrl = _configuration["Identity:PublicBaseUrl"] ?? "http://localhost:4200";
            var link = $"{baseUrl.TrimEnd('/')}/verify-email?token={Uri.EscapeDataString(token)}";
            var (subject, html) = IdentityEmailTemplates.Verification(link, user.Email);
            try
            {
                await email.SendEmailAsync(user.Email!, subject, html);
                await audit.RecordSuccessAsync(EventCode.EmailVerificationRequested, "VerificationReminder", targetUserId: user.Id, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send verification reminder to {UserId}", user.Id);
            }
        }
    }
}
