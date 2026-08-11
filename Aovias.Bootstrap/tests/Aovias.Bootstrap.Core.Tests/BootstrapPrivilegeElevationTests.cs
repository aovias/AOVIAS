using Aovias.Bootstrap.Contracts;
using Aovias.Release.Contracts.RemoteDeployment;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aovias.Bootstrap.Core.Tests;

/// <summary>
/// Bootstrap Privilege Elevation regression tests. Covers the four scenarios called out by the
/// feature's own verification requirements: passwordless sudo, password-based sudo, an incorrect
/// password followed by a correct retry, and an operator-cancelled elevation prompt — all against
/// the same <see cref="BootstrapOrchestrator"/>/<see cref="FakeBootstrapTargetExecutor"/> used by
/// every other Core test, so there is exactly one orchestrator under test, not a parallel one.
/// </summary>
public sealed class BootstrapPrivilegeElevationTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "m-privilege-elevation-tests", Guid.NewGuid().ToString("N"));

    public BootstrapPrivilegeElevationTests() => Directory.CreateDirectory(workingDirectory);

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Mode1_SshKey_and_passwordless_sudo_never_calls_the_elevation_handler()
    {
        // HasRequiredPermissions=true (default) => `sudo -n true` succeeds => Passwordless.
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var elevationHandler = new FakeBootstrapElevationHandler(); // no answers queued — must never be asked
        var orchestrator = CreateOrchestrator(executor, registration, elevationHandler);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        Assert.Equal(0, elevationHandler.RequestCount);
        // Privileged commands still ran (docker install, network create, mkdir, docker load/compose
        // up) — just elevated with no password, proving the passwordless path is genuinely exercised
        // rather than silently skipped.
        Assert.Contains(executor.ElevatedCommands, call => call.RequiresSudo && call.Password is null);
    }

    [Fact]
    public async Task Mode3_password_sudo_prompts_once_and_reuses_the_remembered_password_for_every_later_privileged_command()
    {
        var executor = new FakeBootstrapTargetExecutor { HasRequiredPermissions = false, InSudoGroup = true };
        var registration = new FakeBootstrapRegistrationClient();
        var elevationHandler = new FakeBootstrapElevationHandler(
            new SudoPasswordPromptResult { Password = executor.CorrectSudoPassword, Remember = true });
        var orchestrator = CreateOrchestrator(executor, registration, elevationHandler);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        Assert.Equal(1, elevationHandler.RequestCount);
        Assert.True(executor.ElevatedCommands.Count(call => call.RequiresSudo) > 1);
        Assert.All(executor.ElevatedCommands.Where(call => call.RequiresSudo), call => Assert.Equal(executor.CorrectSudoPassword, call.Password));
    }

    [Fact]
    public async Task Password_sudo_without_remember_reprompts_for_every_later_privileged_command()
    {
        var executor = new FakeBootstrapTargetExecutor { HasRequiredPermissions = false, InSudoGroup = true };
        var registration = new FakeBootstrapRegistrationClient();
        var elevationHandler = new FakeBootstrapElevationHandler(
            Enumerable.Repeat(new SudoPasswordPromptResult { Password = executor.CorrectSudoPassword, Remember = false }, 25).ToArray());
        var orchestrator = CreateOrchestrator(executor, registration, elevationHandler);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        var privilegedCallCount = executor.ElevatedCommands.Count(call => call.RequiresSudo);
        Assert.True(privilegedCallCount > 1);
        Assert.Equal(privilegedCallCount, elevationHandler.RequestCount);
    }

    [Fact]
    public async Task Incorrect_password_reprompts_and_succeeds_on_the_corrected_retry_without_restarting_the_phase()
    {
        var executor = new FakeBootstrapTargetExecutor { HasRequiredPermissions = false, InSudoGroup = true };
        var registration = new FakeBootstrapRegistrationClient();
        var elevationHandler = new FakeBootstrapElevationHandler(
            new SudoPasswordPromptResult { Password = "wrong-password", Remember = true },
            new SudoPasswordPromptResult { Password = executor.CorrectSudoPassword, Remember = true });
        var orchestrator = CreateOrchestrator(executor, registration, elevationHandler);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        Assert.Equal(2, elevationHandler.RequestCount);
        Assert.Equal([false, true], elevationHandler.RetryFlagsSeen);
        Assert.Equal(11, report.Phases.Count);
        Assert.All(report.Phases, phase => Assert.True(phase.Succeeded));
        // The wrong password was tried exactly once and then discarded — never reused.
        Assert.Single(executor.ElevatedCommands, call => call.Password == "wrong-password");
    }

    [Fact]
    public async Task Cancelling_the_password_prompt_fails_cleanly_and_leaves_earlier_completed_phases_untouched_on_resume()
    {
        var executor = new FakeBootstrapTargetExecutor { HasRequiredPermissions = false, InSudoGroup = true };
        var registration = new FakeBootstrapRegistrationClient();
        var cancellingHandler = new FakeBootstrapElevationHandler(new SudoPasswordPromptResult { Password = null });
        var target = BuildTarget();
        var orchestrator = CreateOrchestrator(executor, registration, cancellingHandler);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });

        Assert.False(report.OverallSuccess);
        Assert.Equal(BootstrapPhaseOutcome.Completed, report.Phases.Single(p => p.Phase == BootstrapPhaseName.Preflight).Outcome);
        Assert.Equal(BootstrapPhaseName.DockerInstallation, report.FirstFailure!.Phase);

        // Resuming (a fresh orchestrator against the same on-disk state store, as a real "Retry"
        // click would do) must not re-run Preflight — only DockerInstallation onward.
        var successfulHandler = new FakeBootstrapElevationHandler(
            new SudoPasswordPromptResult { Password = executor.CorrectSudoPassword, Remember = true });
        var resumedOrchestrator = CreateOrchestrator(executor, registration, successfulHandler);
        var resumedReport = await resumedOrchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });

        Assert.True(resumedReport.OverallSuccess);
        Assert.Equal(BootstrapPhaseOutcome.AlreadySatisfied, resumedReport.Phases.Single(p => p.Phase == BootstrapPhaseName.Preflight).Outcome);
        // Preflight's own os-release check ran exactly once across both attempts — never repeated
        // on resume, proving the cancelled elevation prompt did not force a full restart.
        Assert.Single(executor.ExecutedCommands, c => c.Contains("os-release", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RuntimeDirectoryCreation_still_fixes_ownership_when_the_profile_directory_already_existed()
    {
        // Real-VPS regression: a target bootstrapped by an older build (or a previous attempt before
        // this ownership fix existed) can already have its profile directory present — and, worse,
        // stale root-owned files sitting under runtimeParent (e.g. a runtime-agent-extracted/ that a
        // fully-elevated old extract step created). If chown only ran on first creation, a resumed
        // run would never fix that, and the later unprivileged extract/upload steps would fail with
        // "Permission denied" trying to overwrite those root-owned files. Chown must run every time
        // this phase actually executes, not just when the directory is freshly created.
        var executor = new FakeBootstrapTargetExecutor { HasRequiredPermissions = false, InSudoGroup = true };
        executor.ExistingRemotePaths.Add("/opt/aovias/config/profiles/demo");
        var registration = new FakeBootstrapRegistrationClient();
        var elevationHandler = new FakeBootstrapElevationHandler(
            new SudoPasswordPromptResult { Password = executor.CorrectSudoPassword, Remember = true });
        var orchestrator = CreateOrchestrator(executor, registration, elevationHandler);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        // Sprint M7.x semantics: a pre-existing profile directory with no recorded checkpoint means
        // the repair pass genuinely runs (Completed, honestly — it re-applied ownership and
        // permissions), rather than being skipped as merely "already satisfied" on the strength of
        // the directory existing. The chown below is exactly that repair work happening.
        Assert.Equal(BootstrapPhaseOutcome.Completed, report.Phases.Single(p => p.Phase == BootstrapPhaseName.RuntimeDirectoryCreation).Outcome);
        Assert.Contains(executor.ElevatedCommands, call => call.Command.StartsWith("chown -R", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Unavailable_privilege_mode_blocks_at_validate_privileges_without_ever_prompting()
    {
        // Sprint M7.x.2: privilege-mode detection/blocking is ValidatePrivileges' own stage now,
        // ahead of Preflight — this proves the block happens there, correctly attributed, and that
        // Preflight's own (irrelevant, since this run never reaches it) checks are never attempted.
        var executor = new FakeBootstrapTargetExecutor { HasRequiredPermissions = false, InSudoGroup = false };
        var registration = new FakeBootstrapRegistrationClient();
        var elevationHandler = new FakeBootstrapElevationHandler(); // must never be asked — ValidatePrivileges should block first
        var orchestrator = CreateOrchestrator(executor, registration, elevationHandler);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.False(report.OverallSuccess);
        Assert.Equal(BootstrapPhaseName.ValidatePrivileges, report.FirstFailure!.Phase);
        Assert.Contains("Required Permissions", report.FirstFailure.Message);
        Assert.Equal(0, elevationHandler.RequestCount);
    }

    private BootstrapOrchestrator CreateOrchestrator(
        FakeBootstrapTargetExecutor executor,
        FakeBootstrapRegistrationClient registration,
        FakeBootstrapElevationHandler elevationHandler)
        => new(
            executor,
            registration,
            new BootstrapStateStore(workingDirectory),
            NullLogger<BootstrapOrchestrator>.Instance,
            healthVerificationMaxAttempts: 3,
            healthVerificationPollDelayMs: 0,
            elevationHandler: elevationHandler,
            connectRetryDelayMs: 0);

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
