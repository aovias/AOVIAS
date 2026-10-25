using FluentValidation;
using AccessGovernanceService.Application.Contracts;
using AccessGovernanceService.Application.Services;
using AccessGovernanceService.Infrastructure.Clients;
using AccessGovernanceService.Infrastructure.MasterData;
using AccessGovernanceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Services;
using System.Text;

namespace AccessGovernanceService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAccessGovernanceDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AccessGovernanceDbContext>(options =>
            options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null))
                .AddInterceptors(Shared.Logging.Diagnostics.LoggingDbCommandInterceptor.Instance));
        services.AddScoped<IPlatformSequenceStore, EfAccessGovernanceSequenceStore>();
        services.AddScoped<IPlatformIdGenerator, PlatformIdGenerator>();
        return services;
    }

    public static IServiceCollection AddAccessGovernanceAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey is required");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer is required");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience is required");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });
        services.AddInternalApiAuthorization();
        return services;
    }

    public static IServiceCollection AddAccessGovernanceApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IRoleAppService, RoleAppService>();
        services.AddScoped<IPermissionAppService, PermissionAppService>();
        services.AddScoped<IPolicyAppService, PolicyAppService>();
        services.AddScoped<IDelegationAppService, DelegationAppService>();
        services.AddScoped<IResponsibilityAppService, ResponsibilityAppService>();
        services.AddScoped<IApprovalAuthorityAppService, ApprovalAuthorityAppService>();
        services.AddScoped<IAuthorizationEvaluatorService, AuthorizationEvaluatorService>();
        services.AddScoped<LookupAppService>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RoleAppService).Assembly));
        services.AddValidatorsFromAssemblyContaining<Application.Validators.CreateRoleCommandValidator>();

        services.AddTransient<InternalApiKeyForwardingHandler>();

        services.AddHttpClient<IWorkforceServiceClient, WorkforceServiceClient>(c => c.BaseAddress = new Uri(configuration["WorkforceService:BaseUrl"] ?? "https://localhost:7195"))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();
        services.AddHttpClient<IProfessionalServiceClient, ProfessionalServiceClient>(c => c.BaseAddress = new Uri(configuration["ProfessionalService:BaseUrl"] ?? "https://localhost:7200"))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();
        services.AddHttpClient<IIdentityServiceClient, IdentityServiceClient>(c => c.BaseAddress = new Uri(configuration["IdentityService:BaseUrl"] ?? "https://localhost:7180"))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();

        services.AddHttpClient<IOrganizationServiceClient, OrganizationServiceClient>(c => c.BaseAddress = new Uri(configuration["OrganizationService:BaseUrl"] ?? "https://localhost:7185"))
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<InternalApiKeyForwardingHandler>();

        services.AddHostedService<AccessGovernanceMasterDataBootstrapHostedService>();
        services.AddHostedService<Infrastructure.Outbox.AccessGovernanceOutboxProcessor>();
        return services;
    }

    public static IServiceCollection AddAccessGovernanceApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });
        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });
        return services;
    }
}
