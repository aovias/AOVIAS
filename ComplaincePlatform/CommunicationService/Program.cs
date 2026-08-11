using CommunicationService.Extensions;
using CommunicationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Extensions;
using Shared.Logging;
using Shared.Logging.Health;
using Shared.Logging.Platform;

SerilogBootstrap.CreateBootstrapLogger("CommunicationService");

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog("CommunicationService");
builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.Communication);
builder.Services.AddHealthChecks().AddDbContextCheck<CommunicationDbContext>("database");
builder.Services.AddCommunicationDatabase(builder.Configuration);
builder.Services.AddCommunicationAuthentication(builder.Configuration);
builder.Services.AddCommunicationApiVersioning();
builder.Services.AddCommunicationApplication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();
app.UsePramanLogging();
app.UsePramanLifecycleLogging("CommunicationService");
app.UseGlobalExceptionHandling();

try
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<CommunicationDbContext>().Database.MigrateAsync();
}
catch (Exception ex)
{
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup").LogError(ex, "Failed to apply CommunicationService migrations.");
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UsePramanObservability();
app.MapPramanAoviasPlatform();
app.Run();
