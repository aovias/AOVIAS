using Microsoft.EntityFrameworkCore;
using ProfessionalService.Extensions;
using ProfessionalService.Infrastructure.Persistence;
using Shared.Infrastructure.Extensions;
using Shared.Logging;
using Shared.Logging.Health;
using Shared.Logging.Platform;

SerilogBootstrap.CreateBootstrapLogger("ProfessionalService");

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog("ProfessionalService");
builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.Professional);
builder.Services.AddHealthChecks().AddDbContextCheck<ProfessionalDbContext>("database");

builder.Services.AddProfessionalDatabase(builder.Configuration);
builder.Services.AddProfessionalAuthentication(builder.Configuration);
builder.Services.AddProfessionalApiVersioning();
builder.Services.AddProfessionalApplication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UsePramanLogging();
app.UsePramanLifecycleLogging("ProfessionalService");
app.UseGlobalExceptionHandling();

try
{
    using var migrationScope = app.Services.CreateScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<ProfessionalDbContext>();
    await db.Database.MigrateAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogError(ex, "Failed to apply ProfessionalService database migrations on startup.");
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UsePramanObservability();
app.MapPramanAoviasPlatform();

app.Run();
