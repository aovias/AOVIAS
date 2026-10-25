using EnterpriseDirectoryService.Extensions;
using EnterpriseDirectoryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Extensions;
using Shared.Logging;
using Shared.Logging.Health;
using Shared.Logging.Platform;

SerilogBootstrap.CreateBootstrapLogger("EnterpriseDirectoryService");

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog("EnterpriseDirectoryService");
builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.EnterpriseDirectory);
builder.Services.AddHealthChecks().AddDbContextCheck<EnterpriseDirectoryDbContext>("database");
builder.Services.AddEnterpriseDirectoryDatabase(builder.Configuration);
builder.Services.AddEnterpriseDirectoryAuthentication(builder.Configuration);
builder.Services.AddEnterpriseDirectoryApiVersioning();
builder.Services.AddEnterpriseDirectoryApplication(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();
app.UsePramanLogging();
app.UsePramanLifecycleLogging("EnterpriseDirectoryService");
app.UseGlobalExceptionHandling();

try
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<EnterpriseDirectoryDbContext>().Database.MigrateAsync();
    await DirectorySynonymSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<EnterpriseDirectoryDbContext>());
}
catch (Exception ex)
{
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup").LogError(ex, "Failed to apply EnterpriseDirectoryService migrations.");
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

internal static class DirectorySynonymSeeder
{
    public static async Task SeedAsync(EnterpriseDirectoryDbContext db)
    {
        if (await db.DirectorySynonyms.AnyAsync()) return;
        var pairs = new[] { ("nurse", "rn"), ("icu", "intensive care"), ("er", "emergency"), ("md", "doctor"), ("acls", "advanced cardiac life support") };
        foreach (var (token, synonym) in pairs)
            db.DirectorySynonyms.Add(new EnterpriseDirectoryService.Domain.ReadModels.DirectorySynonym { Id = Guid.NewGuid(), Token = token, Synonym = synonym });
        await db.SaveChangesAsync();
    }
}
