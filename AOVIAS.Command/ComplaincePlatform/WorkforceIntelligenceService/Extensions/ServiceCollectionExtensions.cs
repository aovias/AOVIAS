using WorkforceIntelligenceService.Application.Intelligence.Forecasting;
using WorkforceIntelligenceService.Application.Intelligence.Platform;
using WorkforceIntelligenceService.Application.Intelligence.Projections;
using WorkforceIntelligenceService.Application.Intelligence.Queries;
using WorkforceIntelligenceService.Application.Intelligence.Recommendations;
using WorkforceIntelligenceService.Application.Intelligence.Snapshots;
using WorkforceIntelligenceService.Infrastructure.Background;
using WorkforceIntelligenceService.Infrastructure.Clients;
using WorkforceIntelligenceService.Infrastructure.Persistence;
using WorkforceIntelligenceService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Authorization;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Extensions;
using System.Text;

namespace WorkforceIntelligenceService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkforceIntelligenceDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WorkforceIntelligenceDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .AddInterceptors(Shared.Logging.Diagnostics.LoggingDbCommandInterceptor.Instance));
        return services;
    }

    public static IServiceCollection AddWorkforceIntelligenceAuthentication(this IServiceCollection services, IConfiguration configuration)
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

    public static IServiceCollection AddWorkforceIntelligenceApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IntelligenceOptions>(configuration.GetSection(IntelligenceOptions.SectionName));
        services.AddHttpContextAccessor();

        RegisterHttpClient<IWorkforceSourceClient, WorkforceSourceClient>(services, configuration, "WorkforceService");
        RegisterHttpClient<IProfessionalSourceClient, ProfessionalSourceClient>(services, configuration, "ProfessionalService");
        RegisterHttpClient<IGovernanceSourceClient, GovernanceSourceClient>(services, configuration, "GovernanceService");
        RegisterHttpClient<IOrganizationSourceClient, OrganizationSourceClient>(services, configuration, "OrganizationService");

        services.AddScoped<IWorkforceSnapshotService, WorkforceSnapshotService>();
        services.AddScoped<ISnapshotJob>(sp => sp.GetRequiredService<IWorkforceSnapshotService>() as WorkforceSnapshotService ?? throw new InvalidOperationException());
        services.AddScoped<IIntelligenceQueryService, IntelligenceQueryService>();
        services.AddScoped<IForecastEngine, ForecastEngine>();
        services.AddScoped<IRecommendationOrchestrator, RecommendationOrchestrator>();
        services.AddScoped<IAnalyticsCacheService, AnalyticsCacheService>();
        services.AddScoped<IIntelligenceOutboxWriter, IntelligenceOutboxWriter>();
        services.AddScoped<IIntelligenceProjectionRegistry, IntelligenceProjectionRegistry>();

        foreach (var eventType in new[]
        {
            "EmploymentCreated", "EmploymentTransferred", "EmploymentTerminated",
            "QualificationAdded", "TrainingCompleted", "ResponsibilityAssigned",
            "CommitteeDecisionRecorded", "LicenseRenewed", "PersonUpdated"
        })
        {
            services.AddScoped<IIntelligenceProjectionHandler>(sp =>
                new SnapshotRefreshProjection(
                    sp.GetRequiredService<WorkforceIntelligenceDbContext>(),
                    sp.GetRequiredService<IWorkforceSnapshotService>(),
                    sp.GetRequiredService<ILogger<SnapshotRefreshProjection>>(),
                    eventType));
        }
        services.AddScoped<IIntelligenceProjectionHandler, CertificationExpiredProjection>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IntelligenceQueryService).Assembly));
        services.AddHostedService<SnapshotSchedulerHostedService>();
        services.AddHostedService<IntelligenceEventConsumerHostedService>();
        services.AddHostedService<IntelligenceOutboxProcessor>();
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

    public static IServiceCollection AddWorkforceIntelligenceApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(o => { o.DefaultApiVersion = new ApiVersion(1, 0); o.AssumeDefaultVersionWhenUnspecified = true; o.ReportApiVersions = true; });
        services.AddVersionedApiExplorer(o => { o.GroupNameFormat = "'v'VVV"; o.SubstituteApiVersionInUrl = true; });
        return services;
    }
}
