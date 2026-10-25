using FluentValidation;
using Gateway.Services;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using Shared.Infrastructure.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Security;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Net;
using System.Threading.RateLimiting;

namespace Gateway.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures JWT Bearer authentication to validate tokens issued by IdentityService.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey is required");
        var previousSecretKey = jwtSettings["PreviousSecretKey"];
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer is required");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience is required");

        // Phase 7.1 — dual-key window for zero-downtime JWT signing key rotation.
        var signingKeys = new List<SecurityKey>
        {
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
        if (!string.IsNullOrWhiteSpace(previousSecretKey) &&
            !string.Equals(previousSecretKey, secretKey, StringComparison.Ordinal))
        {
            signingKeys.Add(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(previousSecretKey)));
        }

        services.AddMemoryCache();
        services.AddSingleton<IdentityTokenValidationClient>();

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
                IssuerSigningKey = signingKeys[0],
                IssuerSigningKeys = signingKeys,
                ClockSkew = TimeSpan.Zero // Strict token expiry validation
            };

            options.Events = new JwtBearerEvents
            {
                // SignalR WebSockets cannot set Authorization headers; token arrives as ?access_token=
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken)
                        && path.StartsWithSegments(Gateway.Hubs.NotificationHub.HubPath))
                    {
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Append("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                },
                OnTokenValidated = async context =>
                {
                    var principal = context.Principal;
                    if (principal == null)
                    {
                        context.Fail("Missing principal.");
                        return;
                    }

                    var userIdValue = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                    var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                    var tokenVersionValue = principal.FindFirst(AuthConstants.Claims.TokenVersion)?.Value;
                    var securityStamp = principal.FindFirst(AuthConstants.Claims.SecurityStamp)?.Value ?? string.Empty;

                    if (!Guid.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(jti))
                    {
                        context.Fail("Invalid token identity claims.");
                        return;
                    }

                    if (!int.TryParse(tokenVersionValue, out var tokenVersion))
                        tokenVersion = 0;

                    var validator = context.HttpContext.RequestServices.GetRequiredService<IdentityTokenValidationClient>();
                    var valid = await validator.ValidateAsync(
                        userId, jti, tokenVersion, securityStamp, context.HttpContext.RequestAborted);

                    if (!valid)
                        context.Fail("Token has been revoked or is no longer valid.");
                }
            };
        });

        return services;
    }

    /// <summary>
    /// Configures API versioning for the Gateway.
    /// </summary>
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

    /// <summary>
    /// Configures CORS for the Gateway. In Development, allows any origin (supports LAN devices
    /// on varying DHCP IPs — tablets, phones, second laptops). In non-Development environments,
    /// uses the strict allow-list from <c>CorsSettings:AllowedOrigins</c>.
    /// </summary>
    public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment env)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AngularApp", policy =>
            {
                if (env.IsDevelopment())
                {
                    // Dev LAN: accept any origin so tablets/phones on varying DHCP IPs work.
                    // AllowAnyOrigin() is incompatible with AllowCredentials(); SetIsOriginAllowed
                    // reflects the request Origin on the response and still permits credentials.
                    policy.SetIsOriginAllowed(_ => true)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()
                          .WithExposedHeaders("Token-Expired");
                }
                else
                {
                    var corsSettings = configuration.GetSection("CorsSettings");
                    var allowedOrigins = corsSettings.GetSection("AllowedOrigins").Get<string[]>()
                        ?? new[] { "http://localhost:4200", "https://localhost:4200" };

                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod() // This includes OPTIONS for preflight
                          // AllowAnyHeader: browsers send multipart as Content-Type: multipart/form-data; boundary=...
                          // — the boundary must be allowed on preflight (strict allow-lists that omit Content-Type break uploads).
                          // Also used: Authorization, api-version, X-Correlation-Id, X-Hospital-Id.
                          .AllowAnyHeader()
                          .AllowCredentials() // Required when using cookies or credentials
                          .WithExposedHeaders("Token-Expired"); // Expose custom headers to client
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Resolves Gateway → downstream HttpClient timeout: per-service override, then
    /// <c>Services:GlobalProxy:DefaultHttpClientTimeoutSeconds</c>, then 100 seconds. Minimum 1 second.
    /// </summary>
    private static int GetHttpClientTimeoutSeconds(string serviceName, IConfiguration configuration)
    {
        const int fallbackSeconds = 100;
        var perService = configuration[$"Services:{serviceName}:HttpClientTimeoutSeconds"];
        if (!string.IsNullOrWhiteSpace(perService)
            && int.TryParse(perService, out var per)
            && per >= 1)
        {
            return per;
        }

        var global = configuration["Services:GlobalProxy:DefaultHttpClientTimeoutSeconds"];
        if (!string.IsNullOrWhiteSpace(global)
            && int.TryParse(global, out var g)
            && g >= 1)
        {
            return g;
        }

        return fallbackSeconds;
    }

    /// <summary>
    /// Configures HttpClientFactory for backend service calls.
    /// </summary>
    public static IServiceCollection AddBackendHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        // ComplianceService HttpClient with Polly timeout + circuit breaker + retry
        services.AddHttpClient("ComplianceService", client =>
        {
            // Prefer configured Development URL (http://localhost:5016). Fallback matches CS https profile.
            var complianceBaseUrl = configuration["Services:ComplianceService:BaseUrl"] 
                ?? "http://localhost:5016";
            client.BaseAddress = new Uri(complianceBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("ComplianceService", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .AddHttpMessageHandler<InternalApiKeyForwardingHandler>()
        .AddBackendResiliencePolicies()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler
            {
                // Never follow HTTPS redirects: Authorization is stripped on redirect and becomes 401.
                AllowAutoRedirect = false
            };
            
            // Allow self-signed certificates in development
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") 
                ?? "Development";
            
            if (environment == "Development")
            {
                handler.ServerCertificateCustomValidationCallback = 
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            
            return handler;
        });

        // IdentityService HttpClient (for public endpoints: login, refresh).
        // Retries only on HttpRequestException (connection/DNS/TLS handshake) — not on HTTP status codes,
        // so failed refresh responses from Identity are not double-submitted. Revisit if refresh rotates tokens.
        services.AddHttpClient("IdentityService", client =>
        {
            var identityBaseUrl = configuration["Services:IdentityService:BaseUrl"] 
                ?? "http://localhost:5062";
            client.BaseAddress = new Uri(identityBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("IdentityService", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler
            {
                // Refresh/login POSTs must not lose body/auth across HTTPS redirects.
                AllowAutoRedirect = false
            };
            
            // Allow self-signed certificates in development
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") 
                ?? "Development";
            
            if (environment == "Development")
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }
            
            return handler;
        })
        .AddIdentityConnectionResiliencePolicies();

        // IdentityClient for internal endpoints
        services.AddHttpClient("IdentityClient", client =>
        {
            var identityBaseUrl = configuration["InternalServices:IdentityBaseUrl"] 
                ?? configuration["Services:IdentityService:BaseUrl"] 
                ?? "http://localhost:5062";
            client.BaseAddress = new Uri(identityBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("IdentityClient", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .AddHttpMessageHandler<InternalApiKeyForwardingHandler>()
        .AddBackendResiliencePolicies()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = false };
            
            // Allow self-signed certificates in development
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") 
                ?? "Development";
            
            if (environment == "Development")
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }
            
            return handler;
        });

        // OrganizationClient for internal endpoints — timeout + circuit breaker + retries.
        services.AddHttpClient("OrganizationClient", client =>
        {
            var orgBaseUrl = configuration["InternalServices:OrganizationBaseUrl"] 
                ?? "https://localhost:7181";
            client.BaseAddress = new Uri(orgBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("OrganizationClient", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .AddHttpMessageHandler<InternalApiKeyForwardingHandler>()
        .AddBackendResiliencePolicies()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            
            // Allow self-signed certificates in development
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") 
                ?? "Development";
            
            if (environment == "Development")
            {
                handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }
            
            return handler;
        });

        // AIService HttpClient
        services.AddHttpClient("AIService", client =>
        {
            var aiBaseUrl = configuration["Services:AIService:BaseUrl"] 
                ?? "http://localhost:5003";
            client.BaseAddress = new Uri(aiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("AIService", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>();

        // AuditService HttpClient
        services.AddHttpClient("AuditService", client =>
        {
            var auditBaseUrl = configuration["Services:AuditService:BaseUrl"] 
                ?? "http://localhost:5004";
            client.BaseAddress = new Uri(auditBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("AuditService", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>();

        // PersonService HttpClient
        services.AddHttpClient("PersonService", client =>
        {
            var personBaseUrl = configuration["Services:PersonService:BaseUrl"]
                ?? "https://localhost:7190";
            client.BaseAddress = new Uri(personBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("PersonService", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Development";
            if (environment == "Development")
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            return handler;
        });

        // WorkforceService HttpClient
        services.AddHttpClient("WorkforceService", client =>
        {
            var workforceBaseUrl = configuration["Services:WorkforceService:BaseUrl"]
                ?? "https://localhost:7195";
            client.BaseAddress = new Uri(workforceBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("WorkforceService", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Development";
            if (environment == "Development")
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            return handler;
        });

        // ProfessionalService HttpClient
        services.AddHttpClient("ProfessionalService", client =>
        {
            var professionalBaseUrl = configuration["Services:ProfessionalService:BaseUrl"]
                ?? "https://localhost:7200";
            client.BaseAddress = new Uri(professionalBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("ProfessionalService", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Development";
            if (environment == "Development")
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            return handler;
        });

        // AccessGovernanceService HttpClient
        services.AddHttpClient("AccessGovernanceService", client =>
        {
            var baseUrl = configuration["Services:AccessGovernanceService:BaseUrl"]
                ?? "https://localhost:7205";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("AccessGovernanceService", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Development";
            if (environment == "Development")
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            return handler;
        });

        // GovernanceService HttpClient
        services.AddHttpClient("GovernanceService", client =>
        {
            var baseUrl = configuration["Services:GovernanceService:BaseUrl"] ?? "https://localhost:7210";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("GovernanceService", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
            if (environment == "Development")
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });

        // CommunicationService HttpClient
        services.AddHttpClient("CommunicationService", client =>
        {
            var baseUrl = configuration["Services:CommunicationService:BaseUrl"] ?? "https://localhost:7215";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("CommunicationService", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
            if (environment == "Development")
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });

        // WorkforceIntelligenceService HttpClient
        services.AddHttpClient("WorkforceIntelligenceService", client =>
        {
            var baseUrl = configuration["Services:WorkforceIntelligenceService:BaseUrl"] ?? "https://localhost:7216";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("WorkforceIntelligenceService", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
            if (environment == "Development")
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });

        // EnterpriseDirectoryService HttpClient
        services.AddHttpClient("EnterpriseDirectoryService", client =>
        {
            var baseUrl = configuration["Services:EnterpriseDirectoryService:BaseUrl"] ?? "https://localhost:7217";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("EnterpriseDirectoryService", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
            if (environment == "Development")
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });

        // IntegrationService HttpClient
        services.AddHttpClient("IntegrationService", client =>
        {
            var baseUrl = configuration["Services:IntegrationService:BaseUrl"] ?? "https://localhost:7218";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("IntegrationService", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Development";
            if (environment == "Development")
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            return handler;
        });

        // KnowledgeBase Service HttpClient (includes AI/ask — allow longer timeout)
        services.AddHttpClient("KnowledgeBaseService", client =>
        {
            var kbBaseUrl = configuration["Services:KnowledgeBaseService:BaseUrl"]
                ?? "http://localhost:5120";
            client.BaseAddress = new Uri(kbBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(GetHttpClientTimeoutSeconds("KnowledgeBaseService", configuration));
        })
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Development";

            if (environment == "Development")
            {
                handler.ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }

            return handler;
        });

        return services;
    }

    /// <summary>
    /// Configures rate limiting for the Gateway.
    /// </summary>
    public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";
        var isDevelopment = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
        var permitLimit = configuration.GetValue("RateLimiting:GlobalPermitLimitPerMinute", isDevelopment ? 5000 : 100);

        services.AddRateLimiter(options =>
        {
            // 429 is the correct semantic for rate limiting; avoids masquerading as downstream outage (503).
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
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
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = permitLimit,
                        Window = TimeSpan.FromMinutes(1)
                    });
            });

            // Rate limiter for AI endpoints - per user
            options.AddPolicy("AIPolicy", context =>
            {
                // Use user ID from JWT token as partition key
                var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                    ?? context.Connection.RemoteIpAddress?.ToString() 
                    ?? "unknown";
                
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId,
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 10, // 10 requests per window
                        Window = TimeSpan.FromMinutes(1) // 1 minute window
                    });
            });

            // Rate limiter for risk recalculation - per organization
            // Prevents spam/abuse of expensive background job scheduling
            options.AddPolicy("RiskRecalculationPolicy", context =>
            {
                // Extract organization ID from route or JWT token
                var orgId = context.Request.RouteValues["orgId"]?.ToString() 
                    ?? context.User.FindFirst("organization_id")?.Value 
                    ?? context.Connection.RemoteIpAddress?.ToString() 
                    ?? "unknown";
                
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"risk-recalc-{orgId}",
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 2, // Maximum 2 recalculations per organization per window
                        Window = TimeSpan.FromMinutes(5) // 5 minute window
                    });
            });
        });

        return services;
    }

    /// <summary>
    /// Configures YARP reverse proxy.
    /// </summary>
    public static IServiceCollection AddReverseProxyConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));

        return services;
    }

    /// <summary>
    /// Registers the ProxyService for proxying requests to backend services.
    /// Also registers RegulatoryProxyService for regulatory/framework enrollment operations.
    /// </summary>
    public static IServiceCollection AddProxyService(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<Gateway.Services.IProxyService, Gateway.Services.ProxyService>();
        services.AddScoped<Gateway.Application.Handlers.IdentityUsersAdminOrchestrator>();
        services.AddScoped<Gateway.Application.Services.CommercialAdminGatewayService>();
        services.AddScoped<Gateway.Application.Services.InvitationRedeemOrchestrator>();
        services.AddScoped<Gateway.Services.IRegulatoryProxyService, Gateway.Services.RegulatoryProxyService>();
        services.AddSingleton<Gateway.Services.IAIFeaturePolicy, Gateway.Services.AIFeaturePolicy>();
        services.AddSingleton<Gateway.Services.IOnboardingWizardIdempotencyStore, Gateway.Services.OnboardingWizardIdempotencyStore>();

        return services;
    }

    /// <summary>
    /// Registers MediatR for CQRS pattern.
    /// </summary>
    public static IServiceCollection AddMediatR(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Gateway.Application.Queries.GetOrganizationDetailQuery).Assembly));
        return services;
    }

    /// <summary>
    /// Registers FluentValidation validators.
    /// </summary>
    public static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<Gateway.Application.Validators.GetOrganizationDetailQueryValidator>();
        return services;
    }

    /// <summary>
    /// Adds authorization policies for Gateway (TenantScope, ComplianceScope, and OrgAccess).
    /// </summary>
    public static IServiceCollection AddTenantAuthorization(this IServiceCollection services)
    {
        // Register the scope handler for space-separated scope claims
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, ScopeHandler>();
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Shared.Infrastructure.Auth.AnyScopeHandler>();
        
        // Register organization access handler for multi-tenant isolation
        services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, Authorization.OrganizationAccessHandler>();
        
        services.AddAuthorization(options =>
        {
            // TenantScope policy - requires "organization.api" scope
            options.AddPolicy("TenantScope", policy =>
            {
                // Use ScopeRequirement instead of RequireClaim to handle space-separated scopes
                policy.Requirements.Add(new ScopeRequirement(AuthConstants.Scopes.OrganizationApi));
            });

            // ComplianceScope policy - requires "compliance.api" scope
            options.AddPolicy(AuthConstants.Policies.ComplianceScope, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new ScopeRequirement(AuthConstants.Scopes.ComplianceApi));
            });

            // Onboarding wizard: allow either API scope (see docs/onboarding-wizard-rules.md auth section).
            options.AddPolicy(AuthConstants.Policies.ComplianceOrOrganizationScope, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new Shared.Infrastructure.Auth.AnyScopeRequirement(
                    AuthConstants.Scopes.ComplianceApi,
                    AuthConstants.Scopes.OrganizationApi));
            });

            // OrgAccess policy - ensures users can only access their own organization
            // SUPER_ADMIN can access any organization
            // Regular users must have organization_id claim matching route orgId
            options.AddPolicy("OrgAccess", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.Requirements.Add(new Authorization.OrganizationAccessRequirement());
            });

            // Phase 6 commercial / ops surfaces
            options.AddPolicy(AuthConstants.Policies.AdminOnly, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(
                    ApplicationRoles.SUPER_ADMIN,
                    ApplicationRoles.ORG_ADMIN,
                    ApplicationRoles.SYSTEM);
            });

            // CRP Phase 4 — Commercial Administration Console (AOVIAS staff only; excludes ORG_ADMIN)
            options.AddPolicy(AuthConstants.Policies.CommercialAdmin, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(
                    ApplicationRoles.SUPER_ADMIN,
                    ApplicationRoles.SYSTEM,
                    ApplicationRoles.COMMERCIAL_ADMIN,
                    ApplicationRoles.CSM,
                    ApplicationRoles.SUPPORT_ENGINEER,
                    ApplicationRoles.LICENSE_ADMIN,
                    ApplicationRoles.SALES_OPS,
                    ApplicationRoles.COMPLIANCE_OPS,
                    ApplicationRoles.COMMERCIAL_AUDITOR);
            });
        });

        return services;
    }

    /// <summary>
    /// Phase 7.5 — Polly.Extensions.Http stack for backend clients.
    /// Registration order: last handler is innermost (timeout → retry → circuit breaker).
    /// </summary>
    private static IHttpClientBuilder AddBackendResiliencePolicies(this IHttpClientBuilder builder)
    {
        return builder
            .AddPolicyHandler(GetBackendCircuitBreakerPolicy())
            .AddPolicyHandler(GetBackendServiceRetryPolicy())
            .AddPolicyHandler(GetBackendTimeoutPolicy());
    }

    /// <summary>
    /// Identity login/refresh: connection-only retries (no HTTP status retries) + timeout + circuit breaker.
    /// </summary>
    private static IHttpClientBuilder AddIdentityConnectionResiliencePolicies(this IHttpClientBuilder builder)
    {
        return builder
            .AddPolicyHandler(GetIdentityConnectionCircuitBreakerPolicy())
            .AddPolicyHandler(GetIdentityServiceConnectionRetryPolicy())
            .AddPolicyHandler(GetBackendTimeoutPolicy());
    }

    /// <summary>
    /// Per-attempt timeout for backend HttpClient calls (inner-most Polly policy).
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetBackendTimeoutPolicy()
    {
        return Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Opens the circuit after repeated transient failures to fail fast while the downstream recovers.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetBackendCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }

    private static IAsyncPolicy<HttpResponseMessage> GetIdentityConnectionCircuitBreakerPolicy()
    {
        return Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromSeconds(30));
    }

    /// <summary>
    /// Retries transient OrganizationService (and similar backend) failures: connection errors and 5xx/408.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetBackendServiceRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(100 * attempt));
    }

    private static bool IsRateLimitExemptPath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/liveness", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/readiness", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("/platform/", StringComparison.OrdinalIgnoreCase)
            || value.Contains("/setup", StringComparison.OrdinalIgnoreCase);
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

    /// <summary>
    /// Retries only connection-level failures to Identity (not HTTP 5xx responses), so POST /refresh is not duplicated on application errors.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetIdentityServiceConnectionRetryPolicy()
    {
        return Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(150 * retryAttempt));
    }

    /// <summary>
    /// Creates a retry policy for transient HTTP failures using Polly.
    /// Retries on 5xx status codes and network exceptions.
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // Handles HttpRequestException and 5xx/408 status codes
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.TooManyRequests) // Also retry on 429
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff: 2s, 4s, 8s
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // Log retry attempts (logger would need to be injected, but this is a static method)
                    // For now, we rely on Polly's built-in logging or application-level logging
                });
    }
}
