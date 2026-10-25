using System.Text.Json;
using System.Text.Json.Serialization;
using Aovias.Bootstrap.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aovias.Bootstrap.Core;

/// <summary>
/// File-backed <see cref="IBootstrapStateStore"/> — one small JSON file per target
/// (<c>{workingDirectory}/bootstrap-state/{targetIdentity-sanitized}.json</c>) recording which
/// phases have completed. This is the entire idempotency/resumability mechanism: the orchestrator
/// consults it before running each phase and skips anything already recorded, and records a phase
/// the moment it completes (not at the end of the run), so a process killed mid-run leaves an
/// accurate record of exactly how far it got.
///
/// Phases serialize as their string names (<see cref="JsonStringEnumConverter"/>), not numeric
/// values — <see cref="BootstrapPhaseName"/> gained a new value (<c>ValidateRuntimeConfiguration</c>)
/// in the middle of its existing numbering when this defect fix shipped, and string names make any
/// *future* phase insertion equally safe: a numeric encoding would have silently reattributed
/// on-disk phase 6/7/8 completions (the old <c>RuntimeAgentInstallation</c>/<c>RuntimeAgentRegistration</c>/
/// <c>BootstrapVerification</c>) to whatever phase now happens to occupy those numbers.
/// </summary>
public sealed class BootstrapStateStore : IBootstrapStateStore
{
    /// <summary>Bumped when <see cref="BootstrapPhaseName"/>'s numbering changes in a way that could
    /// make an old on-disk phase list mean something different than it did when it was written. A
    /// file with no <c>SchemaVersion</c> at all (any build before this defect fix) deserializes to 0
    /// — see <see cref="StateRecord.SchemaVersion"/>'s remarks for why that can't just default to
    /// <see cref="CurrentSchemaVersion"/> the way most "new field" migrations would.</summary>
    private const int CurrentSchemaVersion = 2;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string stateDirectory;
    private readonly ILogger<BootstrapStateStore> logger;

    public BootstrapStateStore(string workingDirectory, ILogger<BootstrapStateStore>? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        stateDirectory = Path.Combine(workingDirectory, "bootstrap-state");
        this.logger = logger ?? NullLogger<BootstrapStateStore>.Instance;
    }

    public async Task<IReadOnlySet<BootstrapPhaseName>> GetCompletedPhasesAsync(string targetIdentity, CancellationToken cancellationToken)
    {
        var path = StateFilePath(targetIdentity);
        if (!File.Exists(path))
        {
            return new HashSet<BootstrapPhaseName>();
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var record = await JsonSerializer.DeserializeAsync<StateRecord>(stream, SerializerOptions, cancellationToken);
            if (record is null)
            {
                return new HashSet<BootstrapPhaseName>();
            }

            if (record.SchemaVersion < CurrentSchemaVersion)
            {
                // Written before BootstrapPhaseName's numbering changed (System.Text.Json's
                // JsonStringEnumConverter still happily reads old numeric phase values without
                // erroring — it would silently reattribute them to whatever phase now occupies that
                // number, e.g. an old "6" meaning RuntimeAgentInstallation being misread as the new
                // ValidateRuntimeConfiguration). Treat as "nothing completed yet" rather than trusting
                // numbers that may no longer mean what they meant when written — the safe direction to
                // fail in, since every phase's own already-satisfied check makes a full
                // re-verification cheap and idempotent, matching what Force/Repair already does on
                // purpose.
                logger.LogWarning(
                    "Bootstrap state file '{Path}' predates schema version {CurrentSchemaVersion} (found {FoundSchemaVersion}); treating '{Target}' as not yet started rather than trusting possibly-renumbered phase values.",
                    path, CurrentSchemaVersion, record.SchemaVersion, targetIdentity);
                return new HashSet<BootstrapPhaseName>();
            }

            return new HashSet<BootstrapPhaseName>(record.CompletedPhases);
        }
        catch (JsonException exception)
        {
            // Any other corruption (truncated file, hand-edited garbage, etc.) — same safe direction.
            logger.LogWarning(exception, "Bootstrap state file '{Path}' could not be read; treating '{Target}' as not yet started.", path, targetIdentity);
            return new HashSet<BootstrapPhaseName>();
        }
    }

    public async Task MarkCompletedAsync(string targetIdentity, BootstrapPhaseName phase, CancellationToken cancellationToken)
    {
        var completed = new HashSet<BootstrapPhaseName>(await GetCompletedPhasesAsync(targetIdentity, cancellationToken)) { phase };
        await WriteAsync(targetIdentity, completed, cancellationToken);
    }

    public Task ResetAsync(string targetIdentity, CancellationToken cancellationToken)
    {
        var path = StateFilePath(targetIdentity);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private async Task WriteAsync(string targetIdentity, IReadOnlySet<BootstrapPhaseName> completedPhases, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(stateDirectory);
        var path = StateFilePath(targetIdentity);
        var record = new StateRecord { SchemaVersion = CurrentSchemaVersion, TargetIdentity = targetIdentity, CompletedPhases = completedPhases.ToList() };
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, record, SerializerOptions, cancellationToken);
    }

    private string StateFilePath(string targetIdentity)
    {
        var sanitized = string.Concat(targetIdentity.Select(c => char.IsLetterOrDigit(c) ? c : '-'));
        return Path.Combine(stateDirectory, $"{sanitized}.json");
    }

    private sealed class StateRecord
    {
        /// <summary>Deliberately no default-value initializer (so it stays 0, not <see cref="CurrentSchemaVersion"/>, when absent from JSON) — System.Text.Json only overwrites properties actually present in the source; a property with an initializer would keep looking "current" for a file that never had this property at all, defeating the whole check.</summary>
        public int SchemaVersion { get; init; }

        public string TargetIdentity { get; init; } = string.Empty;

        public List<BootstrapPhaseName> CompletedPhases { get; init; } = [];
    }
}
