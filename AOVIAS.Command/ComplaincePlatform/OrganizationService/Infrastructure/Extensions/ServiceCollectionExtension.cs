using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrganizationService.Application.Contracts;
using OrganizationService.Application.Services;
using OrganizationService.Infrastructure.MasterData;
using OrganizationService.Infrastructure.Persistence;
using OrganizationService.Infrastructure.Repositories;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Services;
using System.Text;
using FluentValidation;

namespace OrganizationService.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration config)
        {
            services.AddDbContext<OrganizationDbContext>(options =>
            {
                options.UseSqlServer(
                    config.GetConnectionString("DefaultConnection"),
                    sql => sql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null));
                options.AddInterceptors(Shared.Logging.Diagnostics.LoggingDbCommandInterceptor.Instance);
                
                // Suppress pending model changes warning in Development
                var environment = config["ASPNETCORE_ENVIRONMENT"];
                if (environment == "Development")
                {
                    options.ConfigureWarnings(warnings => 
                        warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
                }
            });

            services.AddScoped<IPlatformSequenceStore, EfOrganizationSequenceStore>();
            services.AddScoped<IPlatformIdGenerator, PlatformIdGenerator>();

            return services;
        }
        public static IServiceCollection AddAuthenticationSystem(this IServiceCollection services, IConfiguration configuration)
        {
            // Get JWT settings - these must match IdentityService configuration
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey is required. Must match IdentityService secret key.");
            var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer is required. Must match IdentityService issuer.");
            var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience is required. Must match IdentityService audience.");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
           .AddJwtBearer(options =>
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
                   ClockSkew = TimeSpan.Zero // Strict token expiry validation
               };

               // Token validation events
               options.Events = new JwtBearerEvents
               {
                   OnAuthenticationFailed = context =>
                   {
                       if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                       {
                           context.Response.Headers.Append("Token-Expired", "true");
                       }
                       return Task.CompletedTask;
                   }
               };
           });

            // Register the scope handler for space-separated scope claims
            services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Shared.Infrastructure.Auth.ScopeHandler>();
            
            services.AddAuthorization(options =>
            {
                options.AddPolicy("OrganizationScope", policy =>
                {
                    // Use ScopeRequirement instead of RequireClaim to handle space-separated scopes
                    policy.Requirements.Add(new Shared.Infrastructure.Auth.ScopeRequirement("organization.api"));
                });
            });

            services.AddInternalApiAuthorization();

            return services;
        }

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

        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<OrganizationAppService>();
            services.AddScoped<HospitalAppService>();
            services.AddScoped<OrganizationMemberAppService>();
            services.AddScoped<CommonLookupAppService>();

            // Enterprise Configuration API (Stage 5B.1) — one service resolves every Category A
            // configuration domain from the shared metadata store.
            services.AddScoped<PlatformConfigurationAppService>();
            services.AddScoped<SubscriptionAppService>();
            services.AddScoped<EntitlementEngine>();
            services.AddScoped<LicenseAppService>();
            services.AddScoped<OnboardingTelemetryAppService>();
            services.AddScoped<CommercialAdminAuditAppService>();
            services.AddScoped<OrganizationCommercialAdminAppService>();
            services.AddScoped<EmailDeliveryLogAppService>();
            services.AddScoped<TrialLifecycleEngine>();
            services.AddScoped<IOrganizationTimelineService, OrganizationTimelineProjector>();
            services.AddScoped<IMembershipAppService, MembershipAppService>();
            services.AddScoped<IInvitationAppService, InvitationAppService>();
            services.AddScoped<IOrganizationalUnitAppService, OrganizationalUnitAppService>();
            services.AddScoped<IDepartmentHierarchyAppService, DepartmentHierarchyAppService>();
            services.AddScoped<IOrganizationGraphAppService, OrganizationGraphAppService>();
            services.AddScoped<IOrganizationSettingsAppService, OrganizationSettingsAppService>();
            services.AddScoped<IOrganizationBrandingAppService, OrganizationBrandingAppService>();
            services.AddScoped<IFileStorageService, Infrastructure.Storage.LocalFileStorageService>();
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MembershipAppService).Assembly));
            services.AddValidatorsFromAssemblyContaining<Application.Validators.CreateMembershipCommandValidator>();
            services.AddHostedService<OrganizationMasterDataBootstrapHostedService>();
            services.AddHostedService<Infrastructure.Background.TrialLifecycleHostedService>();
            return services;
        }

        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
        {
            services.AddScoped<IOrganizationRepository, OrganizationRepository>();
            services.AddScoped<IHospitalRepository, HospitalRepository>();
            services.AddScoped<IOrganizationMemberRepository, OrganizationMemberRepository>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IServiceRepository, ServiceRepository>();
            services.AddScoped<IMasterDepartmentRepository, MasterDepartmentRepository>();
            services.AddScoped<IHospitalMasterDepartmentRepository, HospitalMasterDepartmentRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}
