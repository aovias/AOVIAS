using Aovias.Configuration.Deployment;
using Aovias.Deployment.Contracts.Provisioning;

namespace Aovias.Runtime.Agent.Infrastructure.Deployment;

/// <summary>
/// Runtime Agent adapter over the platform <see cref="IDeploymentPathResolver"/>.
/// </summary>
public sealed class RuntimeAgentDeploymentPathResolver
{
    private readonly IDeploymentPathResolver pathResolver;

    public RuntimeAgentDeploymentPathResolver(IDeploymentPathResolver pathResolver)
    {
        this.pathResolver = pathResolver;
    }

    public HostPathContext? TryResolveHostPaths()
        => new()
        {
            HostConfigRoot = pathResolver.ConfigRoot,
            HostLogsRoot = pathResolver.LogsRoot,
            HostPackagesRoot = pathResolver.PackagesRoot
        };

    public string ResolveRuntimeRoot() => pathResolver.RuntimeRoot;

    public string ResolveSessionStoreRoot()
    {
        // Path.Combine joins with the OS-native separator; DeploymentRoot is already
        // forward-slash normalized (ConfiguredDeploymentPathResolver's platform-wide convention),
        // so on Windows this must be re-normalized to avoid a mixed-separator result — invisible
        // on Linux, where the native separator already is '/'.
        var path = Path.Combine(pathResolver.DeploymentRoot, "state", "deployment-sessions").Replace('\\', '/');
        pathResolver.EnsureDirectory(path);
        return path;
    }

    public string ResolvePackageStoreRoot() => pathResolver.PackagesRoot;

    public void EnsureDirectory(string path) => pathResolver.EnsureDirectory(path);

    public string ValidateWritablePath(string path)
    {
        var validation = pathResolver.ValidateWritable(path, "Deployment Path", "Provisioning", "ValidateWritable");
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(validation.FailureMessage ?? $"Path '{path}' is not writable.");
        }

        return path;
    }

    internal bool IsConfiguredDeploymentPath(string normalizedPath)
        => normalizedPath.StartsWith(pathResolver.DeploymentRoot, StringComparison.OrdinalIgnoreCase)
           || normalizedPath.StartsWith(pathResolver.RuntimeRoot, StringComparison.OrdinalIgnoreCase);
}
