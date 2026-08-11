using FluentValidation;
using WorkforceService.Application.Contracts;
using WorkforceService.Application.Services;
using WorkforceService.Infrastructure.Clients;
using WorkforceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Services;
using System.Text;

namespace WorkforceService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkforceDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<WorkforceDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .AddInterceptors(Shared.Logging.Diagnostics.LoggingDbCommandInterceptor.Instance));
        services.AddScoped<IPlatformSequenceStore, EfWorkforceSequenceStore>();
        services.AddScoped<IPlatformIdGenerator, PlatformIdGenerator>();
        return services;
    }

    public static IServiceCollection AddWorkforceAuthentication(this IServiceCollection services, IConfiguration configuration)
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
        services.AddTransient<InternalApiKeyForwardingHandler>();
        return services;
    }

    public static IServiceCollection AddWorkforceApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IEmploymentAppService, EmploymentAppService>();
        services.AddScoped<IWorkforceAuditService, WorkforceAuditService>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(EmploymentAppService).Assembly));
        services.AddValidatorsFromAssemblyContaining<Application.Validators.CreateEmploymentCommandValidator>();

        services.AddHttpClient<IPersonServiceClient, PersonServiceClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["PersonService:BaseUrl"] ?? "https://localhost:7190");
        })
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();

        services.AddHttpClient<IOrganizationServiceClient, OrganizationServiceClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["OrganizationService:BaseUrl"] ?? "https://localhost:7181");
        })
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();

        services.AddHostedService<Infrastructure.Background.WorkforceOutboxProcessor>();
        services.AddHostedService<Infrastructure.Background.WorkforceMasterDataBootstrapHostedService>();
        return services;
    }

    public static IServiceCollection AddWorkforceApiVersioning(this IServiceCollection services)
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
