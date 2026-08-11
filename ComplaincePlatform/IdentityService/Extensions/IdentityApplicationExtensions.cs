using IdentityService.Application.Contracts;
using IdentityService.Application.Contracts.Security;
using IdentityService.Application.Services;
using IdentityService.Application.Services.Security;
using IdentityService.Application.Users;
using IdentityService.Infrastructure.Clients;
using IdentityService.Domain;

namespace IdentityService.Extensions;

public static class IdentityApplicationExtensions
{
    public static IServiceCollection AddIdentityApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Application.Configuration.IdentityCleanupOptions>(
            configuration.GetSection(Application.Configuration.IdentityCleanupOptions.SectionName));

        services.AddHttpContextAccessor();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SecurityEventService).Assembly));

        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<IMfaService, MfaService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IRevokedAccessTokenService, RevokedAccessTokenService>();
        services.AddScoped<ITokenValidationService, TokenValidationService>();
        services.AddScoped<ISecurityPolicyService, SecurityPolicyService>();
        services.AddScoped<IIdentityAuditService, IdentityAuditService>();
        services.AddScoped<IUserAdminAppService, UserAdminAppService>();
        services.AddScoped<IIdentityUnitOfWork, IdentityUnitOfWork>();

        services.AddScoped<ISecurityEventService, SecurityEventService>();
        services.AddScoped<ISessionHistoryService, SessionHistoryService>();
        services.AddScoped<ITrustedDeviceService, TrustedDeviceService>();
        services.AddScoped<IRiskScoringService, RiskScoringService>();
        services.AddScoped<IThreatDetectionService, ThreatDetectionService>();
        services.AddScoped<IAdaptiveAuthenticationService, AdaptiveAuthenticationService>();
        services.AddScoped<ISecurityTimelineService, SecurityTimelineService>();
        services.AddScoped<IIdentityAuditReadService, IdentityAuditReadService>();
        services.AddScoped<ISecurityDashboardService, SecurityDashboardService>();
        services.AddScoped<ISecurityPolicyAdminService, SecurityPolicyAdminService>();

        services.AddHttpClient<IOrganizationServiceClient, OrganizationServiceClient>(client =>
        {
            var baseUrl = configuration["OrganizationService:BaseUrl"] ?? "https://localhost:7181";
            client.BaseAddress = new Uri(baseUrl);
        })
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();

        services.AddHttpClient<IPersonServiceClient, PersonServiceClient>(client =>
        {
            var baseUrl = configuration["PersonService:BaseUrl"] ?? "https://localhost:7190";
            client.BaseAddress = new Uri(baseUrl);
        })
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();

        services.AddHttpClient<IWorkforceContextClient, WorkforceContextClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["WorkforceService:BaseUrl"] ?? "https://localhost:7195");
        })
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();

        services.AddHttpClient<IProfessionalContextClient, ProfessionalContextClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["ProfessionalService:BaseUrl"] ?? "https://localhost:7200");
        })
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();

        var enableBackgroundJobs = configuration.GetValue("Identity:EnableBackgroundJobs", true);
        if (enableBackgroundJobs)
        {
            services.AddHostedService<Infrastructure.Background.IdentityOutboxProcessor>();
            services.AddHostedService<Infrastructure.Background.IdentityCleanupHostedService>();
            services.AddHostedService<Infrastructure.Background.IdentitySecurityPolicyBootstrapHostedService>();
            services.AddHostedService<Infrastructure.Background.PersonIdBackfillHostedService>();
            services.AddHostedService<Infrastructure.Background.RiskScoreRecalculationHostedService>();
            services.AddHostedService<Infrastructure.Background.EmailVerificationReminderHostedService>();
        }

        return services;
    }
}
