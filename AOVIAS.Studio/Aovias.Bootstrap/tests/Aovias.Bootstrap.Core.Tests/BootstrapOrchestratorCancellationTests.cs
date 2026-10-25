using Aovias.Bootstrap.Contracts;
using Aovias.Release.Contracts.RemoteDeployment;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aovias.Bootstrap.Core.Tests;

/// <summary>
/// Phase C regression test for the between-phase cancellation check added to
/// <see cref="BootstrapOrchestrator.BootstrapAsync"/> — proves cancellation genuinely stops a run
/// (no phase after the cancellation point executes) rather than the token being threaded through
/// every layer as a parameter nobody ever checks, which was the case before this change.
/// </summary>
public sealed class BootstrapOrchestratorCancellationTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "m4.1-bootstrap-cancel-tests", Guid.NewGuid().ToString("N"));

    public BootstrapOrchestratorCancellationTests() => Directory.CreateDirectory(workingDirectory);

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task BootstrapAsync_throws_OperationCanceledException_when_the_token_is_already_cancelled()
    {
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = new BootstrapOrchestrator(executor, registration, new BootstrapStateStore(workingDirectory), NullLogger<BootstrapOrchestrator>.Instance);

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => orchestrator.BootstrapAsync(
            new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory },
            observer: null,
            cts.Token));

        // No command was ever issued to the target — cancellation took effect before Preflight even started.
        Assert.Empty(executor.ExecutedCommands);
    }

    [Fact]
    public async Task Cancelling_during_the_first_phase_prevents_the_second_phase_from_ever_starting()
    {
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = new BootstrapOrchestrator(executor, registration, new BootstrapStateStore(workingDirectory), NullLogger<BootstrapOrchestrator>.Instance, connectRetryDelayMs: 0);

        using var cts = new CancellationTokenSource();
        var observer = new CancelOnFirstPhaseObserver(cts);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => orchestrator.BootstrapAsync(
            new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory },
            observer,
            cts.Token));

        // Connect (the first stage, Sprint M7.x.2) ran to completion — the fake's TestConnectionAsync
        // doesn't itself observe the token, so cancelling inside Connect's own OnPhaseStarting
        // callback doesn't retroactively interrupt work already past that point — but persisting
        // that success (BootstrapStateStore.MarkCompletedAsync, which honors cancellation the same
        // way any System.Text.Json async I/O does) is what actually throws, before the loop reaches
        // observer.OnPhaseFinished for Connect. Whether or not that exact notification fires is not
        // the contract this test protects; what matters is that the next stage's own between-stage
        // cancellation check means ValidatePrivileges never got the chance to start at all.
        Assert.Contains(BootstrapPhaseName.Connect, observer.StartingPhases);
        Assert.DoesNotContain(observer.StartingPhases, phase => phase == BootstrapPhaseName.ValidatePrivileges);
    }

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

    private sealed class CancelOnFirstPhaseObserver(CancellationTokenSource cts) : IBootstrapExecutionObserver
    {
        public List<BootstrapPhaseName> StartingPhases { get; } = [];

        public List<BootstrapPhaseResult> FinishedPhases { get; } = [];

        public void OnPhaseStarting(BootstrapPhaseName phase)
        {
            StartingPhases.Add(phase);
            cts.Cancel();
        }

        public void OnPhaseFinished(BootstrapPhaseResult result) => FinishedPhases.Add(result);

        public void OnCommandStarting(string command)
        {
        }

        public void OnCommandFinished(string command, RemoteCommandResult result)
        {
        }

        public void OnUploadProgress(string remotePath, ulong bytesTransferred, long totalBytes)
        {
        }

        public void OnLog(string message)
        {
        }

        public void OnPhaseProgress(BootstrapPhaseName phase, string message, double? fraction)
        {
        }
    }
}
