using Gateway.Configuration;
using Gateway.Extensions;
using Gateway.Middleware;
using Microsoft.AspNetCore.Http.Features;
using Shared.Logging.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using Shared.Infrastructure.Caching;
using Shared.Infrastructure.Email;
using Shared.Logging;
using Shared.Logging.Health;
using Shared.Logging.OpenTelemetry;
using Shared.Logging.Platform;
using Shared.Authorization;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

SerilogBootstrap.CreateBootstrapLogger("Gateway");

var builder = WebApplication.CreateBuilder(args);

// Optional runtime install config (written by setup wizard — never overwrite source appsettings).
var installationJson = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "aovias.installation.json");
if (File.Exists(installationJson))
{
    builder.Configuration.AddJsonFile(installationJson, optional: true, reloadOnChange: true);
}

// Phase 7.1 — Azure Key Vault configuration provider (no-op when VaultUri unset).
builder.AddAoviasKeyVault();
ProductionSecretsValidator.ValidateOrThrow(builder.Configuration, builder.Environment);

builder.ConfigureSerilog("Gateway");
builder.AddAoviasOpenTelemetry("Gateway");
builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.Gateway);
builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();

var redisConnection = RedisConfiguration.GetConnectionString(builder.Configuration);
if (redisConnection != null)
{
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
}

builder.Services.AddHttpClient();
builder.Services.AddSingleton<Gateway.Services.Installation.IInstallationStateService, Gateway.Services.Installation.InstallationStateService>();

const long EvidenceMultipartMaxBytes = 10_485_760;
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = EvidenceMultipartMaxBytes * 15);
builder.Services.Configure<KestrelServerOptions>(o => o.Limits.MaxRequestBodySize = EvidenceMultipartMaxBytes * 15);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
    });
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var traceId = context.HttpContext.TraceIdentifier;
        var errors = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .SelectMany(kvp => kvp.Value!.Errors.Select(e => new ApiError
            {
                Field = kvp.Key,
                Message = string.IsNullOrEmpty(e.ErrorMessage)
                    ? e.Exception?.Message ?? "Invalid value."
                    : e.ErrorMessage
            }))
            .ToList();
        if (errors.Count == 0)
        {
            errors.Add(new ApiError { Message = "Input validation failed." });
        }

        var envelope = ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, errors, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
        return new ObjectResult(envelope) { StatusCode = meta.HttpStatus };
    };
});
builder.Services.AddEndpointsApiExplorer();

// Configure services using extension methods
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddTransient<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();
builder.Services.AddPramanAuthorization(builder.Configuration);
builder.Services.Configure<Gateway.Configuration.OnboardingWizardOptions>(
    builder.Configuration.GetSection(Gateway.Configuration.OnboardingWizardOptions.SectionName));
builder.Services.Configure<Gateway.Configuration.WorkforceOnboardingOptions>(
    builder.Configuration.GetSection(Gateway.Configuration.WorkforceOnboardingOptions.SectionName));
builder.Services.Configure<Gateway.Configuration.TrialEnablementOptions>(
    builder.Configuration.GetSection(Gateway.Configuration.TrialEnablementOptions.SectionName));
builder.Services.Configure<Gateway.Configuration.OnboardingExperienceOptions>(
    builder.Configuration.GetSection(Gateway.Configuration.OnboardingExperienceOptions.SectionName));
builder.Services.Configure<Gateway.Configuration.TrialLifecycleGatewayOptions>(
    builder.Configuration.GetSection(Gateway.Configuration.TrialLifecycleGatewayOptions.SectionName));
builder.Services.Configure<Gateway.Configuration.LicensingFeatureGateOptions>(
    builder.Configuration.GetSection(Gateway.Configuration.LicensingFeatureGateOptions.SectionName));
builder.Services.Configure<Gateway.Configuration.AIFeatureFlagsOptions>(
    builder.Configuration.GetSection(Gateway.Configuration.AIFeatureFlagsOptions.SectionName));
builder.Services.AddConfigurableEmailService(builder.Configuration, builder.Environment);
builder.Services.AddTenantAuthorization();
builder.Services.AddApiVersioningSystem();
builder.Services.AddMediatR();
if (redisConnection != null)
{
    builder.Services.AddSignalR().AddStackExchangeRedis(redisConnection);
}
else
{
    builder.Services.AddSignalR();
}
builder.Services.AddFluentValidation();
builder.Services.AddCorsConfiguration(builder.Configuration, builder.Environment);
builder.Services.AddBackendHttpClients(builder.Configuration);
builder.Services.AddTransient<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();
builder.Services.AddProxyService();
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddReverseProxyConfiguration(builder.Configuration);
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

var identityUrl = builder.Configuration["Services:IdentityService:BaseUrl"] ?? "https://localhost:7135";
if (!Uri.TryCreate(identityUrl, UriKind.Absolute, out _))
{
    throw new InvalidOperationException("Services:IdentityService:BaseUrl must be a valid absolute URI.");
}

var app = builder.Build();

// Must run before any middleware or MVC component can read the body; otherwise multipart proxy sees 0 bytes.
app.UseMiddleware<MultipartRequestBufferingMiddleware>();

// Correlation + structured request logging (early, so every log line is enriched).
app.UsePramanLogging();
app.UsePramanLifecycleLogging("Gateway");

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
// Development: no HTTPS redirect — LAN clients reach Gateway on http://0.0.0.0:5000
// and must not be 307'd to https://<LAN_IP>:7216 (cert bound to localhost only).

app.UseRouting();
app.UseMiddleware<SecurityHeadersMiddleware>();
// Phase 7.6 — early gate so API traffic is blocked while health/platform/ops remain reachable.
app.UseMiddleware<Gateway.Middleware.MaintenanceModeMiddleware>();
app.UseResponseCompression();
app.UseDashboardRequestTiming();

// Global exception handling for Gateway-specific endpoints
app.UseMiddleware<ExceptionHandlingMiddleware>();

// CORS must be AFTER UseRouting but BEFORE UseAuthentication/UseAuthorization
app.UseCors("AngularApp");

// Authentication and Authorization middleware (must be before MapReverseProxy)
app.UseAuthentication();
app.UseMiddleware<Gateway.Middleware.TenantIsolationMiddleware>();
app.UseMiddleware<Gateway.Middleware.TrialAccessMiddleware>();
app.UseMiddleware<Gateway.Middleware.LicenseEntitlementMiddleware>();
app.UseAuthorization();

// Organization access failure middleware (must be after UseAuthorization to catch 403 responses)
app.UseMiddleware<OrganizationAccessFailureMiddleware>();

// Idempotency middleware (must be after UseAuthorization to access route values)
app.UseMiddleware<IdempotencyMiddleware>();

// Rate limiting (must be after UseAuthorization)
app.UseRateLimiter();

// Map reverse proxy
app.MapReverseProxy();

// Map controllers (for health checks or gateway-specific endpoints)
app.MapControllers();
app.MapHub<Gateway.Hubs.NotificationHub>(Gateway.Hubs.NotificationHub.HubPath);
app.UsePramanObservability();
app.MapPramanAoviasPlatform();

app.Run();
