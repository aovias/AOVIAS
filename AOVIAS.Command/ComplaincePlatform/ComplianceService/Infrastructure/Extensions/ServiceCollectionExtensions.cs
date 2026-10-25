using Compliance.MasterData.Contracts;
using Compliance.MasterData.Providers;
using Compliance.MasterData.Seeders.NABH;
using ComplianceService.Application.Journey;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Seeders;
using ComplianceService.Application.Services;
using ComplianceService.Infrastructure.Persistence;
using ComplianceService.Infrastructure.Persistence.Interceptors;
using ComplianceService.Infrastructure.Storage;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Threading.RateLimiting;
using System.Text;

namespace ComplianceService.Infrastructure.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures JWT Bearer authentication to validate tokens issued by IdentityService.
        /// This service does NOT generate tokens - it only validates them.
        /// </summary>
        public static IServiceCollection AddJwtValidation(this IServiceCollection services, IConfiguration configuration)
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
                            context.Response.Headers.Add("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        // Check if token is blacklisted (for logout functionality)
                        var tokenBlacklistService = context.HttpContext.RequestServices
                            .GetService<Infrastructure.Services.ITokenBlacklistService>();
                        
                        if (tokenBlacklistService != null)
                        {
                            try
                            {
                                var jti = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
                                if (!string.IsNullOrEmpty(jti))
                                {
                                    var isBlacklisted = await tokenBlacklistService.IsTokenBlacklistedAsync(jti).ConfigureAwait(false);
                                    if (isBlacklisted)
                                    {
                                        context.Fail("Token has been revoked");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                // Log but don't fail authentication if blacklist check fails
                                var logger = context.HttpContext.RequestServices
                                    .GetService<ILogger<JwtBearerEvents>>();
                                logger?.LogWarning(ex, "Error checking token blacklist");
                            }
                        }
                    }
                };
            });

            // Configure authorization policies
            services.AddAuthorization(options =>
            {
                // ComplianceScope policy - requires "compliance.api" scope
                options.AddPolicy(Shared.Infrastructure.Auth.AuthConstants.Policies.ComplianceScope, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.AddRequirements(new Shared.Infrastructure.Auth.ScopeRequirement(Shared.Infrastructure.Auth.AuthConstants.Scopes.ComplianceApi));
                });

                // InternalOnly policy - requires API key header for internal automation endpoints
                // Used by background jobs (Hangfire) to call internal lifecycle automation endpoints
                options.AddPolicy("InternalOnly", policy =>
                {
                    policy.RequireAssertion(context =>
                    {
                        // Get HttpContext from AuthorizationHandlerContext
                        if (context.Resource is not HttpContext httpContext)
                        {
                            return false;
                        }

                        var request = httpContext.Request;
                        var providedKey = request.Headers["X-Internal-Api-Key"].FirstOrDefault();
                        
                        // Get expected API key from configuration
                        var configuration = httpContext.RequestServices.GetRequiredService<IConfiguration>();
                        var expectedKey = configuration["InternalApi:ApiKey"];

                        // If no API key is configured, allow only localhost (for development)
                        if (string.IsNullOrEmpty(expectedKey))
                        {
                            var isLocalhost = request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                                            request.Host.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                                            request.Host.Host.StartsWith("::1", StringComparison.OrdinalIgnoreCase);
                            return isLocalhost;
                        }

                        // Validate API key
                        return !string.IsNullOrEmpty(providedKey) && 
                               providedKey.Equals(expectedKey, StringComparison.Ordinal);
                    });
                });
            });

            // Register scope handler for authorization
            services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Shared.Infrastructure.Auth.ScopeHandler>();

            return services;
        }

        public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            // Add connection timeout to prevent indefinite hanging
            if (!string.IsNullOrEmpty(connectionString) && !connectionString.Contains("Connection Timeout", StringComparison.OrdinalIgnoreCase))
            {
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
                builder.ConnectTimeout = 30; // 30 seconds connection timeout
                connectionString = builder.ConnectionString;
            }
            
            services.AddMemoryCache();
            services.AddScoped<QueryCountingDbCommandInterceptor>();
            services.AddScoped<FrameworkCatalogInvalidationInterceptor>();
            services.AddScoped<IRequestContextCache, RequestContextCache>();
            services.AddSingleton<IFrameworkCatalogCache, FrameworkCatalogMemoryCache>();

            services.AddDbContext<ComplianceDbContext>((serviceProvider, options) =>
                options
                    .UseSqlServer(connectionString, sqlOptions =>
                    {
                        sqlOptions.CommandTimeout(120); // 2 minutes command timeout
                        sqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null);
                    })
                    // Allow MigrateAsync to apply existing migrations when the snapshot lags the model
                    // (common during iterative sprint work). Prefer adding a sync migration when practical.
                    .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning))
                    .AddInterceptors(
                        serviceProvider.GetRequiredService<QueryCountingDbCommandInterceptor>(),
                        serviceProvider.GetRequiredService<FrameworkCatalogInvalidationInterceptor>(),
                        Shared.Logging.Diagnostics.LoggingDbCommandInterceptor.Instance));

            services.AddScoped<IPlatformSequenceStore, EfComplianceSequenceStore>();
            services.AddScoped<IPlatformIdGenerator, PlatformIdGenerator>();
            services.AddScoped<IEvidenceService, EvidenceService>();
            services.AddScoped<IEvidenceRegistrationService, EvidenceRegistrationService>();
            services.AddScoped<IEvidenceReviewService, EvidenceReviewService>();
            services.AddScoped<IEvidenceLibraryService, EvidenceLibraryService>();
            var storageProvider = configuration["Storage:Provider"] ?? "Local";
            if (string.Equals(storageProvider, "AzureBlob", StringComparison.OrdinalIgnoreCase))
            {
                services.AddScoped<IFileStorageService, AzureBlobFileStorageService>();
            }
            else
            {
                services.AddScoped<IFileStorageService, LocalFileStorageService>();
            }
            services.AddScoped<ComplianceService.Application.EvidenceReview.IEvidenceVirusScanProvider,
                ComplianceService.Application.EvidenceReview.NoOpEvidenceVirusScanProvider>();

            return services;
        }

        public static IServiceCollection AddMasterData(this IServiceCollection services)
        {
            // Master data infrastructure
            services.AddScoped<IMasterDataPersistence, EfMasterDataPersistence>();
            services.AddScoped<IFrameworkCatalogReplicationService, FrameworkCatalogReplicationService>();

            // Seeders (explicit order: Entry Level first, then Full 6E catalog replication)
            services.AddScoped<IMasterDataSeeder, NabhEntryLevelSeeder>();
            services.AddScoped<IMasterDataSeeder, NabhFull6eCatalogReplicationSeeder>();

            // Registry & runner
            services.AddScoped<IMasterDataSeederRegistry, DefaultMasterDataSeederRegistryService>();
            services.AddScoped<MasterDataSeederRunner>();
            services.AddScoped<Scripts.SeedSampleSnapshotData>();
           
            services.AddScoped<IMasterDataProvider>(sp =>
            {
                var env = sp.GetRequiredService<IWebHostEnvironment>();

                var basePath = Path.Combine(
      Directory.GetParent(env.ContentRootPath)!.FullName,
      "Compliance.MasterData",
      "Data",
      "NABH",
      "EntryLevel");


                return new FileBasedMasterDataProvider(
                    Path.Combine(basePath, "manifest.json"),
                    Path.Combine(basePath, "nabh-entry-2024.json")
                );
            });

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

        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var corsSettings = configuration.GetSection("CorsSettings");
            var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>() 
                ?? new[] { "http://localhost:4200", "https://localhost:4200" };

            services.AddCors(options =>
            {
                options.AddPolicy("AngularApp", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod() // This includes OPTIONS for preflight
                          // AllowAnyHeader: required for multipart/form-data; boundary=... and SPA headers (Authorization, api-version, X-Correlation-Id, X-Hospital-Id).
                          .AllowAnyHeader()
                          .AllowCredentials() // Required when using cookies or credentials
                          .WithExposedHeaders("Token-Expired"); // Expose custom headers to client
                });
            });

            return services;
        }

        public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Production";
            var isDevelopment = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);

            // Configure rate limiting using .NET built-in rate limiting
            services.AddRateLimiter(options =>
            {
                // 429 is the correct semantic for rate limiting; avoids masquerading as downstream outage (503).
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // Global rate limiter
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                {
                    // Infrastructure probes must answer even once the budget is spent. Throttling them
                    // made a healthy service report itself unavailable to its own orchestrator: 636
                    // /health requests were rejected while the service was demonstrably serving.
                    if (IsRateLimitExemptPath(context.Request.Path))
                    {
                        return RateLimitPartition.GetNoLimiter("health-probes");
                    }

                    // Development/QA: Playwright and ng serve proxy funnel all traffic through loopback.
                    if (isDevelopment && IsLoopbackClient(context.Connection.RemoteIpAddress))
                    {
                        return RateLimitPartition.GetNoLimiter("dev-loopback");
                    }

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        });
                });

                // Specific rate limiter for login endpoint
                options.AddPolicy("LoginPolicy", context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 5, // 5 attempts per minute
                            Window = TimeSpan.FromMinutes(1)
                        }));
            });

            return services;
        }

        /// <summary>
        /// Infrastructure probe paths mapped by MapPramanAoviasPlatform: /health, /health/ready,
        /// /liveness, /readiness and /platform/registration. These belong to the orchestrator, not
        /// to a client, so they are never counted against a client's request budget.
        /// </summary>
        private static bool IsRateLimitExemptPath(PathString path)
        {
            var value = path.Value ?? string.Empty;
            return value.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/liveness", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/readiness", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("/platform/", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLoopbackClient(IPAddress? address)
        {
            if (address is null)
            {
                return false;
            }

            if (IPAddress.IsLoopback(address))
            {
                return true;
            }

            return address.IsIPv4MappedToIPv6 && IPAddress.IsLoopback(address.MapToIPv4());
        }

        public static IServiceCollection AddSecurityServices(this IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddScoped<Infrastructure.Services.ITokenBlacklistService, Infrastructure.Services.TokenBlacklistService>();
            return services;
        }

        /// <summary>
        /// Configures Hangfire for background job processing with SQL Server storage.
        /// </summary>
        public static IServiceCollection AddHangfireConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' is required for Hangfire.");
            }

            // Add Hangfire SQL storage. The processing server is optional so a verification host
            // can share the database without a second Hangfire worker (Hangfire:DisableServer).
            services.AddHangfire(config =>
            {
                config.UseSqlServerStorage(connectionString, new Hangfire.SqlServer.SqlServerStorageOptions
                {
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                });
            });

            if (!configuration.GetValue("Hangfire:DisableServer", false))
            {
                services.AddHangfireServer(options =>
                {
                    options.WorkerCount = Environment.ProcessorCount * 5;
                    options.ServerTimeout = TimeSpan.FromMinutes(4);
                    options.SchedulePollingInterval = TimeSpan.FromSeconds(15);
                });
            }

            return services;
        }
    }
}
