using Microsoft.EntityFrameworkCore;
using WorkforceIntelligenceService.Extensions;
using WorkforceIntelligenceService.Infrastructure.Persistence;
using Shared.Infrastructure.Extensions;
using Shared.Logging;
using Shared.Logging.Health;
using Shared.Logging.Platform;

SerilogBootstrap.CreateBootstrapLogger("WorkforceIntelligenceService");

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog("WorkforceIntelligenceService");
builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.WorkforceIntelligence);
builder.Services.AddHealthChecks().AddDbContextCheck<WorkforceIntelligenceDbContext>("database");
builder.Services.AddWorkforceIntelligenceDatabase(builder.Configuration);
builder.Services.AddWorkforceIntelligenceAuthentication(builder.Configuration);
builder.Services.AddWorkforceIntelligenceApiVersioning();
builder.Services.AddWorkforceIntelligenceApplication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();
app.UsePramanLogging();
app.UsePramanLifecycleLogging("WorkforceIntelligenceService");
app.UseGlobalExceptionHandling();

try
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<WorkforceIntelligenceDbContext>().Database.MigrateAsync();
}
catch (Exception ex)
{
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup").LogError(ex, "Failed to apply WorkforceIntelligenceService migrations.");
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UsePramanObservability();
app.MapPramanAoviasPlatform();
app.Run();

public partial class Program;
