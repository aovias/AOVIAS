using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core;

/// <summary>
/// One runtime directory the Runtime Agent container depends on, declared as an object (Sprint
/// M7.x "Directory Management": never blindly chmod — every folder declares what it needs and how
/// to verify it) rather than as a path string scattered across stage code.
/// </summary>
public sealed record RuntimeDirectorySpec
{
    public required string Name { get; init; }

    public required string Path { get; init; }

    /// <summary>Octal mode the directory must carry. 0777 for every container-accessed directory — see <see cref="PermissionRepairService"/> remarks for why broad-open is the deliberate policy here rather than per-UID ownership.</summary>
    public required string RequiredMode { get; init; }

    /// <summary>Whether the container must be able to WRITE here (rw bind mount) or only read (config).</summary>
    public required bool ContainerWritable { get; init; }

    public required string Purpose { get; init; }
}

/// <summary>Outcome of one permission ensure/repair pass.</summary>
public sealed record PermissionRepairOutcome(bool Succeeded, string Evidence, string? Error = null);

/// <summary>Result of verifying one path's on-disk mode: positively verified, positively wrong, or unreadable/unparseable (inconclusive — never treated as a blocker on its own, matching Preflight's own inconclusive-reading rule).</summary>
public enum PermissionVerificationState
{
    Verified,
    Mismatch,
    Inconclusive
}

public sealed record PermissionVerification(PermissionVerificationState State, string Detail);

/// <summary>
/// Sprint M7.x — the centralized Permission Repair Engine. Every directory-permission concern
/// Bootstrap has (ownership handoff to the SSH user for SFTP writes, opening access for the
/// container's own UID, clearing immutable attributes, verifying the result, and producing
/// evidence when something still refuses) lives here instead of as chmod/chown/chattr calls
/// scattered through stage code — the direct lesson of the M6-era defect rounds, where the same
/// fix had to be re-applied in two phases because each phase owned its own copy.
///
/// Policy note on <c>0777</c>: the Runtime Agent container runs as a fixed non-root UID baked into
/// its image (see <see cref="DetectContainerUserAsync"/> and
/// <c>Aovias.Deployment.Contracts.PlatformRuntimeDefaults.ContainerUserId</c>), which is unrelated
/// to whichever user Bootstrap connects as. Chowning precisely to that UID would break the SSH
/// user's own later unprivileged SFTP uploads into the same tree, so opening permissions broadly
/// is the deliberate, pragmatic policy — host-hardening beyond that is the operator's concern.
/// </summary>
public sealed class PermissionRepairService(RemoteExecutionEngine engine)
{
    /// <summary>The bind-mount source directories the Runtime Agent's compose file maps into the container, declared once. <paramref name="target"/>'s config root's parent (e.g. <c>/opt/aovias</c>) anchors the tree.</summary>
    public static IReadOnlyList<RuntimeDirectorySpec> BuildBindMountPlan(BootstrapTarget target)
    {
        var configRoot = target.RemoteConfigRoot.TrimEnd('/');
        var runtimeParent = ResolveRuntimeParent(target);
        return
        [
            new RuntimeDirectorySpec { Name = "Config", Path = configRoot, RequiredMode = "0777", ContainerWritable = false, Purpose = "Profile settings the agent reads at startup (ro bind mount)." },
            new RuntimeDirectorySpec { Name = "Logs", Path = $"{runtimeParent}/logs", RequiredMode = "0777", ContainerWritable = true, Purpose = "Runtime Agent's own rolling log files." },
            new RuntimeDirectorySpec { Name = "Packages", Path = $"{runtimeParent}/packages", RequiredMode = "0777", ContainerWritable = true, Purpose = "Uploaded deployment packages and their extraction workspace." },
            new RuntimeDirectorySpec { Name = "Deployments", Path = $"{runtimeParent}/deployments", RequiredMode = "0777", ContainerWritable = true, Purpose = "Deployment storage tree (incoming/releases/temp/state and session records)." }
        ];
    }

    public static string ResolveRuntimeParent(BootstrapTarget target)
    {
        var configRoot = target.RemoteConfigRoot.TrimEnd('/');
        var parentSlash = configRoot.LastIndexOf('/');
        return parentSlash > 0 ? configRoot[..parentSlash] : configRoot;
    }

    /// <summary>
    /// "Never assume UID — determine runtime UID dynamically": asks Docker what user the container
    /// is actually configured to run as. Returns null when the container does not exist yet or the
    /// reading is unparseable — callers use this as diagnostic evidence, never as a gate (the 0777
    /// policy above makes the exact UID informational).
    /// </summary>
    public async Task<string?> DetectContainerUserAsync(string containerName, BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
    {
        var result = await engine.RunPrivilegedAsync($"docker inspect --format \"{{{{.Config.User}}}}\" {containerName}", phase, observer, cancellationToken);
        if (!result.Succeeded)
        {
            return null;
        }

        var user = result.StandardOutput.Trim();
        return string.IsNullOrWhiteSpace(user) ? null : user;
    }

    /// <summary>
    /// Stage 4's tree preparation: creates the profile scaffold, hands ownership of the whole
    /// runtime parent to the SSH user (so later plain SFTP writes need no elevation — skipped for a
    /// root connection, where SFTP already writes as root), clears immutable attributes
    /// (best-effort: rotated logs and closed release files have been observed <c>chattr +i</c>'d by
    /// host-side integrity tooling, which makes even root's chmod fail with "Operation not
    /// permitted"), and opens permissions for the container's own UID. Runs unconditionally every
    /// time it is called — cheap, idempotent, and the only defense a target bootstrapped by an
    /// older build has.
    /// </summary>
    public async Task<PermissionRepairOutcome> EnsureRuntimeTreeAsync(
        BootstrapTarget target,
        string profileDirectory,
        BootstrapPhaseName phase,
        IBootstrapExecutionObserver? observer,
        CancellationToken cancellationToken)
    {
        var runtimeParent = ResolveRuntimeParent(target);

        var mkdir = await engine.RunPrivilegedAsync($"mkdir -p {profileDirectory}", phase, observer, cancellationToken);
        if (!mkdir.Succeeded)
        {
            return new PermissionRepairOutcome(false, $"Could not create profile directory '{profileDirectory}'.", mkdir.StandardError);
        }

        await engine.RunPrivilegedAsync($"chattr -R -i {runtimeParent}", phase, observer, cancellationToken);

        if (engine.PrivilegeMode != BootstrapPrivilegeMode.NotRequired)
        {
            var chown = await engine.RunPrivilegedAsync($"chown -R {target.Username}:{target.Username} {runtimeParent}", phase, observer, cancellationToken);
            if (!chown.Succeeded)
            {
                return new PermissionRepairOutcome(false, $"Could not grant '{target.Username}' ownership of '{runtimeParent}'.", chown.StandardError);
            }
        }

        var chmod = await engine.RunPrivilegedAsync($"chmod -R 0777 {runtimeParent}", phase, observer, cancellationToken);
        if (!chmod.Succeeded)
        {
            var inspection = await InspectAsync([runtimeParent], phase, observer, cancellationToken);
            return new PermissionRepairOutcome(
                false,
                $"Could not open permissions on '{runtimeParent}' for the Runtime Agent container's own UID.",
                $"{chmod.StandardError}{Environment.NewLine}{Environment.NewLine}{inspection}");
        }

        return new PermissionRepairOutcome(true, $"Prepared '{runtimeParent}' — profile scaffold present, immutable attributes cleared, ownership and permissions applied.");
    }

    /// <summary>
    /// Stage 7's immediate pre-start pass over each bind-mount source directory, named explicitly
    /// (not one recursive parent-level command) after a real-world round where a blanket recursive
    /// chmod demonstrably reached some siblings but not others. Independent of whether Stage 4 ran
    /// this attempt — the container must never start against directories whose state depends on
    /// another stage's checkpoint history.
    /// </summary>
    public async Task<PermissionRepairOutcome> EnsureBindMountDirectoriesAsync(
        BootstrapTarget target,
        BootstrapPhaseName phase,
        IBootstrapExecutionObserver? observer,
        CancellationToken cancellationToken)
    {
        var paths = BuildBindMountPlan(target).Select(spec => spec.Path).ToArray();
        var quoted = string.Join(' ', paths.Select(path => $"\"{path}\""));

        var mkdir = await engine.RunPrivilegedAsync($"mkdir -p {quoted}", phase, observer, cancellationToken);
        if (!mkdir.Succeeded)
        {
            return new PermissionRepairOutcome(false, "Could not ensure the Runtime Agent's bind-mount directories exist.", mkdir.StandardError);
        }

        await engine.RunPrivilegedAsync($"chattr -R -i {quoted}", phase, observer, cancellationToken);

        var chmod = await engine.RunPrivilegedAsync($"chmod -R 0777 {quoted}", phase, observer, cancellationToken);
        if (!chmod.Succeeded)
        {
            return new PermissionRepairOutcome(
                false,
                "Runtime Startup Failed — could not open permissions on the Runtime Agent's bind-mount directories for the container's own UID.",
                chmod.StandardError);
        }

        return new PermissionRepairOutcome(true, $"Bind-mount directories prepared: {string.Join(", ", paths)}.");
    }

    /// <summary>
    /// Verifies a path's actual on-disk mode. A parsed match is <see cref="PermissionVerificationState.Verified"/>,
    /// a parsed different mode is <see cref="PermissionVerificationState.Mismatch"/>, and anything
    /// unreadable/unparseable is <see cref="PermissionVerificationState.Inconclusive"/> — the same
    /// "only a genuine, parsed negative signal is a blocker" rule Preflight applies to its own
    /// remote readings. Unprivileged: after the ensure pass the SSH user can stat its own tree.
    /// </summary>
    public async Task<PermissionVerification> VerifyModeAsync(string path, string expectedOctal, CancellationToken cancellationToken)
    {
        var stat = await engine.RunAsync($"stat -c '%a' \"{path}\"", cancellationToken);
        var reading = stat.StandardOutput.Trim();

        if (!stat.Succeeded || string.IsNullOrWhiteSpace(reading) || !reading.All(char.IsDigit))
        {
            return new PermissionVerification(PermissionVerificationState.Inconclusive, $"Mode of '{path}' could not be read ({(string.IsNullOrWhiteSpace(reading) ? "no output" : reading)}).");
        }

        var normalizedExpected = expectedOctal.TrimStart('0') is { Length: > 0 } trimmed ? trimmed : "0";
        var normalizedActual = reading.TrimStart('0') is { Length: > 0 } actualTrimmed ? actualTrimmed : "0";
        return string.Equals(normalizedActual, normalizedExpected, StringComparison.Ordinal)
            ? new PermissionVerification(PermissionVerificationState.Verified, $"'{path}' carries mode {reading}.")
            : new PermissionVerification(PermissionVerificationState.Mismatch, $"'{path}' carries mode {reading}, expected {expectedOctal}.");
    }

    /// <summary>
    /// Evidence collection for diagnostics: the verbatim permission/ownership bits (<c>ls -la</c>),
    /// filesystem attributes (<c>lsattr -R</c> — proves or rules out the immutable flag), and the
    /// mount options of the tree's filesystem (<c>findmnt</c> — a read-only remount makes every
    /// write fail no matter what the mode bits say). Best-effort throughout: a diagnostic command
    /// failing is reported as such, never allowed to mask the failure it is attached to.
    /// </summary>
    public async Task<string> InspectAsync(IReadOnlyList<string> paths, BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
    {
        var quoted = string.Join(' ', paths.Select(path => $"\"{path}\""));
        var listing = await engine.RunPrivilegedAsync($"ls -la {quoted}", phase, observer, cancellationToken);
        var attributes = await engine.RunPrivilegedAsync($"lsattr -R {quoted}", phase, observer, cancellationToken);
        var mountOptions = await engine.RunAsync($"findmnt -n -o OPTIONS --target \"{paths[0]}\"", cancellationToken);

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("=== Bind-Mount Directory Permissions (ls -la) ===");
        builder.AppendLine(listing.Succeeded ? listing.StandardOutput.Trim() : $"(ls -la failed: {listing.StandardError})");
        builder.AppendLine();
        builder.AppendLine("=== Bind-Mount Directory Attributes (lsattr -R) ===");
        builder.AppendLine(attributes.Succeeded ? attributes.StandardOutput.Trim() : $"(lsattr failed: {attributes.StandardError})");
        builder.AppendLine();
        builder.AppendLine("=== Filesystem Mount Options (findmnt) ===");
        builder.AppendLine(mountOptions.Succeeded && !string.IsNullOrWhiteSpace(mountOptions.StandardOutput)
            ? mountOptions.StandardOutput.Trim()
            : "(mount options could not be read — inconclusive)");

        return builder.ToString();
    }
}
