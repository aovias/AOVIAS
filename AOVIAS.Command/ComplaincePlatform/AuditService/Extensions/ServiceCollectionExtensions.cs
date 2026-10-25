
using AuditService.Application.Contracts;
using AuditService.Application.Services;
using AuditService.Infrastructure.Persistence;
using AuditService.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Services;

namespace AuditService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiVersioningSystem(this IServiceCollection services)
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

        public static IServiceCollection AddAuthenticationSystem(this IServiceCollection services)
        {
            services.AddAuthentication("Bearer")
           .AddJwtBearer("Bearer", options =>
           {
               options.Authority = "https://localhost:7135";
               options.RequireHttpsMetadata = false;

               options.TokenValidationParameters = new TokenValidationParameters
               {
                   ValidateAudience = false
               };
           });

            // Register the scope handler for space-separated scope claims
            services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Shared.Infrastructure.Auth.ScopeHandler>();
            
            services.AddAuthorization(options =>
            {
                options.AddPolicy("AuditScope", policy =>
                {
                    // Use ScopeRequirement instead of RequireClaim to handle space-separated scopes
                    policy.Requirements.Add(new Shared.Infrastructure.Auth.ScopeRequirement("audit.api"));
                });
            });

            return services;
        }

        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AuditDbContext>(options =>
            {
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sql => sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null));
                options.AddInterceptors(Shared.Logging.Diagnostics.LoggingDbCommandInterceptor.Instance);
                
                // Suppress pending model changes warning in Development
                var environment = configuration["ASPNETCORE_ENVIRONMENT"];
                if (environment == "Development")
                {
                    options.ConfigureWarnings(warnings => 
                        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
                }
            });

            services.AddScoped<IPlatformSequenceStore, EfAuditSequenceStore>();
            services.AddScoped<IPlatformIdGenerator, PlatformIdGenerator>();

            return services;
        }

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<AuditAppService>();
            services.AddScoped<IReasoningCapture, ReasoningCapture>();

            return services;
        }

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped<IAuditRepository, AuditRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
