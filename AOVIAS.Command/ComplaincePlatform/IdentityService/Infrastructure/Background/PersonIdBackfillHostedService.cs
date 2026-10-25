using IdentityService.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Infrastructure.Background;

/// <summary>
/// One-time best-effort backfill: creates Person records for AppUsers missing PersonId.
/// </summary>
public sealed class PersonIdBackfillHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PersonIdBackfillHostedService> _logger;

    public PersonIdBackfillHostedService(IServiceProvider services, ILogger<PersonIdBackfillHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var personClient = scope.ServiceProvider.GetRequiredService<Application.Contracts.IPersonServiceClient>();

        var users = await db.Users
            .Where(u => u.PersonId == null && !u.IsDeleted)
            .Take(100)
            .ToListAsync(stoppingToken);

        foreach (var user in users)
        {
            if (string.IsNullOrWhiteSpace(user.Email)) continue;
            var link = await personClient.CreatePersonForRegistrationAsync(user.Email, user.UserName, stoppingToken);
            if (link == null)
            {
                _logger.LogWarning("Backfill skipped user {UserId}: PersonService unavailable", user.Id);
                continue;
            }

            user.PersonId = link.PersonId;
            user.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Backfilled PersonId {PersonId} for user {UserId}", link.PersonId, user.Id);
        }

        if (users.Count > 0)
            await db.SaveChangesAsync(stoppingToken);
    }
}
