using EnterpriseDirectoryService.Application.Directory.Configuration;
using EnterpriseDirectoryService.Application.Directory.Indexing;
using EnterpriseDirectoryService.Application.Directory.NaturalLanguage;
using EnterpriseDirectoryService.Application.Directory.Platform;
using EnterpriseDirectoryService.Application.Directory.Profiles;
using EnterpriseDirectoryService.Application.Directory.SavedSearch;
using EnterpriseDirectoryService.Application.Directory.Search;
using EnterpriseDirectoryService.Application.Directory.Security;
using EnterpriseDirectoryService.Application.Handlers;
using EnterpriseDirectoryService.Infrastructure.Background;
using EnterpriseDirectoryService.Infrastructure.Clients;
using EnterpriseDirectoryService.Infrastructure.Persistence;
using EnterpriseDirectoryService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Authorization;
using Shared.Infrastructure.Auth;
using System.Text;

namespace EnterpriseDirectoryService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEnterpriseDirectoryDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EnterpriseDirectoryDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
                .AddInterceptors(Shared.Logging.Diagnostics.LoggingDbCommandInterceptor.Instance));
        return services;
    }

    public static IServiceCollection AddEnterpriseDirectoryAuthentication(this IServiceCollection services, IConfiguration configuration)
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

    public static IServiceCollection AddEnterpriseDirectoryApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DirectoryOptions>(configuration.GetSection(DirectoryOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        RegisterHttpClient<IPersonSourceClient, PersonSourceClient>(services, configuration, "PersonService");
        RegisterHttpClient<IWorkforceSourceClient, WorkforceSourceClient>(services, configuration, "WorkforceService");
        RegisterHttpClient<IProfessionalSourceClient, ProfessionalSourceClient>(services, configuration, "ProfessionalService");
        RegisterHttpClient<IGovernanceSourceClient, GovernanceSourceClient>(services, configuration, "GovernanceService");
        RegisterHttpClient<IAIOrchestratorClient, AIOrchestratorClient>(services, configuration, "AIOrchestratorService");

        services.AddScoped<IDirectoryIndexProjector, DirectoryIndexProjector>();
        services.AddScoped<IDirectorySearchEngine, DirectorySearchService>();
        services.AddScoped<IDirectoryAuthorizationService, DirectoryAuthorizationService>();
        services.AddScoped<IDirectoryProfileComposer, DirectoryProfileComposer>();
        services.AddScoped<INaturalLanguageQueryTranslator, NaturalLanguageQueryTranslator>();
        services.AddScoped<ISavedSearchService, SavedSearchService>();
        services.AddScoped<ISearchHistoryService, SearchHistoryService>();
        services.AddScoped<ISearchPreferenceService, SearchPreferenceService>();
        services.AddScoped<DirectoryOrchestrationService>();
        services.AddScoped<IDirectoryOutboxWriter, DirectoryOutboxWriter>();
        services.AddScoped<IDirectoryProjectionRegistry, DirectoryProjectionRegistry>();

        foreach (var eventType in new[]
        {
            "EmploymentCreated", "EmploymentTransferred", "EmploymentTerminated",
            "ProfessionalProfileUpdated", "QualificationAdded", "TrainingCompleted",
            "CertificationRenewed", "LicenseRenewed", "SkillUpdated", "PersonUpdated"
        })
        {
            if (eventType == "PersonUpdated")
                services.AddScoped<IDirectoryProjectionHandler, PersonUpdatedProjection>();
            else if (eventType.StartsWith("Employment"))
                services.AddScoped<IDirectoryProjectionHandler>(sp => new EmploymentProjection(sp.GetRequiredService<IDirectoryIndexProjector>(), eventType));
            else
                services.AddScoped<IDirectoryProjectionHandler>(sp => new ProfessionalProjection(sp.GetRequiredService<IDirectoryIndexProjector>(), eventType));
        }

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DirectoryHandlers).Assembly));
        services.AddHostedService<IndexRebuildSchedulerHostedService>();
        services.AddHostedService<DirectoryEventConsumerHostedService>();
        services.AddHostedService<DirectoryOutboxProcessor>();
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

    public static IServiceCollection AddEnterpriseDirectoryApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(o => { o.DefaultApiVersion = new ApiVersion(1, 0); o.AssumeDefaultVersionWhenUnspecified = true; o.ReportApiVersions = true; });
        services.AddVersionedApiExplorer(o => { o.GroupNameFormat = "'v'VVV"; o.SubstituteApiVersionInUrl = true; });
        return services;
    }
}
