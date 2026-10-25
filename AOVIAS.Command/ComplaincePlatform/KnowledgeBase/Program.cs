using KnowledgeBase.Api.Extensions;
using KnowledgeBase.Infrastructure;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Extensions;
using Shared.Logging;
using Shared.Logging.Health;
using Shared.Logging.Platform;

SerilogBootstrap.CreateBootstrapLogger("KnowledgeBase");

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog("KnowledgeBase");
builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.KnowledgeBase);

var knowledgeConnection = builder.Configuration.GetConnectionString("KnowledgeConnection")
    ?? "Server=localhost\\SQLEXPRESS;Database=KnowledgeBase;Trusted_Connection=True;TrustServerCertificate=True";

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddKnowledgebasePersistenceForTests();
}
else
{
    builder.Services.AddKnowledgebasePersistenceForSqlServer(knowledgeConnection);
}

builder.Services.AddKnowledgebaseAIServices();

builder.Services.AddKnowledgebaseApiVersioning();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddControllers();
builder.Services.AddHealthChecks()
    .AddDbContextCheck<KnowledgeBaseContext>("database");

var app = builder.Build();

// Correlation + request logging + global exception handling (early).
app.UsePramanLogging();
app.UsePramanLifecycleLogging("KnowledgeBase");
app.UseGlobalExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeBaseContext>();
        if (db.Database.IsRelational())
            await db.Database.MigrateAsync();
    }
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UsePramanObservability();
app.MapPramanAoviasPlatform();
app.Run();

public partial class Program;
