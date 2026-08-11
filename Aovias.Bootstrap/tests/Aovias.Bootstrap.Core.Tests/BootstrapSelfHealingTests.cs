using Aovias.Bootstrap.Contracts;
using Aovias.Release.Contracts.RemoteDeployment;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aovias.Bootstrap.Core.Tests;

/// <summary>
/// Sprint M7.x verification scenarios: remote state is authoritative over local checkpoints
/// (checkpoint drift self-heals), configuration drift is detected and corrected by checksum,
/// automatic repair attempts run and are recorded before a stage fails, token rejection fails fast
/// with guidance instead of blind retries, and privilege mode is re-detected on every session
/// (the resumed-run latent defect the stage-owned detection had).
/// </summary>
public sealed class BootstrapSelfHealingTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "m7x-bootstrap-selfheal-tests", Guid.NewGuid().ToString("N"));

    public BootstrapSelfHealingTests() => Directory.CreateDirectory(workingDirectory);

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task BootstrapAsync_reinstalls_docker_when_the_checkpoint_claims_complete_but_the_host_lost_it()
    {
        // "Never depend on local completion state alone. Remote state is authoritative." — the state
        // store swears Docker was installed, but the actual host says otherwise (reimaged VPS, a
        // partial previous installation, manual removal). The old checkpoint-first loop would have
        // skipped straight past this and failed four stages later; the pipeline must re-discover,
        // re-install, and converge.
        var executor = new FakeBootstrapTargetExecutor(); // DockerInstalled = false — the host truth
        var registration = new FakeBootstrapRegistrationClient();
        var stateStore = new BootstrapStateStore(workingDirectory);
        var target = BuildTarget();
        await stateStore.MarkCompletedAsync(target.StableIdentity, BootstrapPhaseName.Preflight, CancellationToken.None);
        await stateStore.MarkCompletedAsync(target.StableIdentity, BootstrapPhaseName.DockerInstallation, CancellationToken.None);

        var orchestrator = new BootstrapOrchestrator(executor, registration, stateStore, NullLogger<BootstrapOrchestrator>.Instance, healthVerificationMaxAttempts: 3, healthVerificationPollDelayMs: 0, containerHealthMaxAttempts: 3, containerHealthPollDelayMs: 0);
        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        Assert.Equal(BootstrapPhaseOutcome.Completed, report.Phases.Single(p => p.Phase == BootstrapPhaseName.DockerInstallation).Outcome);
        Assert.Contains(executor.ExecutedCommands, c => c.Contains("get.docker.com", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BootstrapAsync_corrects_configuration_drift_by_checksum_instead_of_trusting_existing_files()
    {
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);
        var target = BuildTarget();

        var first = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });
        Assert.True(first.OverallSuccess);

        // Drift: something on the host rewrote one generated settings file after bootstrap.
        var driftedPath = RuntimeConfigurationScaffoldBuilder.ResolveProfileFileRemotePath(target, "settings.logging.json");
        var expectedContent = executor.UploadedTextFiles[driftedPath];
        executor.UploadedTextFiles[driftedPath] = "{ \"logRoot\": \"\" }"; // the exact real-world crash shape

        var second = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });

        Assert.True(second.OverallSuccess);
        // The drifted file — and only drift — forced generation to actually run again and restore
        // the correct content byte-for-byte.
        Assert.Equal(BootstrapPhaseOutcome.Completed, second.Phases.Single(p => p.Phase == BootstrapPhaseName.RuntimeConfigurationGeneration).Outcome);
        Assert.Equal(expectedContent, executor.UploadedTextFiles[driftedPath]);
    }

    [Fact]
    public async Task BootstrapAsync_attempts_a_recorded_repair_before_failing_a_crash_looping_container()
    {
        // "Only fail after recovery attempts" — a crash-looping container gets one bounded self-heal
        // round (permission re-repair + force-recreate) before the stage fails, and the failure's
        // root cause records that the repair was attempted, so the operator knows self-healing
        // already tried the obvious fix.
        var executor = new FakeBootstrapTargetExecutor { SimulateCrashLoop = true };
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.False(report.OverallSuccess);
        var failure = report.Phases.Single(p => p.Phase == BootstrapPhaseName.RuntimeAgentInstallation);
        Assert.Contains("Container Crash", failure.Message, StringComparison.Ordinal);
        Assert.Contains("=== Automatic Repair Attempts ===", failure.RootCause, StringComparison.Ordinal);
        Assert.Contains("force-recreated", failure.RootCause, StringComparison.OrdinalIgnoreCase);
        // The repair genuinely re-ran compose up: initial start + one recreate attempt.
        Assert.True(executor.ExecutedCommands.Count(c => c.Contains("--force-recreate", StringComparison.Ordinal)) >= 2);
    }

    [Fact]
    public async Task BootstrapAsync_fails_registration_fast_with_guidance_when_the_token_is_rejected()
    {
        // The real-world round that motivated this: an invalid token produced five identical
        // retries and then a bare "500 Internal Server Error". A token the agent affirmatively
        // rejected will not change on retry — fail once, immediately, with the actual reason and
        // what to do about it.
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient { RejectsRegistrationToken = true };
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.False(report.OverallSuccess);
        var failure = report.Phases.Single(p => p.Phase == BootstrapPhaseName.RuntimeAgentRegistration);
        Assert.Equal(1, registration.AgentRegistrationCallCount); // no blind retries
        Assert.Contains("rejected the registration token", failure.Message, StringComparison.Ordinal);
        Assert.Contains("Registration token is invalid", failure.RootCause, StringComparison.Ordinal);
        Assert.Contains("verify the Registration Token field", failure.RootCause, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BootstrapAsync_redetects_privilege_mode_on_a_resumed_session_instead_of_trusting_the_checkpointed_preflight()
    {
        // Latent defect in the phase-owned design: privilege detection lived inside Preflight, so a
        // resumed run whose Preflight was checkpoint-skipped never detected the mode — leaving it
        // NotRequired and silently running every "privileged" command unelevated. Session
        // initialization now re-detects on every run: the second run below must still elevate.
        var executor = new FakeBootstrapTargetExecutor { HasRequiredPermissions = false, InSudoGroup = true };
        var registration = new FakeBootstrapRegistrationClient();
        var stateStore = new BootstrapStateStore(workingDirectory);
        var elevationHandler = new FakeBootstrapElevationHandler(
            Enumerable.Repeat(new SudoPasswordPromptResult { Password = executor.CorrectSudoPassword, Remember = true }, 4).ToArray());
        var target = BuildTarget();

        var first = new BootstrapOrchestrator(executor, registration, stateStore, NullLogger<BootstrapOrchestrator>.Instance, healthVerificationMaxAttempts: 3, healthVerificationPollDelayMs: 0, elevationHandler: elevationHandler, containerHealthMaxAttempts: 3, containerHealthPollDelayMs: 0);
        Assert.True((await first.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory })).OverallSuccess);

        executor.ElevatedCommands.Clear();
        var resumed = new BootstrapOrchestrator(executor, registration, stateStore, NullLogger<BootstrapOrchestrator>.Instance, healthVerificationMaxAttempts: 3, healthVerificationPollDelayMs: 0, elevationHandler: elevationHandler, containerHealthMaxAttempts: 3, containerHealthPollDelayMs: 0);
        var report = await resumed.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        // The resumed session's read-only discovery against the Docker daemon (network inspect,
        // docker ps, container state) still ran ELEVATED — proof the mode was re-detected rather
        // than defaulting to "no elevation" because Preflight was skipped.
        Assert.Contains(executor.ElevatedCommands, call => call.RequiresSudo);
        Assert.All(executor.ElevatedCommands.Where(call => call.RequiresSudo), call => Assert.Equal(executor.CorrectSudoPassword, call.Password));
    }

    [Fact]
    public async Task BootstrapAsync_recreates_an_existing_but_unhealthy_container_instead_of_calling_it_satisfied()
    {
        // "Container start is NOT success": a container that exists and is even 'running' but not
        // healthy must not satisfy discovery — it gets recreated. The fake reports the container
        // running (docker ps) but its health as 'starting' until the stage's own compose up runs.
        var executor = new FakeBootstrapTargetExecutor { AgentAlreadyRunning = true, ContainerHealthy = false };
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        // Flip to healthy as soon as the stage actually recreates the container, so the run then
        // converges — the point under test is only that recreation happens at all.
        executor.OnComposeUp = () => executor.ContainerHealthy = true;

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        Assert.Equal(BootstrapPhaseOutcome.Completed, report.Phases.Single(p => p.Phase == BootstrapPhaseName.RuntimeAgentInstallation).Outcome);
        Assert.Contains(executor.ExecutedCommands, c => c.Contains("--force-recreate", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BootstrapAsync_attributes_an_exception_thrown_deep_in_DockerNetworkCreation_to_that_stage_never_to_Connect()
    {
        // Sprint M7.x.2 — the exact reported defect: "Bootstrap reports Failed at Connect while the
        // actual exception originates from DockerNetworkCreation." That was possible only because
        // connectivity/privilege detection used to run as unguarded pre-loop code with no stage to
        // attach an exception to. Both are now real stages, and every stage lifecycle call is
        // individually guarded (RunStageAsync's per-step try/catch) — a genuine .NET exception
        // thrown deep inside DockerNetworkCreation's own Execute step must be attributed to
        // DockerNetworkCreation, and Connect/ValidatePrivileges/Preflight (all of which genuinely
        // succeeded first) must be reported as such, never blamed for a later stage's failure.
        var executor = new FakeBootstrapTargetExecutor { ThrowOnDockerNetworkCreate = true };
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.False(report.OverallSuccess);
        Assert.Equal(BootstrapPhaseName.DockerNetworkCreation, report.FirstFailure!.Phase);
        Assert.Contains("DockerNetworkCreation", report.FirstFailure.Message, StringComparison.Ordinal);
        Assert.Contains("Execute", report.FirstFailure.Message, StringComparison.Ordinal);

        // The stages that genuinely ran and succeeded before the failure are reported as exactly
        // that — never swallowed into, or overwritten by, the later failure.
        Assert.Equal(BootstrapPhaseOutcome.Completed, report.Phases.Single(p => p.Phase == BootstrapPhaseName.Connect).Outcome);
        Assert.Equal(BootstrapPhaseOutcome.Completed, report.Phases.Single(p => p.Phase == BootstrapPhaseName.ValidatePrivileges).Outcome);
        Assert.Equal(BootstrapPhaseOutcome.Completed, report.Phases.Single(p => p.Phase == BootstrapPhaseName.Preflight).Outcome);

        // Docker network diagnostics were collected on this failure — never a bare "Unhandled exception".
        Assert.Contains("Docker Daemon Status", report.FirstFailure.RootCause, StringComparison.Ordinal);
        Assert.Contains("Existing Docker Networks", report.FirstFailure.RootCause, StringComparison.Ordinal);
        Assert.Contains("Rootless Docker Detection", report.FirstFailure.RootCause, StringComparison.Ordinal);
    }

    [Fact]
    public async Task BootstrapAsync_attributes_an_exception_thrown_during_privilege_detection_to_ValidatePrivileges_never_to_Connect()
    {
        // The mirror case one stage earlier: an exception during ValidatePrivileges' own Execute
        // step (privilege-mode detection genuinely throwing, not merely returning Unavailable) must
        // never be misattributed to Connect, which already succeeded.
        var executor = new FakeBootstrapTargetExecutor { ThrowOnPrivilegeDetection = true };
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.False(report.OverallSuccess);
        Assert.Equal(BootstrapPhaseName.ValidatePrivileges, report.FirstFailure!.Phase);
        Assert.Contains("ValidatePrivileges", report.FirstFailure.Message, StringComparison.Ordinal);
        Assert.Equal(BootstrapPhaseOutcome.Completed, report.Phases.Single(p => p.Phase == BootstrapPhaseName.Connect).Outcome);
    }

    private BootstrapOrchestrator CreateOrchestrator(FakeBootstrapTargetExecutor executor, FakeBootstrapRegistrationClient registration)
        => new(executor, registration, new BootstrapStateStore(workingDirectory), NullLogger<BootstrapOrchestrator>.Instance, healthVerificationMaxAttempts: 3, healthVerificationPollDelayMs: 0, registrationMaxAttempts: 5, registrationRetryDelayMs: 0, containerHealthMaxAttempts: 3, containerHealthPollDelayMs: 0, connectRetryDelayMs: 0, transientCommandRetryAttempts: 0, transientCommandRetryDelayMs: 0);

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
