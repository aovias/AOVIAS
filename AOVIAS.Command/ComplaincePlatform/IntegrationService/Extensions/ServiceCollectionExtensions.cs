using IntegrationService.Application.Integration.Connectors;
using IntegrationService.Application.Integration.ImportExport;
using IntegrationService.Application.Integration.Mapping;
using IntegrationService.Application.Integration.Platform;
using IntegrationService.Application.Integration.Security;
using IntegrationService.Application.Integration.Synchronization;
using IntegrationService.Application.Integration.Webhooks;
using IntegrationService.Application.Handlers;
using IntegrationService.Application.Validators;
using IntegrationService.Infrastructure.Background;
using IntegrationService.Infrastructure.Clients;
using IntegrationService.Infrastructure.Persistence;
using IntegrationService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using FluentValidation;
using Shared.Authorization;
using Shared.Infrastructure.Auth;
using System.Text;

namespace IntegrationService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddIntegrationDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IntegrationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .AddInterceptors(Shared.Logging.Diagnostics.LoggingDbCommandInterceptor.Instance));
        return services;
    }

    public static IServiceCollection AddIntegrationAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey is required");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"], ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)), ClockSkew = TimeSpan.Zero
            };
        });
        services.AddInternalApiAuthorization();
        services.AddPramanAuthorization(configuration);
        services.AddTransient<InternalApiKeyForwardingHandler>();
        return services;
    }

    public static IServiceCollection AddIntegrationApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IntegrationOptions>(configuration.GetSection(IntegrationOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddHttpClient();

        foreach (var serviceName in new[] { "PersonService", "WorkforceService", "OrganizationService" })
        {
            services.AddHttpClient($"Integration.{serviceName}", c =>
                c.BaseAddress = new Uri(configuration[$"{serviceName}:BaseUrl"] ?? "https://localhost"))
                .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
                .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();
        }
        services.AddScoped<IPlatformWriteClient, PlatformWriteClient>();

        services.AddSingleton<IConnectorProvider, CsvImportConnectorProvider>();
        services.AddSingleton<IConnectorProvider, JsonImportConnectorProvider>();
        services.AddSingleton<IConnectorProvider, CsvExportConnectorProvider>();
        services.AddSingleton<IConnectorProvider, WebhookInboundConnectorProvider>();
        services.AddSingleton<IConnectorProvider, WebhookOutboundConnectorProvider>();
        services.AddSingleton<IConnectorProvider, LdapConnectorProvider>();
        services.AddSingleton<IConnectorProvider, AzureAdConnectorProvider>();
        foreach (var stub in ConnectorProviderRegistration.StubTypes)
            services.AddSingleton<IConnectorProvider>(sp => new StubConnectorProvider(stub));

        services.AddSingleton<IConnectorRegistry, ConnectorRegistry>();
        services.AddScoped<IMappingEngine, MappingEngine>();
        services.AddScoped<MappingProfileService>();
        services.AddScoped<ISynchronizationEngine, SynchronizationEngine>();
        services.AddScoped<SynchronizationJobService>();
        services.AddScoped<IWebhookService, WebhookService>();
        services.AddScoped<WebhookService>();
        services.AddScoped<IImportExportEngine, ImportExportEngine>();
        services.AddScoped<ConnectorAppService>();
        services.AddScoped<ConnectorHealthService>();
        services.AddScoped<IntegrationAuditService>();
        services.AddScoped<IIntegrationAuditService>(sp => sp.GetRequiredService<IntegrationAuditService>());
        services.AddScoped<IIntegrationOutboxWriter, IntegrationOutboxWriter>();
        services.AddSingleton<ICredentialProtector, CredentialProtector>();
        services.AddScoped<IIntegrationAuthorizationService, IntegrationAuthorizationService>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IntegrationHandlers).Assembly));
        services.AddValidatorsFromAssemblyContaining<CreateConnectorRequestValidator>();
        services.AddHostedService<SyncSchedulerHostedService>();
        services.AddHostedService<IntegrationEventConsumerHostedService>();
        services.AddHostedService<IntegrationOutboxProcessor>();
        services.AddHostedService<WebhookRetryHostedService>();
        return services;
    }

    private static void RegisterHttpClient<TInterface, TImpl>(IServiceCollection services, IConfiguration configuration, string section)
        where TInterface : class where TImpl : class, TInterface
    {
        services.AddHttpClient<TInterface, TImpl>(c =>
            c.BaseAddress = new Uri(configuration[$"{section}:BaseUrl"] ?? "https://localhost"))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();
    }

    public static IServiceCollection AddIntegrationApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(o => { o.DefaultApiVersion = new ApiVersion(1, 0); o.AssumeDefaultVersionWhenUnspecified = true; o.ReportApiVersions = true; });
        services.AddVersionedApiExplorer(o => { o.GroupNameFormat = "'v'VVV"; o.SubstituteApiVersionInUrl = true; });
        return services;
    }
}
