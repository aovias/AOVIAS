using IdentityService.Config;
using IdentityService.Configuration;
using IdentityService.Domain;
using IdentityService.Infrastructure;
using IdentityService.Infrastructure.Persistence;
using IdentityService.Services;
using IdentityService.Application.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.Email;
using Shared.Infrastructure.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System;

namespace IdentityService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddDatabase(
            this IServiceCollection services,
            IConfiguration config)
        {
            services.AddDbContext<IdentityDbContext>(options =>
                options
                    .UseSqlServer(
                        config.GetConnectionString("DefaultConnection"),
                        sql => sql.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(30),
                            errorNumbersToAdd: null))
                    .AddInterceptors(Shared.Logging.Diagnostics.LoggingDbCommandInterceptor.Instance));

            return services;
        }

        public static IServiceCollection AddIdentitySystem(this IServiceCollection services)
        {
        services.AddIdentity<AppUser, IdentityRole<Guid>>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<IdentityDbContext>()
        .AddDefaultTokenProviders();

        services.AddSingleton<Microsoft.Extensions.Options.IConfigureOptions<IdentityOptions>, Infrastructure.Configuration.IdentityOptionsPolicyConfigurator>();

            return services;
        }

        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey is required");
            var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JwtSettings:Issuer is required");
            var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JwtSettings:Audience is required");

            services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = issuer,
                        ValidAudience = audience,
                        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                            System.Text.Encoding.UTF8.GetBytes(secretKey)),
                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                    {
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

                            using var scope = context.HttpContext.RequestServices.CreateScope();
                            var validator = scope.ServiceProvider.GetRequiredService<ITokenValidationService>();
                            var valid = await validator.ValidateAsync(
                                userId, jti, tokenVersion, securityStamp, context.HttpContext.RequestAborted);

                            if (!valid)
                                context.Fail("Token has been revoked or is no longer valid.");
                        }
                    };
                });

            return services;
        }

        public static IServiceCollection AddJwtServices(this IServiceCollection services)
        {
            return services;
        }

        public static IServiceCollection AddRateLimiting(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddMemoryCache();
            
            services.AddRateLimiter(options =>
            {
                // Default RejectionStatusCode is 503; use 429 so clients treat this as rate limiting.
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<Microsoft.AspNetCore.Http.HttpContext, string>(context =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1)
                        }));

                // Shared by login + refresh; keep headroom for post-login token refresh bursts.
                options.AddPolicy("LoginPolicy", context =>
                    System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                        factory: partition => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1)
                        }));
            });

            return services;
        }

        public static IServiceCollection AddIdentityServerSystem(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            var identityServerBuilder = services.AddIdentityServer(options =>
                {
                    options.KeyManagement.Enabled = false;
                })
                .AddInMemoryIdentityResources(IdentityConfig.IdentityResources)
                .AddInMemoryApiScopes(IdentityConfig.ApiScopes)
                .AddInMemoryApiResources(IdentityConfig.ApiResources)
                .AddInMemoryClients(IdentityConfig.Clients)
                .AddAspNetIdentity<AppUser>();

            IdentitySigningCredentialConfigurer.Configure(identityServerBuilder, configuration, environment);

            return services;
        }

        public static IServiceCollection AddEmailSystem(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddConfigurableEmailService(configuration, environment);
            services.AddScoped<IEmailSender, IdentityEmailSender>();

            return services;
        }

        public static IServiceCollection AddPlatformIdGenerator(this IServiceCollection services)
        {
            services.AddScoped<IPlatformSequenceStore, EfIdentitySequenceStore>();
            services.AddScoped<IPlatformIdGenerator, PlatformIdGenerator>();

            return services;
        }
    }
}