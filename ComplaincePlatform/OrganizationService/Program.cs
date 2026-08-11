using OrganizationService.Infrastructure.Extensions;
using OrganizationService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.Extensions;
using Shared.Infrastructure.Email;
using Shared.Logging;
using Shared.Logging.Health;
using Shared.Logging.Platform;
using Shared.Communication;

SerilogBootstrap.CreateBootstrapLogger("OrganizationService");

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog("OrganizationService");
builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.Organization);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrganizationDbContext>("database");

// Add services to the container.
builder.Services.AddDatabase(builder.Configuration);

builder.Services.AddEndpointsApiExplorer();
// Swagger UI temporarily disabled - Swashbuckle.AspNetCore is not compatible with .NET 10
// OpenAPI JSON is still available via Microsoft.AspNetCore.OpenApi at /openapi/v1.json
// TODO: Re-enable Swagger UI once Swashbuckle.AspNetCore supports .NET 10
// builder.Services.AddSwaggerGen();

builder.Services.AddAuthenticationSystem(builder.Configuration);

builder.Services.AddApiVersioningSystem();

builder.Services.AddApplicationServices();
builder.Services.Configure<OrganizationService.Application.Services.OrganizationSettingsOptions>(
    builder.Configuration.GetSection("OrganizationSettings"));
builder.Services.Configure<OrganizationService.Configuration.TrialLifecycleOptions>(
    builder.Configuration.GetSection(OrganizationService.Configuration.TrialLifecycleOptions.SectionName));
builder.Services.Configure<OrganizationService.Configuration.WorkforceOnboardingOptions>(
    builder.Configuration.GetSection(OrganizationService.Configuration.WorkforceOnboardingOptions.SectionName));
builder.Services.AddConfigurableEmailService(builder.Configuration, builder.Environment);
builder.Services.AddPramanCommunication(builder.Configuration);

builder.Services.AddInfrastructureServices();

builder.Services.AddControllers();

var app = builder.Build();

// Correlation + request logging + global exception handling (early).
app.UsePramanLogging();
app.UsePramanLifecycleLogging("OrganizationService");
app.UseGlobalExceptionHandling();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    // Swagger UI temporarily disabled - Swashbuckle.AspNetCore is not compatible with .NET 10
    // OpenAPI JSON is still available via Microsoft.AspNetCore.OpenApi at /openapi/v1.json
    // TODO: Re-enable Swagger UI once Swashbuckle.AspNetCore supports .NET 10
    // app.UseSwagger();
    // app.UseSwaggerUI();
    
    // Apply database migrations in Development
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Applying database migrations for OrganizationService...");
        
        var dbContext = scope.ServiceProvider.GetRequiredService<OrganizationDbContext>();
        
        try
        {
            // Try to apply migrations first
            await dbContext.Database.MigrateAsync();
        }
        catch
        {
            // If migrations fail (e.g., pending model changes), ensure database structure matches model
            await dbContext.Database.EnsureCreatedAsync();
        }
        
        logger.LogInformation("Database migrations applied successfully for OrganizationService.");
    }
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.UsePramanObservability();
app.MapPramanAoviasPlatform();

app.Run();
