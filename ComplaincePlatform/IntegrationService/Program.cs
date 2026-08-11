using IntegrationService.Extensions;
using IntegrationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Extensions;
using Shared.Logging;
using Shared.Logging.Health;
using Shared.Logging.Platform;

SerilogBootstrap.CreateBootstrapLogger("IntegrationService");

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog("IntegrationService");
builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.Integration);
builder.Services.AddHealthChecks().AddDbContextCheck<IntegrationDbContext>("database");
builder.Services.AddIntegrationDatabase(builder.Configuration);
builder.Services.AddIntegrationAuthentication(builder.Configuration);
builder.Services.AddIntegrationApiVersioning();
builder.Services.AddIntegrationApplication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();
app.UsePramanLogging();
app.UsePramanLifecycleLogging("IntegrationService");
app.UseGlobalExceptionHandling();

try
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<IntegrationDbContext>().Database.MigrateAsync();
    await IntegrationSeedData.SeedAsync(scope.ServiceProvider.GetRequiredService<IntegrationDbContext>());
}
catch (Exception ex)
{
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup").LogError(ex, "Failed to apply IntegrationService migrations.");
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

internal static class IntegrationSeedData
{
    public static async Task SeedAsync(IntegrationDbContext db)
    {
        if (await db.TransformationRules.AnyAsync()) return;
        db.TransformationRules.Add(new IntegrationService.Domain.ReadModels.TransformationRule
        {
            Id = Guid.NewGuid(), OrganizationId = Guid.Empty, Name = "TrimString", RuleType = "String", RulesJson = "{}"
        });
        await db.SaveChangesAsync();
    }
}
