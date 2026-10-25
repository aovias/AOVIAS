using GovernanceService.Extensions;
using GovernanceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Extensions;
using Shared.Logging;
using Shared.Logging.Health;
using Shared.Logging.Platform;

SerilogBootstrap.CreateBootstrapLogger("GovernanceService");

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog("GovernanceService");
builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.Governance);
builder.Services.AddHealthChecks().AddDbContextCheck<GovernanceDbContext>("database");

builder.Services.AddGovernanceDatabase(builder.Configuration);
builder.Services.AddGovernanceAuthentication(builder.Configuration);
builder.Services.AddGovernanceApiVersioning();
builder.Services.AddGovernanceApplication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UsePramanLogging();
app.UsePramanLifecycleLogging("GovernanceService");
app.UseGlobalExceptionHandling();

try
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<GovernanceDbContext>().Database.MigrateAsync();
}
catch (Exception ex)
{
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup")
        .LogError(ex, "Failed to apply GovernanceService migrations.");
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UsePramanObservability();
app.MapPramanAoviasPlatform();

app.Run();
