using Aovias.Bootstrap.Contracts;
using Aovias.Release.Contracts.RemoteDeployment;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aovias.Bootstrap.Core.Tests;

/// <summary>
/// Phase A regression tests for the <see cref="IBootstrapExecutionObserver"/> seam introduced
/// alongside the Operation Engine work: the orchestrator's phase-lifecycle notifications must be
/// complete, correctly ordered, and never required (omitting the observer must behave exactly as
/// before this seam existed — see
/// <see cref="BootstrapAsync_with_no_observer_behaves_exactly_as_before_the_observer_seam_existed"/>).
/// </summary>
public sealed class BootstrapExecutionObserverTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "m4.1-bootstrap-observer-tests", Guid.NewGuid().ToString("N"));

    public BootstrapExecutionObserverTests() => Directory.CreateDirectory(workingDirectory);

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task BootstrapAsync_reports_every_phase_starting_and_finished_in_ascending_order_on_a_full_run()
    {
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);
        var observer = new RecordingBootstrapExecutionObserver();

        var report = await orchestrator.BootstrapAsync(
            new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory },
            observer);

        Assert.True(report.OverallSuccess);

        var expectedOrder = Enum.GetValues<BootstrapPhaseName>().OrderBy(p => (int)p).ToList();
        Assert.Equal(expectedOrder, observer.StartingPhases);
        Assert.Equal(expectedOrder, observer.FinishedPhases.Select(result => result.Phase));

        // Every finished phase in a from-scratch run actually ran (Completed), never a phantom skip.
        Assert.All(observer.FinishedPhases, result => Assert.Equal(BootstrapPhaseOutcome.Completed, result.Outcome));

        // The report itself is unaffected by observing it — same content whether or not an observer is attached.
        Assert.Equal(report.Phases.Select(p => p.Phase), observer.FinishedPhases.Select(p => p.Phase));
    }

    [Fact]
    public async Task BootstrapAsync_reports_resumed_phases_as_starting_and_finishing_AlreadySatisfied()
    {
        // Remote discovery is authoritative (Sprint M7.x), so the fake host must actually be in
        // the state the checkpoints claim — Docker genuinely installed.
        var executor = new FakeBootstrapTargetExecutor { DockerInstalled = true };
        var registration = new FakeBootstrapRegistrationClient();
        var stateStore = new BootstrapStateStore(workingDirectory);
        var target = BuildTarget();

        await stateStore.MarkCompletedAsync(target.StableIdentity, BootstrapPhaseName.Preflight, CancellationToken.None);
        await stateStore.MarkCompletedAsync(target.StableIdentity, BootstrapPhaseName.DockerInstallation, CancellationToken.None);

        var orchestrator = new BootstrapOrchestrator(executor, registration, stateStore, NullLogger<BootstrapOrchestrator>.Instance);
        var observer = new RecordingBootstrapExecutionObserver();

        await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory }, observer);

        // Resumed phases still raise both callbacks — a UI observer must be able to mark them
        // Completed on screen without special-casing "was this phase actually run or just resumed".
        Assert.Contains(BootstrapPhaseName.Preflight, observer.StartingPhases);
        Assert.Contains(BootstrapPhaseName.DockerInstallation, observer.StartingPhases);
        Assert.Equal(BootstrapPhaseOutcome.AlreadySatisfied, observer.FinishedPhases.Single(r => r.Phase == BootstrapPhaseName.Preflight).Outcome);
        Assert.Equal(BootstrapPhaseOutcome.AlreadySatisfied, observer.FinishedPhases.Single(r => r.Phase == BootstrapPhaseName.DockerInstallation).Outcome);
    }

    [Fact]
    public async Task BootstrapAsync_never_reports_a_phase_starting_after_the_first_failure()
    {
        var executor = new FakeBootstrapTargetExecutor { DockerInstallCommandFails = true };
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);
        var observer = new RecordingBootstrapExecutionObserver();

        var report = await orchestrator.BootstrapAsync(
            new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory },
            observer);

        Assert.False(report.OverallSuccess);

        // Only Connect/ValidatePrivileges/Preflight (succeeded) and DockerInstallation (failed) were
        // ever started — matching the "keep completed stages green, future stages stay Pending"
        // contract a UI relies on.
        Assert.Equal(
            [BootstrapPhaseName.Connect, BootstrapPhaseName.ValidatePrivileges, BootstrapPhaseName.Preflight, BootstrapPhaseName.DockerInstallation],
            observer.StartingPhases);
        Assert.Equal(BootstrapPhaseOutcome.Completed, observer.FinishedPhases.Single(r => r.Phase == BootstrapPhaseName.Preflight).Outcome);
        Assert.Equal(BootstrapPhaseOutcome.Failed, observer.FinishedPhases.Single(r => r.Phase == BootstrapPhaseName.DockerInstallation).Outcome);
    }

    [Fact]
    public async Task BootstrapAsync_with_no_observer_behaves_exactly_as_before_the_observer_seam_existed()
    {
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        // No observer argument at all — the pre-Phase-A call shape must still compile and behave identically.
        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        Assert.Equal(11, report.Phases.Count);
    }

    private BootstrapOrchestrator CreateOrchestrator(FakeBootstrapTargetExecutor executor, FakeBootstrapRegistrationClient registration)
        => new(executor, registration, new BootstrapStateStore(workingDirectory), NullLogger<BootstrapOrchestrator>.Instance);

    private static BootstrapTarget BuildTarget() => new()
    {
        Host = "vps-demo.example.test",
        Username = "deploy",
        Password = "unused-in-tests",
        ProfileName = "demo",
        EnvironmentName = "Demo VPS",
        EnvironmentType = EnvironmentType.Demo,
        RegistrationToken = "demo-vps-registration-token",
        PublicUrl = "https://vps-demo.example.test",
        CommandCenterBaseUrl = "https://command-center.example.test",
        RuntimeAgentArtifactPath = "/local/path/to/agent-publish-output",
        RemoteConfigRoot = "/opt/aovias/config",
        DockerNetworkName = "aovias-net"
    };
}
