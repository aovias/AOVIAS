using Aovias.Bootstrap.Contracts;
using Aovias.Deployment.Contracts;
using Aovias.Release.Contracts.RemoteDeployment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Aovias.Bootstrap.Core.Tests;

/// <summary>
/// Sprint M4.1 regression tests for the Zero-Touch Environment Bootstrap orchestrator. Uses
/// <see cref="FakeBootstrapTargetExecutor"/>/<see cref="FakeBootstrapRegistrationClient"/> (no SSH,
/// no real VPS, no real HTTP) so phase-sequencing, idempotency, and resumability logic is exercised
/// deterministically — consistent with how <c>PlatformVerificationPipelineTests</c> (Sprint M3.3)
/// fakes <c>IDotNetCliExecutor</c> for the same reason. Uses the real, file-backed
/// <see cref="BootstrapStateStore"/> (against a throwaway temp directory) rather than a fake state
/// store, so the idempotency/resume mechanism itself — not just the orchestrator's use of an
/// interface — gets genuine coverage.
/// </summary>
public sealed class BootstrapOrchestratorTests : IDisposable
{
    private readonly string workingDirectory = Path.Combine(Path.GetTempPath(), "m4.1-bootstrap-tests", Guid.NewGuid().ToString("N"));

    public BootstrapOrchestratorTests() => Directory.CreateDirectory(workingDirectory);

    public void Dispose()
    {
        if (Directory.Exists(workingDirectory))
        {
            Directory.Delete(workingDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task BootstrapAsync_runs_every_phase_in_order_and_succeeds_against_a_bare_target()
    {
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        Assert.Equal(11, report.Phases.Count);
        Assert.Equal(
            Enum.GetValues<BootstrapPhaseName>().OrderBy(p => (int)p),
            report.Phases.Select(p => p.Phase));
        Assert.All(report.Phases, phase => Assert.Equal(BootstrapPhaseOutcome.Completed, phase.Outcome));
        Assert.NotNull(report.ReportPath);
        Assert.True(File.Exists(report.ReportPath));
    }

    [Fact]
    public async Task BootstrapAsync_marks_docker_and_network_phases_already_satisfied_when_present()
    {
        var executor = new FakeBootstrapTargetExecutor { DockerInstalled = true };
        executor.ExistingDockerNetworks.Add("aovias-net");
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        var dockerPhase = report.Phases.Single(p => p.Phase == BootstrapPhaseName.DockerInstallation);
        var networkPhase = report.Phases.Single(p => p.Phase == BootstrapPhaseName.DockerNetworkCreation);
        Assert.Equal(BootstrapPhaseOutcome.AlreadySatisfied, dockerPhase.Outcome);
        Assert.Equal(BootstrapPhaseOutcome.AlreadySatisfied, networkPhase.Outcome);
        Assert.DoesNotContain(executor.ExecutedCommands, c => c.Contains("get.docker.com", StringComparison.Ordinal));
        Assert.DoesNotContain(executor.ExecutedCommands, c => c.StartsWith("docker network create", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BootstrapAsync_upgrades_runtime_agent_when_release_library_version_is_newer_than_deployed()
    {
        var executor = new FakeBootstrapTargetExecutor { AgentAlreadyRunning = true, ContainerHealthy = true, DeployedRuntimeAgentVersion = "1.0.1" };
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);
        var target = BuildTarget(runtimeAgentArtifactVersion: "1.0.2");

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        var installPhase = report.Phases.Single(p => p.Phase == BootstrapPhaseName.RuntimeAgentInstallation);
        Assert.Equal(BootstrapPhaseOutcome.Completed, installPhase.Outcome);
        Assert.NotEmpty(executor.UploadedBinaryPaths);
    }

    [Fact]
    public async Task BootstrapAsync_skips_runtime_agent_reinstall_when_deployed_version_matches_release_library_selection()
    {
        var executor = new FakeBootstrapTargetExecutor { AgentAlreadyRunning = true, ContainerHealthy = true, DeployedRuntimeAgentVersion = "1.0.2" };
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);
        var target = BuildTarget(runtimeAgentArtifactVersion: "1.0.2");

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        var installPhase = report.Phases.Single(p => p.Phase == BootstrapPhaseName.RuntimeAgentInstallation);
        Assert.Equal(BootstrapPhaseOutcome.AlreadySatisfied, installPhase.Outcome);
        Assert.Empty(executor.UploadedBinaryPaths);
    }

    [Fact]
    public async Task BootstrapAsync_is_idempotent_on_a_second_run_against_an_already_bootstrapped_target()
    {
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);
        var target = BuildTarget();

        var first = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });
        Assert.True(first.OverallSuccess);

        var commandCountAfterFirstRun = executor.ExecutedCommands.Count;
        var heartbeatCallsAfterFirstRun = registration.HeartbeatCallCount;

        var second = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });

        Assert.True(second.OverallSuccess);
        // Sprint M7.x semantics: a second run re-DISCOVERS remote state everywhere (read-only
        // inspections — docker --version, network inspect, stat, checksum comparisons) but changes
        // nothing an already-healthy target has. Configuration generation is AlreadySatisfied by
        // checksum proof (every remote file matches what this build generates — stronger than
        // "re-uploaded blindly"). Connect, ValidatePrivileges (session properties, re-verified every
        // run by design), in-process contract validation, registration (idempotent end-to-end by
        // design — the agent itself re-registers on every restart, the Fleet Registry upserts by
        // BaseUrl — and verification needs the API key it issues; see RegistrationStage remarks),
        // and end-to-end health verification are the stages that always genuinely re-run.
        var alwaysRerun = new[] { BootstrapPhaseName.Connect, BootstrapPhaseName.ValidatePrivileges, BootstrapPhaseName.ValidateRuntimeConfiguration, BootstrapPhaseName.RuntimeAgentRegistration, BootstrapPhaseName.BootstrapVerification };
        Assert.All(
            second.Phases.Where(phase => !alwaysRerun.Contains(phase.Phase)),
            phase => Assert.Equal(BootstrapPhaseOutcome.AlreadySatisfied, phase.Outcome));
        Assert.All(alwaysRerun, phase => Assert.Equal(BootstrapPhaseOutcome.Completed, second.Phases.Single(p => p.Phase == phase).Outcome));

        // Idempotency is judged by side effects, not command counts: the second run must issue NO
        // mutating command — no install, no network create, no permission rewrite, no extraction
        // cleanup, no image load, no container (re)creation.
        var secondRunCommands = executor.ExecutedCommands.Skip(commandCountAfterFirstRun).ToList();
        Assert.DoesNotContain(secondRunCommands, c => c.Contains("get.docker.com", StringComparison.Ordinal));
        Assert.DoesNotContain(secondRunCommands, c => c.StartsWith("docker network create", StringComparison.Ordinal));
        Assert.DoesNotContain(secondRunCommands, c => c.StartsWith("chmod ", StringComparison.Ordinal) || c.StartsWith("chown ", StringComparison.Ordinal));
        Assert.DoesNotContain(secondRunCommands, c => c.StartsWith("rm -rf ", StringComparison.Ordinal));
        Assert.DoesNotContain(secondRunCommands, c => c.Contains("docker load", StringComparison.Ordinal));
        Assert.DoesNotContain(secondRunCommands, c => c.Contains("up -d", StringComparison.Ordinal));
        // Registration re-affirmed (idempotent upsert), and health WAS re-verified with the
        // re-issued key — that is the point.
        Assert.Equal(2, registration.AgentRegistrationCallCount);
        Assert.Equal(heartbeatCallsAfterFirstRun + 1, registration.HeartbeatCallCount);
        Assert.Equal("fake-api-key", registration.LastHeartbeatApiKey);
    }

    [Fact]
    public async Task BootstrapAsync_resumes_from_the_first_incomplete_phase_after_a_simulated_interruption()
    {
        // Simulate a process that was killed after completing the first three phases — the HOST
        // reflects that work (Docker installed, network created), and the local checkpoints record
        // it. Both matter now: checkpoints alone no longer skip anything — remote discovery is
        // authoritative, so the fake host must actually be in the state the checkpoints claim
        // (see BootstrapAsync_reinstalls_docker_when_the_checkpoint_lies... for the drift case).
        var executor = new FakeBootstrapTargetExecutor { DockerInstalled = true };
        executor.ExistingDockerNetworks.Add("aovias-net");
        var registration = new FakeBootstrapRegistrationClient();
        var stateStore = new BootstrapStateStore(workingDirectory);
        var target = BuildTarget();

        await stateStore.MarkCompletedAsync(target.StableIdentity, BootstrapPhaseName.Preflight, CancellationToken.None);
        await stateStore.MarkCompletedAsync(target.StableIdentity, BootstrapPhaseName.DockerInstallation, CancellationToken.None);
        await stateStore.MarkCompletedAsync(target.StableIdentity, BootstrapPhaseName.DockerNetworkCreation, CancellationToken.None);

        var orchestrator = new BootstrapOrchestrator(executor, registration, stateStore, NullLogger<BootstrapOrchestrator>.Instance, healthVerificationMaxAttempts: 3, healthVerificationPollDelayMs: 0, containerHealthMaxAttempts: 3, containerHealthPollDelayMs: 0);
        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        Assert.Equal(BootstrapPhaseOutcome.AlreadySatisfied, report.Phases.Single(p => p.Phase == BootstrapPhaseName.Preflight).Outcome);
        Assert.Equal(BootstrapPhaseOutcome.AlreadySatisfied, report.Phases.Single(p => p.Phase == BootstrapPhaseName.DockerInstallation).Outcome);
        Assert.Equal(BootstrapPhaseOutcome.AlreadySatisfied, report.Phases.Single(p => p.Phase == BootstrapPhaseName.DockerNetworkCreation).Outcome);
        Assert.Equal(BootstrapPhaseOutcome.Completed, report.Phases.Single(p => p.Phase == BootstrapPhaseName.RuntimeDirectoryCreation).Outcome);
        Assert.Equal(BootstrapPhaseOutcome.Completed, report.Phases.Single(p => p.Phase == BootstrapPhaseName.BootstrapVerification).Outcome);
        // The resumed phases never touched the (already-"prepared") fake target.
        Assert.DoesNotContain(executor.ExecutedCommands, c => c.Contains("get.docker.com", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BootstrapAsync_force_re_runs_every_phase_even_when_previously_recorded_complete()
    {
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);
        var target = BuildTarget();

        await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });
        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory, Force = true });

        Assert.True(report.OverallSuccess);
        // Docker/network were already present on the fake target (set up by the first run), so a
        // forced re-run correctly reports them AlreadySatisfied again rather than Completed — Force
        // means "don't trust the state file", not "pretend nothing exists".
        Assert.Equal(BootstrapPhaseOutcome.AlreadySatisfied, report.Phases.Single(p => p.Phase == BootstrapPhaseName.DockerInstallation).Outcome);
    }

    [Fact]
    public async Task BootstrapAsync_stops_at_the_first_failed_phase_and_does_not_attempt_later_phases()
    {
        var executor = new FakeBootstrapTargetExecutor { DockerInstallCommandFails = true };
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.False(report.OverallSuccess);
        // Connect, ValidatePrivileges, Preflight all succeed before DockerInstallation fails.
        Assert.Equal(4, report.Phases.Count);
        Assert.Equal(BootstrapPhaseName.DockerInstallation, report.FirstFailure!.Phase);
        Assert.NotNull(report.FirstFailure.RootCause);
        Assert.Equal(0, registration.HeartbeatCallCount);
    }

    [Fact]
    public async Task BootstrapAsync_fails_preflight_when_connection_cannot_be_established()
    {
        var executor = new FakeBootstrapTargetExecutor { ConnectionSucceeds = false };
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.False(report.OverallSuccess);
        // Connect is its own first-class stage (Sprint M7.x.2) — a connection failure is reported
        // as Connect failing, with a recorded (bounded, zero-delay in this test) retry attempt, not
        // as a bare "Preflight" failure with no attribution of what actually went wrong.
        Assert.Single(report.Phases);
        Assert.Equal(BootstrapPhaseName.Connect, report.Phases[0].Phase);
        // The repair attempt itself ran (retrying the probe) but didn't succeed — the connection
        // stays down for the whole test — so RetryCount (which only increments after a repair that
        // actually succeeded, meaning a re-Execute/re-Verify happens) stays 0.
        Assert.Equal(0, report.Phases[0].RetryCount);
        Assert.Single(report.Phases[0].RepairAttempts);
    }

    [Fact]
    public async Task BootstrapAsync_retries_agent_registration_when_the_container_is_not_listening_yet()
    {
        // Real-VPS regression: `docker compose up -d` returns as soon as the container has started,
        // not once the app inside has finished booting and bound its port — an immediate, unretried
        // registration attempt can time out against a container that would have answered moments
        // later. This proves the retry loop recovers rather than failing on the first transient
        // connection timeout.
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient { AgentRegistrationFailuresBeforeSuccess = 2 };
        var orchestrator = new BootstrapOrchestrator(
            executor, registration, new BootstrapStateStore(workingDirectory), NullLogger<BootstrapOrchestrator>.Instance,
            healthVerificationMaxAttempts: 3, healthVerificationPollDelayMs: 0,
            registrationMaxAttempts: 5, registrationRetryDelayMs: 0,
            containerHealthMaxAttempts: 3, containerHealthPollDelayMs: 0);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        Assert.Equal(3, registration.AgentRegistrationCallCount);
    }

    [Fact]
    public async Task BootstrapAsync_registers_the_agent_and_then_the_fleet_registry_with_the_issued_api_key()
    {
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);
        var target = BuildTarget();

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        Assert.NotNull(registration.LastAgentRegistrationRequest);
        Assert.Equal(target.RegistrationToken, registration.LastAgentRegistrationRequest!.RegistrationToken);
        Assert.Equal(target.EnvironmentName, registration.LastAgentRegistrationRequest.EnvironmentName);
        Assert.NotNull(registration.LastFleetRegistrationRequest);
        Assert.Equal("fake-api-key", registration.LastFleetRegistrationRequest!.ApiKey);
        Assert.Equal(target.EnvironmentName, registration.LastFleetRegistrationRequest.Name);
    }

    [Fact]
    public async Task BootstrapAsync_fails_verification_when_heartbeat_reports_unhealthy()
    {
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient { HeartbeatHealthStatus = "Degraded" };
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.False(report.OverallSuccess);
        Assert.Equal(BootstrapPhaseName.BootstrapVerification, report.FirstFailure!.Phase);
    }

    [Fact]
    public async Task BootstrapAsync_installs_the_runtime_agent_via_load_and_compose_up_not_a_bare_docker_run()
    {
        // Sprint G1 pre-flight regression: RunRuntimeAgentInstallationAsync used to upload
        // RuntimeAgentArtifactPath (a .aovdeploy package) and pass its raw file path directly as
        // `docker run`'s image argument — which cannot work, since `docker run` expects an image
        // already in the local image store, not an arbitrary file. This proves the fixed sequence
        // (extract, `docker load` the embedded image tar, `docker compose up -d`) is what actually
        // gets issued, mirroring the pre-existing `deploy` verb's own proven pattern.
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        Assert.DoesNotContain(executor.ExecutedCommands, c => c.StartsWith("docker run", StringComparison.Ordinal));
        Assert.Contains(executor.ExecutedCommands, c => c.Contains("docker load", StringComparison.Ordinal));
        Assert.Contains(executor.ExecutedCommands, c => c.Contains("docker compose", StringComparison.Ordinal) && c.Contains("up -d", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BootstrapAsync_recreates_the_container_and_refreshes_permissions_immediately_before_starting_it()
    {
        // Real-VPS regression: `docker compose up -d` is idempotent against an unchanged service
        // definition — it will not recreate an existing container just because it is crash-looping,
        // leaving Docker's own restart policy retrying the *same* stale container indefinitely even
        // after host-side permissions were corrected. And Stage 4's own chmod only runs when that
        // phase actually executes this attempt, which a plain Retry against a target whose Stage 4
        // already succeeded on an older build will keep skipping. Stage 7 must not depend on either.
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        Assert.Contains(executor.ExecutedCommands, c => c.Contains("docker compose", StringComparison.Ordinal) && c.Contains("--force-recreate", StringComparison.Ordinal));
        var chmodIndex = executor.ExecutedCommands.FindIndex(c => c.StartsWith("chmod -R 0777 ", StringComparison.Ordinal));
        var composeUpIndex = executor.ExecutedCommands.FindIndex(c => c.Contains("--force-recreate", StringComparison.Ordinal));
        Assert.True(chmodIndex >= 0, "Expected a permission refresh immediately before starting the container.");
        Assert.True(chmodIndex < composeUpIndex, "Permissions must be refreshed before the container is (re)created, not after.");
    }

    [Fact]
    public async Task BootstrapAsync_opens_runtime_directory_permissions_for_the_containers_own_uid_even_when_root()
    {
        // Real-VPS regression: the Runtime Agent container runs as a fixed non-root UID baked into
        // the image (APP_UID/PlatformRuntimeDefaults.ContainerUserId), independent of whichever user
        // Bootstrap itself connects as. A root-run Bootstrap chowns nothing (no elevation needed) and
        // mkdir's default mode never grants that UID write access to the rw-mounted deployments/
        // logs/packages directories — the container crash-looped with "Access ... is denied." even
        // though Directory Creation had already reported success. The chmod fix must run
        // unconditionally, not just for non-root/sudo targets.
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);
        var target = new BootstrapTarget
        {
            Host = "vps-demo.example.test",
            Username = "root",
            Password = "unused-in-tests",
            ProfileName = "demo",
            EnvironmentName = "Demo VPS",
            EnvironmentType = EnvironmentType.Demo,
            RegistrationToken = "demo-vps-registration-token",
            PublicUrl = "http://vps-demo.example.test:7080",
            CommandCenterBaseUrl = "https://command-center.example.test",
            RuntimeAgentArtifactPath = "/local/path/to/agent-publish-output",
            RemoteConfigRoot = "/opt/aovias/config",
            DockerNetworkName = "aovias-net"
        };

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        Assert.Contains(executor.ExecutedCommands, c => c.StartsWith("chmod -R 0777 ", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BootstrapAsync_clears_the_extraction_directory_before_every_extraction_regardless_of_prior_state()
    {
        // Real-VPS regression: a prior attempt (an older build, or one where Stage 4's ownership fix
        // never got a chance to run because the state store already had it recorded) can leave
        // /opt/.../runtime-agent-extracted populated with root-owned files. Relying on Stage 4's
        // chown to have run recently is fragile — Stage 6 must clear its own extraction directory
        // unconditionally, every time it actually extracts, regardless of what Stage 4 did this run.
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        var rmIndex = executor.ExecutedCommands.FindIndex(c => c.StartsWith("rm -rf ", StringComparison.Ordinal) && c.Contains("runtime-agent-extracted", StringComparison.Ordinal));
        var mkdirIndex = executor.ExecutedCommands.FindIndex(c => c.StartsWith("mkdir -p ", StringComparison.Ordinal) && c.Contains("runtime-agent-extracted", StringComparison.Ordinal));
        Assert.True(rmIndex >= 0, "Expected an rm -rf of the extraction directory before extraction.");
        Assert.True(mkdirIndex > rmIndex, "The cleanup must run strictly before the (re)creation/extraction step.");
    }

    [Fact]
    public async Task BootstrapAsync_detects_a_crash_looping_container_and_attaches_automatic_diagnostics_instead_of_a_bare_connection_failure()
    {
        // Reproduces the real-world defect this fix targets: docker compose up succeeds, but the
        // container itself crash-loops (RestartCount climbing, ExitCode 139) because its
        // configuration was invalid — Bootstrap must never report this as "Registration failed:
        // connection timed out" the way it did before, since Registration must not even be attempted
        // against an unhealthy container.
        var executor = new FakeBootstrapTargetExecutor { SimulateCrashLoop = true };
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.False(report.OverallSuccess);
        var installPhase = report.Phases.Single(p => p.Phase == BootstrapPhaseName.RuntimeAgentInstallation);
        Assert.Equal(BootstrapPhaseOutcome.Failed, installPhase.Outcome);
        Assert.Contains("Container Crash", installPhase.Message, StringComparison.Ordinal);

        // Automatic diagnostics — never "only Connection failed."
        Assert.Contains("docker logs", installPhase.RootCause, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RestartCount", installPhase.RootCause, StringComparison.Ordinal);
        Assert.Contains("ExitCode", installPhase.RootCause, StringComparison.Ordinal);
        Assert.Contains("docker compose ps", installPhase.RootCause, StringComparison.OrdinalIgnoreCase);

        // Registration must never even be attempted against a container that never became healthy.
        Assert.DoesNotContain(report.Phases, p => p.Phase == BootstrapPhaseName.RuntimeAgentRegistration);
        Assert.Equal(0, registration.HeartbeatCallCount);
        Assert.Null(registration.LastAgentRegistrationRequest);
    }

    [Fact]
    public async Task BootstrapAsync_validates_runtime_configuration_before_ever_starting_docker()
    {
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        var validationPhase = report.Phases.Single(p => p.Phase == BootstrapPhaseName.ValidateRuntimeConfiguration);
        Assert.Equal(BootstrapPhaseOutcome.Completed, validationPhase.Outcome);

        // ValidateRuntimeConfiguration is pure, local validation — it must never touch the target host.
        var indexOfValidation = report.Phases.ToList().FindIndex(p => p.Phase == BootstrapPhaseName.ValidateRuntimeConfiguration);
        var indexOfInstall = report.Phases.ToList().FindIndex(p => p.Phase == BootstrapPhaseName.RuntimeAgentInstallation);
        Assert.True(indexOfValidation < indexOfInstall, "Configuration must be validated before Runtime Startup ever begins.");
    }

    [Fact]
    public async Task BootstrapAsync_uploads_a_dotenv_file_the_compose_file_needs_before_starting_it()
    {
        // Real-VPS regression: docker-compose.yml's volumes/environment sections reference AOVIAS_*
        // tokens (e.g. "${AOVIAS_HOST_DEPLOYMENTS_ROOT}:/opt/aovias/deployments"), and Docker Compose
        // only auto-loads a file literally named ".env" from the same directory as the compose file.
        // Before this fix, nothing ever wrote one, so every token resolved blank and `docker compose
        // up` failed with "invalid spec: :/opt/aovias/deployments: empty section between colons".
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);
        var target = BuildTarget();

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        var envPath = executor.UploadedTextFiles.Keys.SingleOrDefault(path => path.EndsWith("/.env", StringComparison.Ordinal));
        Assert.NotNull(envPath);
        var envContent = executor.UploadedTextFiles[envPath!];

        Assert.Contains(executor.ExecutedCommands, c => c.Contains("docker compose", StringComparison.Ordinal) && c.Contains("up -d", StringComparison.Ordinal));

        foreach (var requiredKey in new[]
        {
            "AOVIAS_CONFIG_ROOT", "AOVIAS_LOGS_ROOT", "AOVIAS_ACTIVE_PROFILE", "AOVIAS_DEPLOYMENTS_ROOT",
            "AOVIAS_HOST_DEPLOYMENTS_ROOT", "AOVIAS_HOST_CONFIG_ROOT", "AOVIAS_HOST_LOGS_ROOT",
            "AOVIAS_HOST_PACKAGES_ROOT", "AOVIAS_DOCKER_NETWORK"
        })
        {
            var line = envContent.Split('\n').FirstOrDefault(l => l.StartsWith($"{requiredKey}=", StringComparison.Ordinal));
            Assert.NotNull(line);
            Assert.NotEqual($"{requiredKey}=", line!.Trim()); // never just the empty-string default that broke the real run
        }
    }

    [Fact]
    public async Task BootstrapAsync_fails_installation_when_the_uploaded_package_fails_checksum_verification()
    {
        var executor = new FakeBootstrapTargetExecutor { RemoteFileChecksum = "0000000000000000000000000000000000000000000000000000000000000" };
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.False(report.OverallSuccess);
        Assert.Equal(BootstrapPhaseName.RuntimeAgentInstallation, report.FirstFailure!.Phase);
        Assert.Contains("checksum", report.FirstFailure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BootstrapAsync_preflight_reports_every_blocker_together_not_just_the_first()
    {
        // Sprint M7.x.2: privilege availability is ValidatePrivileges' own blocker now (a separate,
        // earlier stage — see the dedicated test below) so this test's "reports every blocker
        // together" claim is scoped to what Preflight itself still owns: disk and network.
        var executor = new FakeBootstrapTargetExecutor
        {
            AvailableDiskKb = 1024, // ~1 MB — far below the 5 GB minimum
            NetworkConnectivitySucceeds = false
        };
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.False(report.OverallSuccess);
        var preflight = report.Phases.Single(p => p.Phase == BootstrapPhaseName.Preflight);
        Assert.Contains("Disk Space", preflight.RootCause);
        Assert.Contains("Network Connectivity", preflight.RootCause);
    }

    [Fact]
    public async Task BootstrapAsync_validate_privileges_reports_its_own_blocker_as_its_own_stage()
    {
        // The direct counterpart of the test above: an unavailable privilege path is
        // ValidatePrivileges' own failure, distinctly attributed — not folded into Preflight's
        // blocker list, and not reached at all if Preflight's own checks would also have failed
        // (ValidatePrivileges now runs before Preflight).
        var executor = new FakeBootstrapTargetExecutor { HasRequiredPermissions = false, InSudoGroup = false };
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.False(report.OverallSuccess);
        Assert.Equal(BootstrapPhaseName.ValidatePrivileges, report.FirstFailure!.Phase);
        Assert.Contains("Required Permissions", report.FirstFailure.Message);
    }

    [Fact]
    public async Task BootstrapAsync_verification_retries_until_the_agent_reports_healthy()
    {
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient { HeartbeatHealthStatus = "Degraded", UnhealthyAttemptsBeforeRecovery = 1 };
        // 3 attempts / no delay (see CreateOrchestrator) — the agent reports healthy on attempt 2.
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        Assert.Equal(2, registration.HeartbeatCallCount);
    }

    [Fact]
    public async Task BootstrapAsync_fails_verification_when_signalr_negotiation_does_not_respond()
    {
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient { SignalRHealthy = false };
        var orchestrator = CreateOrchestrator(executor, registration);

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = BuildTarget(), WorkingDirectory = workingDirectory });

        Assert.False(report.OverallSuccess);
        Assert.Equal(BootstrapPhaseName.BootstrapVerification, report.FirstFailure!.Phase);
        Assert.Contains("SignalR", report.FirstFailure.Message);
    }

    [Fact]
    public void RuntimeConfigurationScaffoldBuilder_produces_the_flat_shape_DeploymentPathResolver_expects()
    {
        var target = BuildTarget();
        var json = RuntimeConfigurationScaffoldBuilder.BuildDeploymentHostSettingsJson(target);

        var settings = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            .Build()
            .Get<Aovias.Configuration.Settings.DeploymentHostSettings>();

        Assert.NotNull(settings);
        Assert.Equal(target.DockerNetworkName, settings!.DockerNetwork);
        // Real-world defect: ConfiguredDeploymentPathResolver reads host.HostConfigRoot/HostLogsRoot/
        // HostPackagesRoot as ITS OWN ConfigRoot/LogsRoot/PackagesRoot, and that resolver runs inside
        // the container — so these must be the container-internal canonical paths, not the physical
        // VPS bind-mount source path (target.RemoteConfigRoot), which was the actual bug: a previously
        // generated settings.deployment-host.json pointed the Runtime Agent's own storage-layout check
        // at a path never bind-mounted at that literal location inside its own filesystem.
        Assert.Equal(PlatformRuntimeDefaults.ConfigRoot, settings.ConfigRoot);
        Assert.Equal(PlatformRuntimeDefaults.LogsRoot, settings.LogsRoot);
        Assert.Equal(PlatformRuntimeDefaults.PackagesRoot, settings.PackagesRoot);
        Assert.Equal(PlatformRuntimeDefaults.ConfigRoot, settings.HostConfigRoot);
        Assert.Equal(PlatformRuntimeDefaults.LogsRoot, settings.HostLogsRoot);
        Assert.Equal(PlatformRuntimeDefaults.PackagesRoot, settings.HostPackagesRoot);
    }

    [Fact]
    public void RuntimeConfigurationScaffoldBuilder_produces_the_flat_shape_EnvironmentRegistrationSettings_expects()
    {
        var target = BuildTarget();
        var json = RuntimeConfigurationScaffoldBuilder.BuildEnvironmentRegistrationSettingsJson(target);

        var settings = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            .Build()
            .Get<Aovias.Configuration.Settings.EnvironmentRegistrationSettings>();

        Assert.NotNull(settings);
        Assert.True(settings!.Enabled);
        Assert.Equal(target.CommandCenterBaseUrl, settings.CommandCenterBaseUrl);
        Assert.Equal(target.RegistrationToken, settings.RegistrationToken);
        Assert.Equal(target.EnvironmentType.ToString(), settings.EnvironmentType);
    }

    [Fact]
    public async Task BootstrapAsync_uploads_environment_registration_settings_so_automatic_reregistration_activates_on_restart()
    {
        // Sprint M3.3 regression: before this fix, EnvironmentRegistrationSettings had no
        // settings.*.json mapping at all (ProfileConfigurationLoader.FileSectionMap) and Bootstrap
        // never generated the file even if one had existed — so a bootstrapped Runtime Agent's
        // EnvironmentRegistrationHostedService always saw Enabled=false on every restart after the
        // one-time bootstrap-phase registration. This proves the generated file both exists at the
        // path ProfileConfigurationLoader expects and carries the settings that activate the hosted
        // service.
        var executor = new FakeBootstrapTargetExecutor();
        var registration = new FakeBootstrapRegistrationClient();
        var orchestrator = CreateOrchestrator(executor, registration);
        var target = BuildTarget();

        var report = await orchestrator.BootstrapAsync(new BootstrapRequest { Target = target, WorkingDirectory = workingDirectory });

        Assert.True(report.OverallSuccess);
        var expectedPath = RuntimeConfigurationScaffoldBuilder.ResolveEnvironmentRegistrationRemotePath(target);
        Assert.True(executor.UploadedTextFiles.ContainsKey(expectedPath));

        var settings = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(executor.UploadedTextFiles[expectedPath])))
            .Build()
            .Get<Aovias.Configuration.Settings.EnvironmentRegistrationSettings>();
        Assert.NotNull(settings);
        Assert.True(settings!.Enabled);
        Assert.Equal(target.CommandCenterBaseUrl, settings.CommandCenterBaseUrl);
    }

    // Fast, deterministic test values for the health-verification polling loop (production defaults
    // are 10 attempts / 3s apart — appropriate for a real container warming up, not for a unit test).
    private BootstrapOrchestrator CreateOrchestrator(FakeBootstrapTargetExecutor executor, FakeBootstrapRegistrationClient registration)
        => new(executor, registration, new BootstrapStateStore(workingDirectory), NullLogger<BootstrapOrchestrator>.Instance, healthVerificationMaxAttempts: 3, healthVerificationPollDelayMs: 0, containerHealthMaxAttempts: 3, containerHealthPollDelayMs: 0, connectRetryDelayMs: 0);

    private static BootstrapTarget BuildTarget(string? runtimeAgentArtifactVersion = null) => new()
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
        RuntimeAgentArtifactVersion = runtimeAgentArtifactVersion,
        RemoteConfigRoot = "/opt/aovias/config",
        DockerNetworkName = "aovias-net"
    };
}
