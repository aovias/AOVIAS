using IdentityService.Configuration;
using IdentityService.Domain;
using IdentityService.Extensions;
using IdentityService.Infrastructure;
using IdentityService.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Auth;
using Shared.Infrastructure.Email;
using Shared.Logging;
using Shared.Logging.Health;
using Shared.Logging.Platform;
using System;

SerilogBootstrap.CreateBootstrapLogger("IdentityService");

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog("IdentityService");

try
{
    StartupConfigurationValidation.Validate(builder.Configuration, builder.Environment);
}
catch (SigningCredentialValidationException exception)
{
    Console.Error.WriteLine(exception.Message);
    return;
}

builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.Identity);
builder.Services.AddInternalApiAuthorization();
builder.Services.AddTransient<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<IdentityDbContext>("database");

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddIdentitySystem();
builder.Services.AddIdentityServerSystem(builder.Configuration, builder.Environment); // Keep for OAuth2/OIDC if needed
builder.Services.AddEmailSystem(builder.Configuration, builder.Environment);
builder.Services.AddPlatformIdGenerator();
builder.Services.AddIdentityApplication(builder.Configuration);

// JWT Authentication Services
builder.Services.AddJwtServices();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddRateLimiting(builder.Configuration);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<IdentityService.Filters.GlobalExceptionFilter>();
});
builder.Services.AddEndpointsApiExplorer();
// Swagger UI temporarily disabled - Swashbuckle.AspNetCore is not compatible with .NET 10
// OpenAPI JSON is still available via Microsoft.AspNetCore.OpenApi at /openapi/v1.json
// TODO: Re-enable Swagger UI once Swashbuckle.AspNetCore supports .NET 10
// builder.Services.AddSwaggerGen();

builder.Services.AddRazorPages();

builder.Services.Configure<HostOptions>(options =>
{
    options.ServicesStartConcurrently = true;
});

// Role migration deferred to ApplicationStarted inside CanonicalRoleMigrationHostedService.
builder.Services.AddHostedService<IdentityService.Infrastructure.CanonicalRoleMigrationHostedService>();

var app = builder.Build();

// Correlation + structured request logging must run first.
app.UsePramanLogging();
app.UsePramanLifecycleLogging("IdentityService");

// Apply database migrations in Development (run `dotnet ef database update` if schema is behind)
if (app.Environment.IsDevelopment())
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Skipping automatic migrations in Development; run dotnet ef database update when schema changes.");
}

// Gateway Development BaseUrls use http://localhost:5062. HTTPS redirection here causes
// HttpClient to follow 307 to :7135 and drop auth/body semantics — refresh then fails (503/401 cascade).
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

// CORS for Angular frontend
app.UseCors(policy =>
{
    policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
          .AllowAnyMethod()
          .AllowAnyHeader()
          .AllowCredentials();
});

// Rate limiting middleware (must be before UseRateLimiter)
app.UseMiddleware<IdentityService.Middleware.RateLimitMiddleware>();

// Rate limiting
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    // Swagger UI temporarily disabled - Swashbuckle.AspNetCore is not compatible with .NET 10
    // OpenAPI JSON is still available via Microsoft.AspNetCore.OpenApi at /openapi/v1.json
    // TODO: Re-enable Swagger UI once Swashbuckle.AspNetCore supports .NET 10
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseIdentityServer(); // Keep for OAuth2/OIDC if needed
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.UsePramanObservability();
app.MapPramanAoviasPlatform();

app.Run();
