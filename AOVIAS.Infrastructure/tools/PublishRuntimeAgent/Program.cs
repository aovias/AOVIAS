using Aovias.Configuration.Extensions;
using Aovias.Configuration.Layout;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.SDK;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var platformRoot = ResolvePlatformRoot();
var projectPath = Path.Combine(
    platformRoot,
    "Aovias.Runtime.Agent",
    "src",
    "Aovias.Runtime.Agent.Api",
    "Aovias.Runtime.Agent.Api.csproj");

if (!File.Exists(projectPath))
{
    Console.Error.WriteLine($"Runtime Agent project not found: {projectPath}");
    return 1;
}

var bootstrap = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Aovias:ConfigRoot"] = RepositoryLayoutAoviasConfigPaths.ResolveConfigRootDirectory(platformRoot).Replace('\\', '/'),
        ["Aovias:ProfilesDirectory"] = "profiles",
        ["Aovias:SecretsDirectory"] = "secrets",
        ["Aovias:ActiveProfile"] = "local"
    })
    .AddEnvironmentVariables(prefix: "AOVIAS_")
    .Build();

var configuration = ServiceCollectionExtensions.BuildAoviasConfiguration(
    bootstrap,
    Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production");

var services = new ServiceCollection();
services.AddLogging(logging => logging.AddConsole().SetMinimumLevel(LogLevel.Information));
services.AddAoviasConfiguration(configuration);
services.AddAoviasPublishing();
await using var provider = services.BuildServiceProvider();

var client = provider.GetRequiredService<IPublishingClient>();
var progress = new Progress<PublishingProgress>(update =>
    Console.WriteLine($"[{update.PercentComplete,3}%] {update.StepName,-30} {update.Status,-10} {update.Message}"));

Console.WriteLine($"Publishing Runtime Agent from {projectPath}");
var result = await client.PublishAsync(
    new PublishingRequest
    {
        ProjectPath = projectPath,
        ServiceId = "runtime-agent",
        Version = "1.0.0",
        Profile = "local",
        Publisher = "PublishRuntimeAgent"
    },
    progress);

if (result.IsSuccess)
{
    Console.WriteLine();
    Console.WriteLine($"SUCCESS: {result.PackagePath}");
    Console.WriteLine($"Deployment Ready: {result.ReadinessReport?.IsReady}");
    Console.WriteLine($"Score: {result.ReadinessReport?.OverallScore}%");
    return result.ReadinessReport?.IsReady == true ? 0 : 2;
}

Console.Error.WriteLine();
Console.Error.WriteLine($"FAILED: {result.Message}");
if (result.ReadinessReport?.Errors is { Count: > 0 } errors)
{
    foreach (var error in errors)
    {
        Console.Error.WriteLine($"  {error}");
    }
}

return 1;

static string ResolvePlatformRoot()
    => RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot(AppContext.BaseDirectory);
