using IdentityService.Application.Contracts;

namespace IdentityService.Infrastructure.Background;

/// <summary>
/// Refreshes cached security policy on a timer so runtime policy changes propagate.
/// </summary>
public sealed class IdentitySecurityPolicyBootstrapHostedService : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);
    private readonly IServiceProvider _services;
    private readonly ILogger<IdentitySecurityPolicyBootstrapHostedService> _logger;

    public IdentitySecurityPolicyBootstrapHostedService(
        IServiceProvider services,
        ILogger<IdentitySecurityPolicyBootstrapHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);
        await RefreshPolicyAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(RefreshInterval, stoppingToken);
            await RefreshPolicyAsync(stoppingToken);
        }
    }

    private async Task RefreshPolicyAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _services.CreateScope();
            var policyService = scope.ServiceProvider.GetRequiredService<ISecurityPolicyService>();
            var policy = await policyService.GetEffectivePolicyAsync(cancellationToken);
            Configuration.IdentitySecurityPolicyState.Update(policy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh identity security policy");
        }
    }
}
