using AccessGovernanceService.Extensions;
using AccessGovernanceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Extensions;
using Shared.Logging;
using Shared.Logging.Health;
using Shared.Logging.Platform;

SerilogBootstrap.CreateBootstrapLogger("AccessGovernanceService");

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog("AccessGovernanceService");
builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.AccessGovernance);
builder.Services.AddHealthChecks().AddDbContextCheck<AccessGovernanceDbContext>("database");

builder.Services.AddAccessGovernanceDatabase(builder.Configuration);
builder.Services.AddAccessGovernanceAuthentication(builder.Configuration);
builder.Services.AddAccessGovernanceApiVersioning();
builder.Services.AddAccessGovernanceApplication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UsePramanLogging();
app.UsePramanLifecycleLogging("AccessGovernanceService");
app.UseGlobalExceptionHandling();

try
{
    using var migrationScope = app.Services.CreateScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<AccessGovernanceDbContext>();
    await db.Database.MigrateAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogError(ex, "Failed to apply AccessGovernanceService database migrations on startup.");
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UsePramanObservability();
app.MapPramanAoviasPlatform();

app.Run();
