// ============================================================================
// EXAMPLE: Program.cs Configuration for Standardized API Responses
// ============================================================================
// Add these configurations to your service's Program.cs file
// ============================================================================

using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ... existing service registrations ...

// Add FluentValidation (if not already added)
// builder.Services.AddFluentValidation(fv => fv.RegisterValidatorsFromAssemblyContaining<YourValidator>());

var app = builder.Build();

// ============================================================================
// MIDDLEWARE CONFIGURATION (Order is important!)
// ============================================================================

// 1. Correlation ID - should be early in pipeline
app.UseCorrelationId();

// 2. Standard ASP.NET Core middleware
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("YourPolicy");

// 3. Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// 4. Exception handling - MUST be after UseRouting but before UseEndpoints/MapControllers
app.UseGlobalExceptionHandling();
app.UseValidationExceptionHandling();

// 5. Endpoints
app.MapControllers();

app.Run();

// ============================================================================
// COMPLETE EXAMPLE FOR ComplianceService
// ============================================================================

/*
using Compliance.MasterData.Contracts;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Services;
using ComplianceService.Infrastructure.Extensions;
using ComplianceService.Infrastructure.Filters;
using ComplianceService.Infrastructure.Persistence;
using ComplianceService.Infrastructure.Repositories;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Extensions;  // <-- Add this
using Shared.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddMasterData();
builder.Services.AddSecurityServices();
builder.Services.AddJwtValidation(builder.Configuration);
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddHangfireConfiguration(builder.Configuration);
builder.Services.AddApiVersioningSystem();

// Register services...
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IUserContextService, ComplianceService.Infrastructure.Services.UserContextService>();
builder.Services.AddScoped<IRegulationRepository, RegulationRepository>();
// ... other services ...

// Snapshot Service
builder.Services.AddScoped<ISnapshotService, SnapshotService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("AngularApp");
app.UseRateLimiter();

// ============================================================================
// ADD STANDARDIZED API RESPONSE MIDDLEWARE
// ============================================================================
app.UseCorrelationId();  // <-- Add this
app.UseAuthentication();
app.UseAuthorization();

// Exception handling middleware (after auth, before endpoints)
app.UseGlobalExceptionHandling();  // <-- Add this
app.UseValidationExceptionHandling();  // <-- Add this

// Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() },
    DashboardTitle = "Compliance Service - Background Jobs",
    StatsPollingInterval = 2000
});

app.MapControllers();
app.Run();
*/

// ============================================================================
// COMPLETE EXAMPLE FOR Gateway
// ============================================================================

/*
using Gateway.Extensions;
using Shared.Infrastructure.Extensions;  // <-- Add this

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddTenantAuthorization();
builder.Services.AddApiVersioningSystem();
builder.Services.AddMediatR();
builder.Services.AddFluentValidation();
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddBackendHttpClients(builder.Configuration);
builder.Services.AddProxyService();
builder.Services.AddRateLimiting(builder.Configuration);
builder.Services.AddReverseProxyConfiguration(builder.Configuration);

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
else
{
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseCors("AngularApp");

// ============================================================================
// ADD STANDARDIZED API RESPONSE MIDDLEWARE
// ============================================================================
app.UseCorrelationId();  // <-- Add this
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// Exception handling middleware
app.UseGlobalExceptionHandling();  // <-- Add this
app.UseValidationExceptionHandling();  // <-- Add this

app.MapReverseProxy();
app.MapControllers();
app.Run();
*/
