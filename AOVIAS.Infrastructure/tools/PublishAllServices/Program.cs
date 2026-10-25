using Aovias.Configuration.Extensions;
using Aovias.Configuration.Layout;
using Aovias.Publishing.Contracts;
using Aovias.Publishing.SDK;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var platformRoot = ResolvePlatformRoot();
var profile = args.FirstOrDefault(arg => arg.StartsWith("--profile=", StringComparison.OrdinalIgnoreCase))?.Split('=', 2)[1]
    ?? (OperatingSystem.IsWindows() ? "local" : "demo");
var version = args.FirstOrDefault(arg => arg.StartsWith("--version=", StringComparison.OrdinalIgnoreCase))?.Split('=', 2)[1] ?? "1.0.0";
var discoverOnly = args.Any(arg => arg.Equals("--discover", StringComparison.OrdinalIgnoreCase));
var preflightOnly = args.Any(arg => arg.Equals("--preflight-only", StringComparison.OrdinalIgnoreCase));
var skipPreflight = args.Any(arg => arg.Equals("--skip-preflight", StringComparison.OrdinalIgnoreCase));
var inspectOnly = args.Any(arg => arg.Equals("--inspect-only", StringComparison.OrdinalIgnoreCase));

var bootstrap = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Aovias:ConfigRoot"] = RepositoryLayoutAoviasConfigPaths.ResolveConfigRootDirectory(platformRoot).Replace('\\', '/'),
        ["Aovias:ProfilesDirectory"] = "profiles",
        ["Aovias:SecretsDirectory"] = "secrets",
        ["Aovias:ActiveProfile"] = profile
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

if (discoverOnly)
{
    var projects = await client.DiscoverDeployableProjectsAsync(platformRoot);
    Console.WriteLine($"Discovered {projects.Count} deployable project(s):");
    foreach (var project in projects)
    {
        Console.WriteLine($"  {project.ServiceId,-22} {project.Type,-12} {project.DiscoveryPath}");
    }

    return projects.Count == 0 ? 1 : 0;
}

var publishingSettings = configuration.GetSection("Publishing").Get<Aovias.Configuration.Settings.PublishingSettings>();
var outputDirectory = publishingSettings?.OutputFolder ?? Path.Combine(
    RepositoryLayoutAoviasConfigPaths.ResolveConfigRootDirectory(platformRoot),
    "storage",
    "local",
    "packages");

var cliPreflight = !skipPreflight;

if (cliPreflight || preflightOnly)
{
    Console.WriteLine($"Running publish preflight (profile={profile}, version={version})...");
    var preflight = await client.RunSolutionPreflightAsync(
        new SolutionPublishingPreflightRequest
        {
            PlatformRoot = platformRoot,
            Profile = profile,
            Version = version,
            OutputDirectory = outputDirectory
        },
        new Progress<SolutionPublishingPreflightProgress>(update =>
            Console.WriteLine($"[{update.Category}] {update.Message}")));

    PrintPreflightReport(preflight);

    if (preflightOnly)
    {
        return preflight.IsReady ? 0 : 1;
    }

    if (!preflight.IsReady)
    {
        Console.WriteLine();
        Console.WriteLine("Publish aborted. Resolve blocking issues and rerun preflight.");
        return 1;
    }
}

if (inspectOnly)
{
    Console.WriteLine($"Inspecting release library at {outputDirectory}...");
    var inspection = await client.InspectReleaseLibraryAsync(outputDirectory, profile);
    PrintInspectionReport(inspection);
    return inspection.IsValid ? 0 : 1;
}

Console.WriteLine($"Publishing solution with profile={profile}, version={version}");
var result = await client.PublishSolutionAsync(
    new SolutionPublishingRequest
    {
        PlatformRoot = platformRoot,
        Profile = profile,
        Version = version,
        Publisher = "PublishAllServices",
        GenerateDemoBundle = true,
        OrganizeReleaseLibrary = true,
        SkipPreflight = cliPreflight,
    },
    new Progress<SolutionPublishingProgress>(progress =>
    {
        if (progress.ProjectStatus is not null)
        {
            Console.WriteLine($"[{progress.Phase}] {progress.ProjectStatus.ServiceId}: {progress.ProjectStatus.CurrentStep}");
        }
        else
        {
            Console.WriteLine($"[{progress.Phase}] {progress.Message}");
        }
    }));

Console.WriteLine();
Console.WriteLine(result.Message);
Console.WriteLine($"Deployment ready: {result.DeploymentReadyCount}/{result.ProjectResults.Count}");
Console.WriteLine($"Manifest: {result.SolutionDeploymentManifestPath}");
Console.WriteLine($"Bundle: {result.DemoDeploymentBundlePath}");
Console.WriteLine($"Logs: {result.PublishingLogDirectory}");

if (result.PackageInspectionResult is not null)
{
    Console.WriteLine();
    PrintInspectionReport(result.PackageInspectionResult);
}

foreach (var project in result.ProjectResults.Where(item => !item.IsSuccess))
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"FAIL {project.Project.ServiceId}: {project.ErrorMessage}");
    Console.ResetColor();
}

return result.IsSuccess ? 0 : 1;

static void PrintPreflightReport(SolutionPublishingPreflightResult preflight)
{
    Console.WriteLine();
    Console.WriteLine(new string('=', 72));
    Console.WriteLine(preflight.Verdict);
    Console.WriteLine(new string('=', 72));
    Console.WriteLine(preflight.Summary);
    Console.WriteLine();

    foreach (var check in preflight.Checks)
    {
        Console.WriteLine($"{(check.Passed ? "PASS" : "FAIL")} {check.Category}: {check.Message}");
        foreach (var detail in check.Details)
        {
            Console.WriteLine($"  {detail}");
        }
    }

    if (preflight.ProjectWarningCounts.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Build warnings by project:");
        foreach (var (project, count) in preflight.ProjectWarningCounts.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        {
            Console.WriteLine($"  {project}: {count}");
        }
    }

    if (preflight.BlockingReasons.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Blocking reasons:");
        foreach (var reason in preflight.BlockingReasons)
        {
            Console.WriteLine($"  - {reason}");
        }
    }

    if (preflight.Warnings.Count > 0)
    {
        Console.WriteLine();
        Console.WriteLine("Warnings:");
        foreach (var warning in preflight.Warnings)
        {
            Console.WriteLine($"  - {warning}");
        }
    }

    Console.WriteLine();
    Console.WriteLine($"Preflight completed in {preflight.Duration.TotalSeconds:F1}s.");
}

static void PrintInspectionReport(SolutionPackageInspectionResult inspection)
{
    Console.WriteLine(new string('=', 72));
    Console.WriteLine(inspection.IsValid ? "PACKAGES VALID" : "PACKAGES INVALID");
    Console.WriteLine(new string('=', 72));
    Console.WriteLine(inspection.Summary);
    Console.WriteLine($"Manifest: {inspection.SolutionDeploymentManifestPath ?? "(missing)"}");
    Console.WriteLine($"Bundle: {inspection.DemoDeploymentBundlePath ?? "(missing)"}");
    Console.WriteLine();

    foreach (var package in inspection.Packages)
    {
        Console.WriteLine($"{(package.IsValid ? "PASS" : "FAIL")} {package.ServiceId} ({package.Category})");
        Console.WriteLine($"  {package.PackagePath}");
        foreach (var file in package.PresentFiles)
        {
            Console.WriteLine($"    OK  {file}");
        }

        foreach (var file in package.MissingFiles)
        {
            Console.WriteLine($"    MISSING  {file}");
        }
    }

    if (inspection.Errors.Count > 0)
    {
        Console.WriteLine();
        foreach (var error in inspection.Errors)
        {
            Console.WriteLine($"  ERROR: {error}");
        }
    }
}

static string ResolvePlatformRoot()
    => RepositoryLayoutProductionAdapter.ResolvePublishingPlatformRoot(AppContext.BaseDirectory);
