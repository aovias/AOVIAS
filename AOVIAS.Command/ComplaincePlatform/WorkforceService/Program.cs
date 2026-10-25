using Microsoft.EntityFrameworkCore;
using WorkforceService.Extensions;
using WorkforceService.Infrastructure.Persistence;
using Shared.Infrastructure.Extensions;
using Shared.Logging;
using Shared.Logging.Health;
using Shared.Logging.Platform;

SerilogBootstrap.CreateBootstrapLogger("WorkforceService");

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog("WorkforceService");
builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.Workforce);
builder.Services.AddHealthChecks().AddDbContextCheck<WorkforceDbContext>("database");

builder.Services.AddWorkforceDatabase(builder.Configuration);
builder.Services.AddWorkforceAuthentication(builder.Configuration);
builder.Services.AddWorkforceApiVersioning();
builder.Services.AddWorkforceApplication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UsePramanLogging();
app.UsePramanLifecycleLogging("WorkforceService");
app.UseGlobalExceptionHandling();

try
{
    using var migrationScope = app.Services.CreateScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<WorkforceDbContext>();
    var logger = migrationScope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    try
    {
        await db.Database.MigrateAsync();
    }
    catch when (app.Environment.IsDevelopment())
    {
        await db.Database.EnsureCreatedAsync();
    }

    logger.LogInformation("WorkforceService database is ready.");
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogError(ex, "Failed to apply WorkforceService database migrations on startup.");
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UsePramanObservability();
app.MapPramanAoviasPlatform();

app.Run();
