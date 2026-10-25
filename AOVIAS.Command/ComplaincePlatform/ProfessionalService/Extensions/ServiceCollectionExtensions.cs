using FluentValidation;
using ProfessionalService.Application.Contracts;
using ProfessionalService.Application.Services;
using ProfessionalService.Application.Training;
using ProfessionalService.Infrastructure.Clients;
using ProfessionalService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Services;
using System.Text;

namespace ProfessionalService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProfessionalDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ProfessionalDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .AddInterceptors(Shared.Logging.Diagnostics.LoggingDbCommandInterceptor.Instance));
        services.AddScoped<IPlatformSequenceStore, EfProfessionalSequenceStore>();
        services.AddScoped<IPlatformIdGenerator, PlatformIdGenerator>();
        return services;
    }

    public static IServiceCollection AddProfessionalAuthentication(this IServiceCollection services, IConfiguration configuration)
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

    public static IServiceCollection AddProfessionalApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IProfessionalProfileAppService, ProfessionalProfileAppService>();
        services.AddScoped<IProfessionalAuditService, ProfessionalAuditService>();
        services.AddScoped<ITrainingAppService, TrainingAppService>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ProfessionalProfileAppService).Assembly));
        services.AddValidatorsFromAssemblyContaining<Application.Validators.CreateProfessionalProfileCommandValidator>();

        services.AddHttpClient<IWorkforceServiceClient, WorkforceServiceClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["WorkforceService:BaseUrl"] ?? "https://localhost:7195");
        })
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();

        services.AddHttpClient<IOrganizationServiceClient, OrganizationServiceClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["OrganizationService:BaseUrl"] ?? "https://localhost:7181");
        })
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .AddHttpMessageHandler<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();

        services.AddHostedService<Infrastructure.Background.ProfessionalOutboxProcessor>();
        services.AddHostedService<Infrastructure.Background.ProfessionalMasterDataBootstrapHostedService>();
        services.AddHostedService<Infrastructure.Background.CertificationExpiryReminderHostedService>();
        return services;
    }

    public static IServiceCollection AddProfessionalApiVersioning(this IServiceCollection services)
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
