using System.Security.Cryptography;
using System.Text;
using Aovias.Bootstrap.Contracts;

namespace Aovias.Bootstrap.Core;

/// <summary>Per-file drift assessment of the generated profile configuration against what the target actually has.</summary>
public sealed record ConfigurationSyncAssessment(
    IReadOnlyList<string> Missing,
    IReadOnlyList<string> Drifted,
    IReadOnlyList<string> Current)
{
    public bool AllCurrent => Missing.Count == 0 && Drifted.Count == 0;

    public string Describe() => AllCurrent
        ? $"All {Current.Count} configuration files verified current (checksum match)."
        : $"{Missing.Count} missing, {Drifted.Count} drifted, {Current.Count} current.";
}

public sealed record ConfigurationSyncResult(
    IReadOnlyList<string> Uploaded,
    IReadOnlyList<string> Unchanged,
    IReadOnlyList<string> VerificationFailures)
{
    public bool Succeeded => VerificationFailures.Count == 0;
}

/// <summary>
/// Sprint M7.x — checksum-based configuration synchronization. Every bootstrap run regenerates the
/// full profile configuration deterministically (see
/// <c>RuntimeConfigurationScaffoldBuilder.BuildAllRequiredProfileFiles</c>), compares each file's
/// SHA-256 against what is actually on the target, uploads only what is missing or drifted, and
/// verifies each upload landed intact. Configuration drift is therefore always corrected — the
/// direct lesson of the M6-era defect where a stale <c>settings.deployment-host.json</c> from an
/// older build could never be repaired because generation skipped files that already existed. An
/// inconclusive remote checksum reading (host without <c>sha256sum</c>) is treated as drift — a
/// redundant re-upload is harmless; trusting an unverifiable file is not.
/// </summary>
public sealed class ConfigurationSynchronizer(IBootstrapTargetExecutor executor, RemoteExecutionEngine engine)
{
    public async Task<ConfigurationSyncAssessment> AssessAsync(BootstrapTarget target, CancellationToken cancellationToken)
    {
        var missing = new List<string>();
        var drifted = new List<string>();
        var current = new List<string>();

        foreach (var (fileName, content) in RuntimeConfigurationScaffoldBuilder.BuildAllRequiredProfileFiles(target))
        {
            var remotePath = RuntimeConfigurationScaffoldBuilder.ResolveProfileFileRemotePath(target, fileName);
            if (!await executor.RemotePathExistsAsync(remotePath, cancellationToken))
            {
                missing.Add(fileName);
                continue;
            }

            var remoteHash = await ReadRemoteHashAsync(remotePath, cancellationToken);
            if (remoteHash is not null && string.Equals(remoteHash, ComputeHash(content), StringComparison.OrdinalIgnoreCase))
            {
                current.Add(fileName);
            }
            else
            {
                drifted.Add(fileName);
            }
        }

        return new ConfigurationSyncAssessment(missing, drifted, current);
    }

    /// <summary>Generate → Compare → Upload (only what changed) → Verify checksum, per file.</summary>
    public async Task<ConfigurationSyncResult> SynchronizeAsync(BootstrapTarget target, BootstrapPhaseName phase, IBootstrapExecutionObserver? observer, CancellationToken cancellationToken)
    {
        var uploaded = new List<string>();
        var unchanged = new List<string>();
        var verificationFailures = new List<string>();

        foreach (var (fileName, content) in RuntimeConfigurationScaffoldBuilder.BuildAllRequiredProfileFiles(target))
        {
            var remotePath = RuntimeConfigurationScaffoldBuilder.ResolveProfileFileRemotePath(target, fileName);
            var localHash = ComputeHash(content);

            if (await executor.RemotePathExistsAsync(remotePath, cancellationToken))
            {
                var existingHash = await ReadRemoteHashAsync(remotePath, cancellationToken);
                if (existingHash is not null && string.Equals(existingHash, localHash, StringComparison.OrdinalIgnoreCase))
                {
                    unchanged.Add(fileName);
                    continue;
                }
            }

            observer?.OnPhaseProgress(phase, $"Synchronizing {fileName}...", null);
            await executor.UploadTextFileAsync(remotePath, content, cancellationToken);
            uploaded.Add(fileName);

            // Post-upload integrity verification. Inconclusive (no sha256sum on the host) is
            // accepted — only a genuine, parsed mismatch is a failure.
            var verifyHash = await ReadRemoteHashAsync(remotePath, cancellationToken);
            if (verifyHash is not null && !string.Equals(verifyHash, localHash, StringComparison.OrdinalIgnoreCase))
            {
                verificationFailures.Add($"{fileName}: expected sha256:{localHash}, got sha256:{verifyHash}");
            }
        }

        return new ConfigurationSyncResult(uploaded, unchanged, verificationFailures);
    }

    /// <summary>
    /// Post-synchronization verification, distinct from <see cref="AssessAsync"/> on purpose:
    /// assessment treats an unverifiable checksum as drift (prefer a redundant re-upload over
    /// trusting an unverifiable file), but VERIFICATION must not fail a host that simply cannot
    /// compute checksums — after a successful upload, only a missing file or a genuine, parsed
    /// hash mismatch is a failure; an inconclusive reading passes with a note.
    /// </summary>
    public async Task<(bool Verified, string Evidence)> VerifyCurrentAsync(BootstrapTarget target, CancellationToken cancellationToken)
    {
        var missing = new List<string>();
        var mismatched = new List<string>();
        var inconclusive = 0;
        var verified = 0;

        foreach (var (fileName, content) in RuntimeConfigurationScaffoldBuilder.BuildAllRequiredProfileFiles(target))
        {
            var remotePath = RuntimeConfigurationScaffoldBuilder.ResolveProfileFileRemotePath(target, fileName);
            if (!await executor.RemotePathExistsAsync(remotePath, cancellationToken))
            {
                missing.Add(fileName);
                continue;
            }

            var remoteHash = await ReadRemoteHashAsync(remotePath, cancellationToken);
            if (remoteHash is null)
            {
                inconclusive++;
            }
            else if (string.Equals(remoteHash, ComputeHash(content), StringComparison.OrdinalIgnoreCase))
            {
                verified++;
            }
            else
            {
                mismatched.Add(fileName);
            }
        }

        var ok = missing.Count == 0 && mismatched.Count == 0;
        var evidence = ok
            ? $"{verified} file(s) checksum-verified, {inconclusive} present but unverifiable (no usable sha256sum on the host)."
            : $"missing: [{string.Join(", ", missing)}]; checksum mismatch: [{string.Join(", ", mismatched)}].";
        return (ok, evidence);
    }

    private async Task<string?> ReadRemoteHashAsync(string remotePath, CancellationToken cancellationToken)
    {
        var result = await engine.RunAsync($"sha256sum \"{remotePath}\" | awk '{{print $1}}'", cancellationToken);
        var hash = result.StandardOutput.Trim();
        return result.Succeeded && hash.Length == 64 && hash.All(Uri.IsHexDigit) ? hash : null;
    }

    private static string ComputeHash(string content)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
}
