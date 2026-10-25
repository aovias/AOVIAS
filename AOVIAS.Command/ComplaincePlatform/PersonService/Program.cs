using Microsoft.EntityFrameworkCore;
using PersonService.Extensions;
using PersonService.Infrastructure.Persistence;
using Shared.Infrastructure.Extensions;
using Shared.Logging;
using Shared.Logging.Health;
using Shared.Logging.Platform;

SerilogBootstrap.CreateBootstrapLogger("PersonService");

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog("PersonService");
builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.Person);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<PersonDbContext>("database");

builder.Services.AddPersonDatabase(builder.Configuration);
builder.Services.AddPersonAuthentication(builder.Configuration);
builder.Services.AddPersonApiVersioning();
builder.Services.AddPersonApplication();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UsePramanLogging();
app.UsePramanLifecycleLogging("PersonService");
app.UseGlobalExceptionHandling();

try
{
    using var migrationScope = app.Services.CreateScope();
    var db = migrationScope.ServiceProvider.GetRequiredService<PersonDbContext>();
    await db.Database.MigrateAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    logger.LogError(ex, "Failed to apply PersonService database migrations on startup.");
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UsePramanObservability();
app.MapPramanAoviasPlatform();

app.Run();
