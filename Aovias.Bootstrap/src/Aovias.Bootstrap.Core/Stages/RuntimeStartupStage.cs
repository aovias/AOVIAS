using System.Text.Json;
using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core.Stages;

/// <summary>
/// Runtime Startup: uploads the Runtime Agent package (integrity-verified), extracts it, supplies
/// the compose <c>.env</c>, loads the embedded image, prepares every bind-mount directory through
/// the centralized <see cref="PermissionRepairService"/>, force-recreates the container, and only
/// reports success once the container is genuinely running and healthy ("container start is NOT
/// success"). Discovery accepts an already-running-and-healthy container of any origin; a merely
/// existing (stopped, crash-looping, unhealthy) container is NOT satisfied and gets recreated.
/// On a health failure, one bounded self-heal round (permission re-repair + force-recreate) runs
/// before failing with full automatic diagnostics.
/// </summary>
public sealed class RuntimeStartupStage : BootstrapStage
{
    public override BootstrapPhaseName Phase => BootstrapPhaseName.RuntimeAgentInstallation;

    public override async Task<StageDiscovery> DiscoverAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        if (!await context.Docker.IsRuntimeAgentRunningAsync(Phase, context.Observer, cancellationToken))
        {
            return StageDiscovery.NotSatisfied("Runtime Agent container is not running.");
        }

        var state = await context.Docker.GetContainerStateAsync(Phase, context.Observer, cancellationToken);
        if (state is not { IsRunningAndHealthy: true })
        {
            return StageDiscovery.NotSatisfied($"Runtime Agent container exists but is not healthy ({(state is null ? "state unreadable" : $"status={state.Status}, health={state.Health}, restarts={state.RestartCount}")}) — it will be recreated.");
        }

        var requiredVersion = context.Target.RuntimeAgentArtifactVersion;
        if (!string.IsNullOrWhiteSpace(requiredVersion))
        {
            var deployedVersion = await TryReadDeployedRuntimeAgentVersionAsync(context, cancellationToken);
            if (!string.IsNullOrWhiteSpace(deployedVersion)
                && !string.Equals(deployedVersion, requiredVersion, StringComparison.OrdinalIgnoreCase))
            {
                return StageDiscovery.NotSatisfied(
                    $"Runtime Agent {deployedVersion} is running but {requiredVersion} is required from the Release Library — it will be upgraded.");
            }
        }

        return StageDiscovery.SatisfiedBecause($"Runtime Agent container is already running and healthy (status={state.Status}, health={state.Health}).");
    }

    private static async Task<string?> TryReadDeployedRuntimeAgentVersionAsync(
        BootstrapStageContext context,
        CancellationToken cancellationToken)
    {
        var runtimeParent = PermissionRepairService.ResolveRuntimeParent(context.Target);
        var manifestPath = $"{runtimeParent}/packages/runtime-agent-extracted/manifest.json";
        var read = await context.Engine.RunAsync($"cat \"{manifestPath}\" 2>/dev/null", cancellationToken);
        if (!read.Succeeded || string.IsNullOrWhiteSpace(read.StandardOutput))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(read.StandardOutput);
            if (document.RootElement.TryGetProperty("Version", out var versionElement)
                && versionElement.ValueKind == JsonValueKind.String)
            {
                return versionElement.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    public override Task<StageValidation> ValidateAsync(BootstrapStageContext context, CancellationToken cancellationToken)
        => Task.FromResult(string.IsNullOrWhiteSpace(context.Target.RuntimeAgentArtifactPath)
            ? StageValidation.Blocked("Runtime Package Availability: no Runtime Agent package path was supplied.")
            : StageValidation.Ok);

    public override async Task<StageOutcome> ExecuteAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var target = context.Target;
        var runtimeParent = PermissionRepairService.ResolveRuntimeParent(target);
        var remotePackagePath = $"{runtimeParent}/packages/runtime-agent.aovdeploy";
        var extractionPath = $"{runtimeParent}/packages/runtime-agent-extracted";

        // Upload with real integrity verification: compute the checksum locally before upload, ask
        // the remote host to compute the same checksum on what it actually received, and compare.
        // An empty/unparseable remote reading is treated as inconclusive (older BusyBox-style hosts
        // without `sha256sum`) rather than a hard failure — only a genuine mismatch blocks the run.
        context.Observer?.OnPhaseProgress(Phase, "Uploading Runtime Agent package...", null);
        var localChecksum = await context.Executor.ComputeLocalFileChecksumAsync(target.RuntimeAgentArtifactPath, cancellationToken);
        await context.Executor.UploadFileAsync(target.RuntimeAgentArtifactPath, remotePackagePath, cancellationToken);

        var checksumCheck = await context.Engine.RunAsync($"sha256sum \"{remotePackagePath}\" | awk '{{print $1}}'", cancellationToken);
        var remoteChecksum = checksumCheck.StandardOutput.Trim();
        if (checksumCheck.Succeeded && !string.IsNullOrWhiteSpace(remoteChecksum)
            && !string.Equals(remoteChecksum, localChecksum, StringComparison.OrdinalIgnoreCase))
        {
            return StageOutcome.Fail(
                "Uploaded Runtime Agent package failed checksum verification — the upload may be corrupted.",
                $"Expected sha256:{localChecksum}, got sha256:{remoteChecksum}.");
        }

        // Elevated cleanup, unconditionally, before every extraction: a prior attempt can have left
        // this exact directory populated with root-owned files, which an unprivileged `unzip -o`/
        // `tar -x` can never overwrite. Removing and letting the very next (unprivileged) mkdir
        // recreate it guarantees a clean, SSH-user-owned directory regardless of prior attempts.
        var cleanup = await context.Engine.RunPrivilegedAsync($"rm -rf \"{extractionPath}\"", Phase, context.Observer, cancellationToken);
        if (!cleanup.Succeeded)
        {
            return StageOutcome.Fail($"Could not clear the previous extraction directory '{extractionPath}'.", cleanup.StandardError);
        }

        // Unprivileged: the directory is freshly (re)created by the SSH user itself — only the
        // Docker daemon calls further down need elevation.
        var extract = await context.Engine.RunAsync(
            $"mkdir -p \"{extractionPath}\" && "
            + $"(unzip -o \"{remotePackagePath}\" -d \"{extractionPath}\" || tar -xf \"{remotePackagePath}\" -C \"{extractionPath}\")",
            cancellationToken);
        if (!extract.Succeeded)
        {
            return StageOutcome.Fail("Runtime Agent package could not be extracted.", extract.StandardError);
        }

        // Docker Compose only auto-loads a file literally named `.env` next to the compose file,
        // and nothing upstream of Bootstrap ever generates one for a deployed target — without it,
        // every AOVIAS_* substitution is silently blank and compose fails on the first empty
        // bind-mount spec.
        var envContent = RuntimeConfigurationScaffoldBuilder.BuildRuntimeAgentEnvFile(target);
        await context.Executor.UploadTextFileAsync($"{extractionPath}/.env", envContent, cancellationToken);

        var imageTarPath = $"{extractionPath}/service-image.tar";
        if (await context.Executor.RemotePathExistsAsync(imageTarPath, cancellationToken))
        {
            var load = await context.Docker.LoadImageAsync(imageTarPath, Phase, context.Observer, cancellationToken);
            if (!load.Succeeded)
            {
                return StageOutcome.Fail("Runtime Agent image could not be loaded into Docker.", load.StandardError);
            }
        }

        // Bind-mount preparation through the centralized permission engine, independent of whether
        // the directory stage ran this attempt — the container must never start against directories
        // whose state depends on another stage's checkpoint history.
        var permissions = await context.Permissions.EnsureBindMountDirectoriesAsync(target, Phase, context.Observer, cancellationToken);
        if (!permissions.Succeeded)
        {
            return StageOutcome.Fail(permissions.Evidence, permissions.Error);
        }

        var start = await context.Docker.ComposeUpAsync($"{extractionPath}/docker-compose.yml", Phase, context.Observer, cancellationToken);
        if (!start.Succeeded)
        {
            return StageOutcome.Fail("Runtime Startup Failed — `docker compose up` did not complete successfully.", start.StandardError);
        }

        // Best-effort local firewall opening: a hardened Ubuntu image often ships with ufw enabled
        // and only port 22 allowed. Deliberately a no-op (never fails the stage) when ufw is absent
        // or inactive; a hosting provider's own network-level firewall remains the operator's
        // infrastructure to configure.
        if (Uri.TryCreate(target.PublicUrl, UriKind.Absolute, out var publicUri) && publicUri.Port > 0)
        {
            await context.Engine.RunPrivilegedAsync(
                $"sh -c \"command -v ufw >/dev/null 2>&1 && ufw status | grep -q 'Status: active' && ufw allow {publicUri.Port}/tcp; true\"",
                Phase,
                context.Observer,
                cancellationToken);
        }

        return StageOutcome.Ok("Runtime Agent artifact was installed and the container was started.");
    }

    /// <summary>
    /// Registration must only execute once the Runtime Agent is actually healthy — a docker-level
    /// check (container running + Docker's own healthcheck, which already probes the API's own
    /// <c>/health</c> endpoint per the compose file) done here over the same SSH session, rather
    /// than leaving Registration's own HTTP call as the first signal that anything is wrong.
    /// </summary>
    public override async Task<StageOutcome> VerifyAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        var wait = await context.Docker.WaitForHealthyAsync(
            context.Tunables.ContainerHealthMaxAttempts,
            context.Tunables.ContainerHealthPollDelayMs,
            Phase,
            context.Observer,
            cancellationToken);

        if (wait.Healthy)
        {
            return StageOutcome.Ok($"Runtime Agent container is running and healthy ({wait.Reason}).");
        }

        return StageOutcome.Fail(
            wait.IsCrashLoop
                ? "Container Crash — the Runtime Agent container is repeatedly restarting."
                : "Health Check Failed — the Runtime Agent container did not become healthy in time.",
            wait.Reason);
    }

    /// <summary>
    /// Self-heal before failing: for an unhealthy/crash-looping container, one round of bind-mount
    /// permission re-repair plus a force-recreate (a stale container created before a host-side fix
    /// never picks that fix up on its own — real-world observed). For an execution failure, only a
    /// corrupted upload is worth a retry; every other execution failure is deterministic.
    /// </summary>
    public override async Task<StageRepair> TryRepairAsync(BootstrapStageContext context, string failureReason, bool executionFailed, CancellationToken cancellationToken)
    {
        if (executionFailed)
        {
            return failureReason.Contains("checksum", StringComparison.OrdinalIgnoreCase)
                ? StageRepair.Repaired("Retrying the package upload once — the previous upload failed integrity verification.")
                : StageRepair.None;
        }

        context.Observer?.OnPhaseProgress(Phase, "Repairing: re-opening bind-mount permissions and force-recreating the container...", null);
        var permissions = await context.Permissions.EnsureBindMountDirectoriesAsync(context.Target, Phase, context.Observer, cancellationToken);
        if (!permissions.Succeeded)
        {
            return StageRepair.Failed($"Bind-mount permission re-repair failed: {permissions.Error}");
        }

        var runtimeParent = PermissionRepairService.ResolveRuntimeParent(context.Target);
        var recreate = await context.Docker.ComposeUpAsync($"{runtimeParent}/packages/runtime-agent-extracted/docker-compose.yml", Phase, context.Observer, cancellationToken);
        return recreate.Succeeded
            ? StageRepair.Repaired("Re-opened bind-mount permissions and force-recreated the container.")
            : StageRepair.Failed($"Container force-recreate failed: {recreate.StandardError}");
    }

    /// <summary>
    /// Automatic Runtime Startup diagnostics — container state summary, compose ps, the last 200
    /// container log lines, the verbatim on-disk permission/attribute/mount state of every
    /// bind-mount directory, and the container's actual configured user (never assumed) — collected
    /// unconditionally whenever this stage fails, so the operator sees the actual root cause
    /// instead of only "Connection failed."
    /// </summary>
    public override async Task<string?> CollectDiagnosticsAsync(BootstrapStageContext context, CancellationToken cancellationToken)
    {
        context.Observer?.OnPhaseProgress(Phase, "Collecting Runtime Agent diagnostics...", null);

        var container = await context.Docker.CollectContainerDiagnosticsAsync(Phase, context.Observer, cancellationToken);
        var paths = PermissionRepairService.BuildBindMountPlan(context.Target).Select(spec => spec.Path).ToArray();
        var filesystem = await context.Permissions.InspectAsync(paths, Phase, context.Observer, cancellationToken);
        var containerUser = await context.Permissions.DetectContainerUserAsync(DockerManager.RuntimeAgentContainerName, Phase, context.Observer, cancellationToken);

        return $"{container}{Environment.NewLine}{filesystem}{Environment.NewLine}=== Container Configured User ==={Environment.NewLine}{containerUser ?? "(unreadable — container may not exist; the image's baked-in APP_UID applies)"}";
    }
}
