using Microsoft.EntityFrameworkCore;
using ProfessionalService.Infrastructure.Persistence;
using System.Text.Json;

namespace ProfessionalService.Infrastructure.Background;

/// <summary>
/// Daily reminder for expiring training certificates / renewals (no Hangfire in ProfessionalService).
/// </summary>
public sealed class CertificationExpiryReminderHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CertificationExpiryReminderHostedService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private const int DefaultWithinDays = 30;

    public CertificationExpiryReminderHostedService(
        IServiceProvider services,
        ILogger<CertificationExpiryReminderHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Initial delay so startup migrations/bootstrap finish first.
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CertificationExpiryReminder failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    internal async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProfessionalDbContext>();
        var now = DateTime.UtcNow.Date;
        var until = now.AddDays(DefaultWithinDays);

        var trainingExpiring = await db.TrainingAttendances.AsNoTracking()
            .Where(a => !a.IsDeleted
                        && a.ExpiryDate != null
                        && a.ExpiryDate >= now
                        && a.ExpiryDate <= until
                        && a.CertificateUrl != null)
            .Select(a => new
            {
                a.Id,
                a.ProfessionalProfileId,
                a.ExpiryDate,
                a.CertificateUrl,
                a.TrainingSessionId
            })
            .Take(200)
            .ToListAsync(ct);

        var certExpiring = await db.Certifications.AsNoTracking()
            .Where(c => !c.IsDeleted
                        && c.ExpiryDate != null
                        && c.ExpiryDate >= now
                        && c.ExpiryDate <= until)
            .Select(c => new { c.Id, c.ProfessionalProfileId, c.CertificationName, c.ExpiryDate })
            .Take(200)
            .ToListAsync(ct);

        if (trainingExpiring.Count == 0 && certExpiring.Count == 0)
        {
            _logger.LogDebug("CertificationExpiryReminder: no expiries within {Days} days", DefaultWithinDays);
            return;
        }

        _logger.LogInformation(
            "CertificationExpiryReminder: {TrainingCount} training certificates and {CertCount} professional certifications expire within {Days} days",
            trainingExpiring.Count, certExpiring.Count, DefaultWithinDays);

        foreach (var row in trainingExpiring)
        {
            db.OutboxMessages.Add(new Domain.Entities.OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateId = row.Id,
                OccurredOn = DateTime.UtcNow,
                Type = "Training.CertificateExpiryReminder",
                Payload = JsonSerializer.Serialize(new
                {
                    AttendanceId = row.Id,
                    row.ProfessionalProfileId,
                    row.TrainingSessionId,
                    row.ExpiryDate,
                    row.CertificateUrl,
                    WithinDays = DefaultWithinDays
                }),
            });
        }

        foreach (var row in certExpiring)
        {
            db.OutboxMessages.Add(new Domain.Entities.OutboxMessage
            {
                Id = Guid.NewGuid(),
                AggregateId = row.Id,
                OccurredOn = DateTime.UtcNow,
                Type = "Professional.CertificationExpiryReminder",
                Payload = JsonSerializer.Serialize(new
                {
                    CertificationId = row.Id,
                    row.ProfessionalProfileId,
                    row.CertificationName,
                    row.ExpiryDate,
                    WithinDays = DefaultWithinDays
                }),
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
