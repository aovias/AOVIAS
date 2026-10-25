using Aovias.Configuration.Loading;

namespace Aovias.Configuration.Loading;

/// <summary>
/// Structured bootstrap diagnostics for Runtime Agent startup.
/// </summary>
public sealed class RuntimeBootstrapReport
{
    private readonly List<RuntimeBootstrapModuleResult> modules = [];

    public IReadOnlyList<RuntimeBootstrapModuleResult> Modules => modules;

    public bool StartupSucceeded { get; private set; }

    public void RecordPass(string moduleName)
        => modules.Add(new RuntimeBootstrapModuleResult(moduleName, RuntimeBootstrapModuleStatus.Pass));

    public void RecordSkipped(string moduleName)
        => modules.Add(new RuntimeBootstrapModuleResult(moduleName, RuntimeBootstrapModuleStatus.Skipped));

    public void RecordFailure(string moduleName, string reason)
        => modules.Add(new RuntimeBootstrapModuleResult(moduleName, RuntimeBootstrapModuleStatus.Failed, reason));

    public void MarkStartupSucceeded()
        => StartupSucceeded = true;

    public void MarkStartupFailed()
        => StartupSucceeded = false;

    public void Write(TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(output);

        output.WriteLine();
        output.WriteLine("Runtime Bootstrap Report");
        output.WriteLine();

        foreach (var module in modules)
        {
            output.WriteLine(module.ModuleName);
            output.WriteLine(module.Status.ToString().ToUpperInvariant());

            if (module.Status == RuntimeBootstrapModuleStatus.Failed
                && !string.IsNullOrWhiteSpace(module.Reason))
            {
                output.WriteLine();
                output.WriteLine("Reason");
                output.WriteLine();
                output.WriteLine(module.Reason);
            }

            output.WriteLine();
        }

        output.WriteLine("Startup");
        output.WriteLine(StartupSucceeded ? "SUCCESS" : "FAILED");
    }
}

/// <summary>
/// Bootstrap result for an individual platform module.
/// </summary>
public sealed record RuntimeBootstrapModuleResult(
    string ModuleName,
    RuntimeBootstrapModuleStatus Status,
    string? Reason = null);

/// <summary>
/// Bootstrap status for a platform module.
/// </summary>
public enum RuntimeBootstrapModuleStatus
{
    Pass,
    Skipped,
    Failed
}
