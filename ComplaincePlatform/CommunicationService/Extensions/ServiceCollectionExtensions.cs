using CommunicationService.Application.Commands;
using CommunicationService.Application.Contracts;
using CommunicationService.Application.Queries;
using CommunicationService.Application.Services;
using CommunicationService.Infrastructure.Background;
using CommunicationService.Infrastructure.Clients;
using CommunicationService.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Email;
using Shared.Infrastructure.Extensions;
using System.Text;

namespace CommunicationService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCommunicationDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CommunicationDbContext>(options =>
            options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null))
                .AddInterceptors(Shared.Logging.Diagnostics.LoggingDbCommandInterceptor.Instance));
        return services;
    }

    public static IServiceCollection AddCommunicationAuthentication(this IServiceCollection services, IConfiguration configuration)
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
        services.AddTransient<InternalApiKeyForwardingHandler>();
        return services;
    }

    public static IServiceCollection AddCommunicationApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddConfigurableEmailService(configuration);
        services.AddScoped<ICommunicationChannelProvider, EmailChannelProvider>();
        services.AddScoped<ICommunicationChannelProvider, InAppChannelProvider>();
        services.AddScoped<ICommunicationChannelProvider>(_ => new StubChannelProvider(Domain.Enums.CommunicationChannelType.Sms));
        services.AddScoped<ICommunicationChannelProvider>(_ => new StubChannelProvider(Domain.Enums.CommunicationChannelType.Teams));
        services.AddScoped<ICommunicationChannelProvider>(_ => new StubChannelProvider(Domain.Enums.CommunicationChannelType.Slack));
        services.AddScoped<AvailabilityEngine>();
        services.AddScoped<TemplateRenderer>();
        services.AddScoped<DeliveryOrchestrator>();
        services.AddScoped<ICommunicationRequestService, CommunicationRequestService>();
        services.AddScoped<INotificationInboxService, NotificationInboxService>();
        services.AddScoped<IPreferenceService, PreferenceService>();
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<ICommunicationTimelineService, CommunicationTimelineService>();
        services.AddScoped<IDeliveryQueryService, DeliveryQueryService>();
        services.AddScoped<IReminderRuleService, ReminderRuleService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IEventSubscriptionProcessor, EventSubscriptionProcessor>();
        services.AddScoped<INotificationAdminService, NotificationAdminService>();
        services.AddHttpClient<IPersonContactClient, PersonContactClient>(c => c.BaseAddress = new Uri(configuration["PersonService:BaseUrl"] ?? "https://localhost:7203"))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();
        services.AddHttpClient<IIdentityPersonClient, IdentityPersonClient>(c => c.BaseAddress = new Uri(configuration["IdentityService:BaseUrl"] ?? "https://localhost:5001"))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();
        services.AddHttpClient<IGatewayRealtimeClient, GatewayRealtimeClient>(c =>
            c.BaseAddress = new Uri(configuration["Gateway:BaseUrl"] ?? "http://localhost:5212"))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CommunicationRequestService).Assembly));
        services.AddHostedService<CommunicationOutboxProcessor>();
        services.AddHostedService<OutboundMessageProcessor>();
        services.AddHostedService<DigestNotificationHostedService>();
        services.AddHostedService<ReminderSchedulerHostedService>();
        services.AddHostedService<ComplianceEventConsumerHostedService>();
        services.AddHostedService<CommunicationBootstrapHostedService>();
        return services;
    }

    public static IServiceCollection AddCommunicationApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(o => { o.DefaultApiVersion = new ApiVersion(1, 0); o.AssumeDefaultVersionWhenUnspecified = true; o.ReportApiVersions = true; });
        services.AddVersionedApiExplorer(o => { o.GroupNameFormat = "'v'VVV"; o.SubstituteApiVersionInUrl = true; });
        return services;
    }
}
