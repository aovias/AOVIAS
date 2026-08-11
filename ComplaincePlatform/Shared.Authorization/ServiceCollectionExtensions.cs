using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Shared.Infrastructure.Auth;

namespace Shared.Authorization;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPramanAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        // Required by the HttpClient pipeline below — callers historically forgot this registration,
        // which surfaced as 400 VALIDATION_ERROR ("InternalApiKeyForwardingHandler has not been registered")
        // on every authorized AI / AccessGovernance-backed request.
        services.TryAddTransient<InternalApiKeyForwardingHandler>();

        services.AddHttpClient<IAuthorizationEvaluator, AccessGovernanceAuthorizationClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["AccessGovernanceService:BaseUrl"] ?? "https://localhost:7205");
        })
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();

        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        return services;
    }
}

internal sealed class PermissionAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (PermissionPolicyRegistry.TryParsePolicyName(policyName, out var permissionCode))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permissionCode))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
