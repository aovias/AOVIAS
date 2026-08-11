using AIOrchestratorService.Application.Context;
using AIOrchestratorService.Application.WorkflowEngine;
using AIOrchestratorService.Extensions;
using AIOrchestratorService.Infrastructure.Clients;
using AIOrchestratorService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Extensions;
using Shared.Logging;
using Shared.Logging.Health;
using Shared.Logging.Platform;
using Shared.Authorization;
using Shared.Infrastructure.Auth;

SerilogBootstrap.CreateBootstrapLogger("AIOrchestratorService");

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog("AIOrchestratorService");
builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.AiOrchestrator);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AIOrchestratorDbContext>("database");

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
// Swagger UI temporarily disabled - Swashbuckle.AspNetCore is not compatible with .NET 10
// OpenAPI JSON is still available via Microsoft.AspNetCore.OpenApi at /openapi/v1.json
// TODO: Re-enable Swagger UI once Swashbuckle.AspNetCore supports .NET 10
// builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<IRequestContextBuilder, RequestContextBuilder>();

// Register HttpClient "InternalClient" with ForwardAuthHeaderHandler
var complianceServiceUrl = builder.Configuration["Services:ComplianceService:BaseUrl"] 
    ?? "https://localhost:7087";

builder.Services.AddHttpClient("InternalClient", client =>
{
    client.BaseAddress = new Uri(complianceServiceUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<ForwardAuthHeaderHandler>()
.AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>();

builder.Services.AddScoped<ForwardAuthHeaderHandler>();

builder.Services.AddIntentDetection();

builder.Services.AddWorkflowEngine();

builder.Services.AddInternalClients(builder.Configuration);

builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddLLMAdapter();
builder.Services.AddAISystem(builder.Configuration);

builder.Services.AddDraftGeneration();

builder.Services.AddAuthenticationSystem(builder.Configuration);
builder.Services.AddInternalApiAuthorization();
builder.Services.AddPramanAuthorization(builder.Configuration);

builder.Services.AddApiVersioningSystem();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Serialize enums as their PascalCase names (e.g. "OrganizationSpecific", "Critical",
        // "ComplianceScore") so the WebApp can switch on string values for answer mode,
        // severity, and insight type.
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

var app = builder.Build();

// Correlation + request logging + global exception handling (early).
app.UsePramanLogging();
app.UsePramanLifecycleLogging("AIOrchestratorService");
app.UseGlobalExceptionHandling();

try
{
    using var migrationScope = app.Services.CreateScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<AIOrchestratorDbContext>();
    await db.Database.MigrateAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogError(ex, "Failed to apply AIOrchestrator database migrations on startup. The service will start in degraded mode until the database is available.");
}

// Register workflows in the registry
using (var scope = app.Services.CreateScope())
{
    var registry = scope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();
    var startComplianceWorkflow = scope.ServiceProvider.GetRequiredService<StartComplianceWorkflow>();
    registry.Register("StartCompliance", startComplianceWorkflow);
}

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    // Swagger UI temporarily disabled - Swashbuckle.AspNetCore is not compatible with .NET 10
    // OpenAPI JSON is still available via Microsoft.AspNetCore.OpenApi at /openapi/v1.json
    // TODO: Re-enable Swagger UI once Swashbuckle.AspNetCore supports .NET 10
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();
app.UsePramanObservability();
app.MapPramanAoviasPlatform();

app.Run();
