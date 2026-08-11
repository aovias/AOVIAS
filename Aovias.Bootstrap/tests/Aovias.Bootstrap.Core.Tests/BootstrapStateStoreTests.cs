using Aovias.Bootstrap.Contracts;
using Xunit;

namespace Aovias.Bootstrap.Core.Tests;

/// <summary>
/// Bootstrap Configuration Validation &amp; Runtime Startup Diagnostics defect fix: inserting
/// <see cref="BootstrapPhaseName.ValidateRuntimeConfiguration"/> renumbered every phase after it.
/// These tests prove that change is safe for both a fresh state store (string-keyed, unaffected by
/// any future renumbering) and a state file written by an older build in the pre-fix numeric format
/// (must degrade to "nothing completed" rather than throwing).
/// </summary>
public sealed class BootstrapStateStoreTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "m-state-store-tests", Guid.NewGuid().ToString("N"));

    public BootstrapStateStoreTests() => Directory.CreateDirectory(workingDirectory);

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task MarkCompletedAsync_round_trips_via_string_phase_names_not_numeric_values()
    {
        var store = new BootstrapStateStore(workingDirectory);
        await store.MarkCompletedAsync("target-a", BootstrapPhaseName.ValidateRuntimeConfiguration, CancellationToken.None);

        var statePath = Path.Combine(workingDirectory, "bootstrap-state", "target-a.json");
        var raw = await File.ReadAllTextAsync(statePath);

        Assert.Contains("ValidateRuntimeConfiguration", raw, StringComparison.Ordinal);
        Assert.DoesNotContain("\": [\n      6\n    ]", raw, StringComparison.Ordinal);

        var completed = await store.GetCompletedPhasesAsync("target-a", CancellationToken.None);
        Assert.Contains(BootstrapPhaseName.ValidateRuntimeConfiguration, completed);
    }

    [Fact]
    public async Task GetCompletedPhasesAsync_degrades_to_empty_rather_than_throwing_for_a_pre_fix_numeric_format_file()
    {
        var stateDirectory = Path.Combine(workingDirectory, "bootstrap-state");
        Directory.CreateDirectory(stateDirectory);
        // Shape a build before this defect fix would have written: numeric phase values, where 6/7/8
        // meant RuntimeAgentInstallation/RuntimeAgentRegistration/BootstrapVerification — now
        // ValidateRuntimeConfiguration/RuntimeAgentInstallation/RuntimeAgentRegistration instead.
        await File.WriteAllTextAsync(
            Path.Combine(stateDirectory, "legacy-target.json"),
            """{"TargetIdentity":"legacy-target","CompletedPhases":[1,2,3,4,5,6,7,8]}""");

        var store = new BootstrapStateStore(workingDirectory);
        var completed = await store.GetCompletedPhasesAsync("legacy-target", CancellationToken.None);

        Assert.Empty(completed);
    }
}
