using IdentityService.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Hosting;
using Shared.Infrastructure.Auth;

namespace IdentityService.Infrastructure;

/// <summary>
/// Ensures canonical roles exist in AspNetRoles and migrates legacy role assignments to canonical names.
/// </summary>
public sealed class CanonicalRoleMigrationHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CanonicalRoleMigrationHostedService> _logger;

    public CanonicalRoleMigrationHostedService(
        IServiceProvider serviceProvider,
        ILogger<CanonicalRoleMigrationHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await RunMigrationAsync(cancellationToken);
    }

    private async Task RunMigrationAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

            foreach (var roleName in ApplicationRoles.AllSeededRoles)
            {
                var existing = await roleManager.FindByNameAsync(roleName);
                if (existing != null)
                    continue;

                var result = await roleManager.CreateAsync(new IdentityRole<Guid>(roleName));
                if (result.Succeeded)
                    _logger.LogInformation("Created Identity role {Role}", roleName);
                else
                    _logger.LogWarning("Failed to create Identity role {Role}: {Errors}", roleName,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
            }

            await MigrateLegacyAssignmentsAsync(userManager, roleManager, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Canonical role migration skipped because the Identity database is unavailable. " +
                "The service will continue starting; retry migration after database connectivity is restored.");
        }
    }

    private async Task MigrateLegacyAssignmentsAsync(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        CancellationToken cancellationToken)
    {
        // (legacyName, canonicalName) — skip if same
        var pairs = new (string Legacy, string Canonical)[]
        {
            ("User", ApplicationRoles.USER),
            ("GroupAdmin", ApplicationRoles.ORG_ADMIN),
            ("OrgAdmin", ApplicationRoles.ORG_ADMIN),
        };

        foreach (var (legacy, canonical) in pairs)
        {
            if (string.Equals(legacy, canonical, StringComparison.Ordinal))
                continue;

            if (!await roleManager.RoleExistsAsync(legacy))
                continue;

            if (!await roleManager.RoleExistsAsync(canonical))
                continue;

            var usersInLegacy = await userManager.GetUsersInRoleAsync(legacy);
            foreach (var user in usersInLegacy)
            {
                if (await userManager.IsInRoleAsync(user, canonical))
                {
                    await userManager.RemoveFromRoleAsync(user, legacy);
                    continue;
                }

                var add = await userManager.AddToRoleAsync(user, canonical);
                if (add.Succeeded)
                {
                    await userManager.RemoveFromRoleAsync(user, legacy);
                    _logger.LogInformation(
                        "Migrated user {UserId} from role {Legacy} to {Canonical}",
                        user.Id, legacy, canonical);
                }
                else
                {
                    _logger.LogWarning(
                        "Could not migrate user {UserId} to {Canonical}: {Errors}",
                        user.Id, canonical, string.Join(", ", add.Errors.Select(e => e.Description)));
                }
            }

            // Remove empty legacy role row if no users left
            var remaining = await userManager.GetUsersInRoleAsync(legacy);
            if (remaining.Count == 0)
            {
                var legacyRole = await roleManager.FindByNameAsync(legacy);
                if (legacyRole != null)
                {
                    await roleManager.DeleteAsync(legacyRole);
                    _logger.LogInformation("Removed legacy Identity role {Legacy}", legacy);
                }
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
