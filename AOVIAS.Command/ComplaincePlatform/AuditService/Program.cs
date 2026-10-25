using AuditService.Extensions;
using AuditService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Extensions;
using Shared.Logging;
using Shared.Logging.Health;
using Shared.Logging.Platform;

SerilogBootstrap.CreateBootstrapLogger("AuditService");

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog("AuditService");
builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.Audit);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AuditDbContext>("database");

// Add services to the container.

builder.Services.AddEndpointsApiExplorer();
// Swagger UI temporarily disabled - Swashbuckle.AspNetCore is not compatible with .NET 10
// OpenAPI JSON is still available via Microsoft.AspNetCore.OpenApi at /openapi/v1.json
// TODO: Re-enable Swagger UI once Swashbuckle.AspNetCore supports .NET 10
// builder.Services.AddSwaggerGen();

builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddAuthenticationSystem();

builder.Services.AddApiVersioningSystem();

builder.Services.AddApplicationServices();

builder.Services.AddInfrastructureServices();

builder.Services.AddControllers();

var app = builder.Build();

// Correlation + request logging + global exception handling (early).
app.UsePramanLogging();
app.UsePramanLifecycleLogging("AuditService");
app.UseGlobalExceptionHandling();

try
{
    using var migrationScope = app.Services.CreateScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<AuditDbContext>();
    await db.Database.MigrateAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogError(ex, "Failed to apply AuditService database migrations on startup. The service will start in degraded mode until the database is available.");
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
