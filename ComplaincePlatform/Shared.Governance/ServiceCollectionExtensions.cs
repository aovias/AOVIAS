using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Infrastructure.Auth;

namespace Shared.Governance;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPramanGovernance(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddTransient<InternalApiKeyForwardingHandler>();

        services.AddHttpClient<IGovernanceClient, GovernanceHttpClient>(c =>
            c.BaseAddress = new Uri(configuration["GovernanceService:BaseUrl"] ?? "https://localhost:7210"))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();

        services.AddHttpClient<IApprovalWorkflowEvaluator, GovernanceHttpClient>(c =>
            c.BaseAddress = new Uri(configuration["GovernanceService:BaseUrl"] ?? "https://localhost:7210"))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();

        services.AddHttpClient<IEscalationResolver, GovernanceHttpClient>(c =>
            c.BaseAddress = new Uri(configuration["GovernanceService:BaseUrl"] ?? "https://localhost:7210"))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();

        return services;
    }
}
