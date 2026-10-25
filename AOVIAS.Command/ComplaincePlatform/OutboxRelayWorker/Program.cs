using Azure.Messaging.ServiceBus;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OutboxRelayWorker;
using Shared.Logging;
using Shared.Logging.Diagnostics;

SerilogBootstrap.CreateBootstrapLogger("OutboxRelayWorker");

var builder = Host.CreateApplicationBuilder(args);
builder.ConfigureSerilog("OutboxRelayWorker");

var complianceConnection = builder.Configuration.GetConnectionString("ComplianceConnection")
    ?? "Server=localhost\\SQLEXPRESS;Database=Compliance;Trusted_Connection=True;TrustServerCertificate=True";

builder.Services.AddDbContext<ComplianceDbContext>(options => options
    .UseSqlServer(complianceConnection)
    .AddInterceptors(LoggingDbCommandInterceptor.Instance));
builder.Services.AddSingleton(sp =>
{
    var connection = builder.Configuration.GetConnectionString("ServiceBus")
        ?? throw new InvalidOperationException("ServiceBus connection string is required.");
    return new ServiceBusClient(connection);
});
builder.Services.AddHostedService<OutboxRelayBackgroundService>();
builder.Services.AddHostedService<WorkforceOutboxRelayBackgroundService>();
builder.Services.AddHostedService<DirectoryOutboxRelayBackgroundService>();
builder.Services.AddHostedService<IntegrationOutboxRelayBackgroundService>();

await builder.Build().RunAsync();
