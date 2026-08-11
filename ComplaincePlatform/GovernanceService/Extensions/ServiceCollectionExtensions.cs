using FluentValidation;
using GovernanceService.Application.Contracts;
using GovernanceService.Application.Services;
using GovernanceService.Infrastructure.Clients;
using GovernanceService.Infrastructure.MasterData;
using GovernanceService.Infrastructure.Outbox;
using GovernanceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Communication;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Services;
using System.Text;

namespace GovernanceService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGovernanceDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<GovernanceDbContext>(options =>
            options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null))
                .AddInterceptors(Shared.Logging.Diagnostics.LoggingDbCommandInterceptor.Instance));
        services.AddScoped<IPlatformSequenceStore, EfGovernanceSequenceStore>();
        services.AddScoped<IPlatformIdGenerator, PlatformIdGenerator>();
        return services;
    }

    public static IServiceCollection AddGovernanceAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey is required");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer is required");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience is required");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
                ValidIssuer = issuer, ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });
        services.AddInternalApiAuthorization();
        return services;
    }

    public static IServiceCollection AddGovernanceApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IGovernanceAuditWriter, GovernanceAuditWriter>();
        services.AddScoped<IAssigneeValidationService, AssigneeValidationService>();
        services.AddHttpClient<IPersonServiceClient, PersonServiceClient>(c => c.BaseAddress = new Uri(configuration["PersonService:BaseUrl"] ?? "https://localhost:7203"))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();
        services.AddHttpClient<IWorkforceServiceClient, WorkforceServiceClient>(c => c.BaseAddress = new Uri(configuration["WorkforceService:BaseUrl"] ?? "https://localhost:7206"))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();
        services.AddHttpClient<IOrganizationServiceClient, OrganizationServiceClient>(c => c.BaseAddress = new Uri(configuration["OrganizationService:BaseUrl"] ?? "https://localhost:7204"))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();
        services.AddHttpClient<IIdentityContactClient, IdentityContactClient>(c => c.BaseAddress = new Uri(configuration["IdentityService:BaseUrl"] ?? "https://localhost:5001"))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();
        services.AddTransient<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();
        services.AddScoped<IResponsibilityAppService, ResponsibilityAppService>();
        services.AddScoped<ICommitteeAppService, CommitteeAppService>();
        services.AddScoped<IApprovalChainAppService, ApprovalChainAppService>();
        services.AddScoped<IEscalationChainAppService, EscalationChainAppService>();
        services.AddScoped<IApprovalWorkflowEvaluatorService, ApprovalWorkflowEvaluatorService>();
        services.AddScoped<IEscalationResolverService, EscalationResolverService>();
        services.AddScoped<IGovernanceTimelineService, GovernanceTimelineProjector>();
        services.AddScoped<IGovernanceAnalyticsService, GovernanceAnalyticsService>();
        services.AddScoped<IGovernanceKpiService, GovernanceAnalyticsService>();
        services.AddScoped<LookupAppService>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ResponsibilityAppService).Assembly));
        services.AddValidatorsFromAssemblyContaining<Application.Validators.CreateResponsibilityCommandValidator>();
        services.AddHostedService<GovernanceMasterDataBootstrapHostedService>();
        services.AddHostedService<GovernanceOutboxProcessor>();
        services.AddPramanCommunication(configuration);
        return services;
    }

    public static IServiceCollection AddGovernanceApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(o => { o.DefaultApiVersion = new ApiVersion(1, 0); o.AssumeDefaultVersionWhenUnspecified = true; o.ReportApiVersions = true; });
        services.AddVersionedApiExplorer(o => { o.GroupNameFormat = "'v'VVV"; o.SubstituteApiVersionInUrl = true; });
        return services;
    }
}
