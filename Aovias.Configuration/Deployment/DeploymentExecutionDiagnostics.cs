using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Aovias.Configuration.Settings;

namespace Aovias.Configuration.Deployment;

/// <summary>
/// Sprint 11.5D live execution tracing for deployment upload and filesystem operations.
/// </summary>
public static class DeploymentExecutionDiagnostics
{
    private const string BuildManifestPath = "/app/aovias-build-manifest.json";
    private const string UploadEndpoint = "POST /api/releases/upload";
    private static readonly object Sync = new();
    private static int uploadTraceActive;

    public static void BeginUploadTrace(TextWriter? writer = null)
    {
        Interlocked.Increment(ref uploadTraceActive);
        var output = ResolveWriter(writer);
        lock (Sync)
        {
            output.WriteLine();
            output.WriteLine("=================================================");
            output.WriteLine("Deployment Upload Execution Trace");
            output.WriteLine($"Endpoint: {UploadEndpoint}");
            output.WriteLine("=================================================");
            WriteAssemblyIdentity(output);
        }
    }

    public static void EndUploadTrace(TextWriter? writer = null)
    {
        if (Interlocked.Decrement(ref uploadTraceActive) <= 0)
        {
            Interlocked.Exchange(ref uploadTraceActive, 0);
        }

        var output = ResolveWriter(writer);
        lock (Sync)
        {
            output.WriteLine("=================================================");
            output.WriteLine("Deployment Upload Execution Trace — Complete");
            output.WriteLine("=================================================");
            output.WriteLine();
        }
    }

    public static void LogPipelineStage(
        string layer,
        string className,
        string methodName,
        TextWriter? writer = null)
    {
        var output = ResolveWriter(writer);
        lock (Sync)
        {
            output.WriteLine($"[{layer}] {className}.{methodName}");
        }
    }

    public static void LogConfigurationSnapshot(
        ReleaseManagementSettings settings,
        IDeploymentPathResolver pathResolver,
        string context,
        TextWriter? writer = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(pathResolver);

        var output = ResolveWriter(writer);
        lock (Sync)
        {
            output.WriteLine();
            output.WriteLine($"--- Configuration Snapshot ({context}) ---");
            output.WriteLine($"ReleaseManagementSettings.ReleaseRoot = {settings.ReleaseRoot}");
            output.WriteLine($"IDeploymentPathResolver.ReleaseRoot = {pathResolver.ReleaseRoot}");

            if (!PathsEqual(settings.ReleaseRoot, pathResolver.ReleaseRoot))
            {
                output.WriteLine("CONFIGURATION DIVERGENCE DETECTED");
            }

            output.WriteLine($"ReleaseManagementSettings.IncomingFolder = {settings.IncomingFolder}");
            output.WriteLine($"IDeploymentPathResolver.IncomingRoot = {pathResolver.IncomingRoot}");
            output.WriteLine($"ReleaseManagementSettings.TemporaryFolder = {settings.TemporaryFolder}");
            output.WriteLine($"IDeploymentPathResolver.TempRoot = {pathResolver.TempRoot}");
        }
    }

    public static void LogReleaseRootAccess(
        IDeploymentPathResolver pathResolver,
        string className,
        string methodName,
        TextWriter? writer = null)
    {
        ArgumentNullException.ThrowIfNull(pathResolver);

        var output = ResolveWriter(writer);
        lock (Sync)
        {
            output.WriteLine($"[ReleaseRoot Access] {className}.{methodName}");
            output.WriteLine($"Resolved ReleaseRoot = {pathResolver.ReleaseRoot}");
        }
    }

    public static void CreateDirectory(string path, string className, string methodName, TextWriter? writer = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentException.ThrowIfNullOrWhiteSpace(className);
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        var output = ResolveWriter(writer);
        var stackTrace = new StackTrace(true);
        lock (Sync)
        {
            output.WriteLine();
            output.WriteLine("--- Directory.CreateDirectory ---");
            output.WriteLine($"Class: {className}");
            output.WriteLine($"Method: {methodName}");
            output.WriteLine($"Resolved Path: {path}");
            if (IsLegacyReleasePath(path))
            {
                output.WriteLine("LEGACY PATH DETECTED: /opt/aovias/releases");
            }

            output.WriteLine("Call Stack:");
            output.WriteLine(stackTrace.ToString());
        }

        Directory.CreateDirectory(path);
    }

    public static void WriteAssemblyIdentity(TextWriter? writer = null)
    {
        var output = ResolveWriter(writer);
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var identity = assembly.GetName();
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? string.Empty;
        var manifest = TryReadBuildManifest();

        lock (Sync)
        {
            output.WriteLine("--- Executing Assembly ---");
            output.WriteLine($"Assembly: {assembly.GetName().Name}");
            output.WriteLine($"Assembly Version: {identity.Version}");
            output.WriteLine($"Informational Version: {informational}");
            output.WriteLine($"Source Revision: {manifest?.SourceRevision ?? "(not in manifest)"}");
            output.WriteLine($"BuildUtc: {manifest?.BuildUtc ?? "(not in manifest)"}");
            output.WriteLine($"Location: {assembly.Location}");
        }
    }

    private static BuildManifest? TryReadBuildManifest()
    {
        if (!File.Exists(BuildManifestPath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BuildManifest>(
                File.ReadAllText(BuildManifestPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool PathsEqual(string? left, string? right)
        => string.Equals(Norm(left), Norm(right), StringComparison.OrdinalIgnoreCase);

    private static string Norm(string? path)
        => (path ?? string.Empty).Replace('\\', '/').TrimEnd('/');

    private static bool IsLegacyReleasePath(string path)
        => path.Contains("/opt/aovias/releases", StringComparison.OrdinalIgnoreCase);

    private static TextWriter ResolveWriter(TextWriter? writer)
        => writer ?? Console.Out;

    private sealed class BuildManifest
    {
        public string SourceRevision { get; init; } = string.Empty;

        public string BuildUtc { get; init; } = string.Empty;
    }
}
