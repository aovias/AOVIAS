using System.Collections.Concurrent;
using System.Text.Json;
using Aovias.Configuration.Deployment;
using Aovias.Release.Contracts.Releases;

namespace Aovias.ReleaseManager.Services;

/// <summary>
/// File-system backed release repository with in-memory index.
/// </summary>
public sealed class FileSystemReleaseRepository : Abstractions.IReleaseRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly IDeploymentPathResolver pathResolver;
    private readonly ConcurrentDictionary<string, ReleaseRecord> cache = new(StringComparer.OrdinalIgnoreCase);

    public FileSystemReleaseRepository(IDeploymentPathResolver pathResolver)
    {
        this.pathResolver = pathResolver;
        LoadExistingRecords();
    }

    /// <inheritdoc />
    public Task EnsureDirectoriesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DeploymentExecutionDiagnostics.LogPipelineStage(
            "Repository",
            nameof(FileSystemReleaseRepository),
            nameof(EnsureDirectoriesAsync));
        DeploymentExecutionDiagnostics.LogReleaseRootAccess(
            pathResolver,
            nameof(FileSystemReleaseRepository),
            nameof(EnsureDirectoriesAsync));

        pathResolver.EnsureDirectory(pathResolver.ReleaseRoot);
        pathResolver.EnsureDirectory(pathResolver.IncomingRoot);
        pathResolver.EnsureDirectory(pathResolver.StagingRoot);
        pathResolver.EnsureDirectory(pathResolver.ExtractionRoot);
        pathResolver.EnsureDirectory(pathResolver.ActiveRoot);
        pathResolver.EnsureDirectory(pathResolver.RollbackRoot);
        pathResolver.EnsureDirectory(pathResolver.ArchiveRoot);
        pathResolver.EnsureDirectory(pathResolver.TempRoot);
        pathResolver.EnsureDirectory(pathResolver.PackagesRoot);

        DeploymentExecutionDiagnostics.LogPipelineStage(
            "Filesystem",
            nameof(ConfiguredDeploymentPathResolver),
            "EnsureDirectory");

        LoadExistingRecords();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task SaveAsync(ReleaseRecord record, CancellationToken cancellationToken = default)
    {
        cache[record.Identity.ReleaseId] = record;

        var recordDirectory = Path.Combine(pathResolver.ReleaseRoot, record.Identity.ReleaseId);
        pathResolver.EnsureDirectory(recordDirectory);

        var recordPath = Path.Combine(recordDirectory, "release.json");
        await using var stream = File.Create(recordPath);
        await JsonSerializer.SerializeAsync(stream, record, JsonOptions, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ReleaseRecord?> GetAsync(string releaseId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (cache.TryGetValue(releaseId, out var cached))
        {
            return Task.FromResult<ReleaseRecord?>(cached);
        }

        var loaded = TryLoadRecordFromDisk(releaseId);
        if (loaded is not null)
        {
            cache[releaseId] = loaded;
        }

        return Task.FromResult(loaded);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ReleaseRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        LoadExistingRecords();
        IReadOnlyList<ReleaseRecord> records = cache.Values
            .OrderByDescending(record => record.UpdatedAt)
            .ToList();
        return Task.FromResult(records);
    }

    private ReleaseRecord? TryLoadRecordFromDisk(string releaseId)
    {
        if (string.IsNullOrWhiteSpace(releaseId) || !Directory.Exists(pathResolver.ReleaseRoot))
        {
            return null;
        }

        var recordPath = Path.Combine(pathResolver.ReleaseRoot, releaseId, "release.json");
        if (!File.Exists(recordPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(recordPath);
            return JsonSerializer.Deserialize<ReleaseRecord>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private void LoadExistingRecords()
    {
        if (!Directory.Exists(pathResolver.ReleaseRoot))
        {
            return;
        }

        foreach (var recordDirectory in Directory.GetDirectories(pathResolver.ReleaseRoot))
        {
            var recordPath = Path.Combine(recordDirectory, "release.json");
            if (!File.Exists(recordPath))
            {
                continue;
            }

            try
            {
                var json = File.ReadAllText(recordPath);
                var record = JsonSerializer.Deserialize<ReleaseRecord>(json, JsonOptions);
                if (record is not null)
                {
                    cache[record.Identity.ReleaseId] = record;
                }
            }
            catch (JsonException)
            {
                // Skip corrupt records during index rebuild.
            }
        }
    }
}
