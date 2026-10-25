using System.Collections.Concurrent;
using Aovias.Configuration.Settings;
using Aovias.Publishing.Contracts;
using Microsoft.Extensions.Options;

namespace Aovias.Publishing.Core.Workspace;

/// <summary>
/// Creates and manages isolated publishing workspaces.
/// </summary>
public sealed class PublishingWorkspaceManager : IPublishingWorkspaceManager
{
    private static readonly ConcurrentDictionary<string, byte> ActiveLocks = new(StringComparer.OrdinalIgnoreCase);

    private readonly IOptionsMonitor<PublishingSettings> settings;

    public PublishingWorkspaceManager(IOptionsMonitor<PublishingSettings> settings)
    {
        this.settings = settings;
    }

    public PublishingSession CreateSession(PublishingRequest request)
    {
        var publishingSettings = settings.CurrentValue;
        var sessionId = Guid.NewGuid().ToString("N");
        var workspaceRoot = Path.Combine(
            publishingSettings.TemporaryFolder,
            "sessions",
            Path.GetFileNameWithoutExtension(request.ProjectPath),
            sessionId);

        Directory.CreateDirectory(workspaceRoot);
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "staging"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "publish"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "docker-context"));
        Directory.CreateDirectory(Path.Combine(workspaceRoot, "logs"));

        ActiveLocks[sessionId] = 0;

        return new PublishingSession
        {
            SessionId = sessionId,
            MachineName = Environment.MachineName,
            OperatingSystem = Environment.OSVersion.VersionString,
            Profile = request.Profile,
            ProjectPath = request.ProjectPath,
            ServiceId = request.ServiceId,
            PackageVersion = request.Version,
            WorkspaceRoot = workspaceRoot,
            LogsRoot = Path.Combine(workspaceRoot, "logs")
        };
    }

    public void RecoverWorkspace(PublishingSession session)
    {
        RecoverDirectory(Path.Combine(session.WorkspaceRoot, "staging"));
        RecoverDirectory(Path.Combine(session.WorkspaceRoot, "publish"));
        RecoverDirectory(Path.Combine(session.WorkspaceRoot, "docker-context"));

        var validationRoot = settings.CurrentValue.DeploymentValidationFolder;
        if (Directory.Exists(validationRoot))
        {
            foreach (var directory in Directory.GetDirectories(validationRoot))
            {
                var info = new DirectoryInfo(directory);
                if (info.LastWriteTimeUtc < DateTime.UtcNow.AddHours(-6))
                {
                    TryDelete(directory);
                }
            }
        }
    }

    public void CleanStaleWorkspaces(TimeSpan maxAge)
    {
        var sessionsRoot = Path.Combine(settings.CurrentValue.TemporaryFolder, "sessions");
        if (!Directory.Exists(sessionsRoot))
        {
            return;
        }

        foreach (var projectDirectory in Directory.EnumerateDirectories(sessionsRoot))
        {
            foreach (var sessionDirectory in Directory.EnumerateDirectories(projectDirectory))
            {
                var sessionId = Path.GetFileName(sessionDirectory);
                if (ActiveLocks.ContainsKey(sessionId))
                {
                    continue;
                }

                var info = new DirectoryInfo(sessionDirectory);
                if (DateTime.UtcNow - info.LastWriteTimeUtc > maxAge)
                {
                    TryDelete(sessionDirectory);
                }
            }

            if (!Directory.EnumerateFileSystemEntries(projectDirectory).Any())
            {
                TryDelete(projectDirectory);
            }
        }
    }

    public void DisposeSession(PublishingSession session)
    {
        ActiveLocks.TryRemove(session.SessionId, out _);
        TryDelete(session.WorkspaceRoot);

        var projectDirectory = Path.GetDirectoryName(session.WorkspaceRoot);
        if (!string.IsNullOrWhiteSpace(projectDirectory)
            && Directory.Exists(projectDirectory)
            && !Directory.EnumerateFileSystemEntries(projectDirectory).Any())
        {
            TryDelete(projectDirectory);
        }
    }

    private static void RecoverDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            return;
        }

        foreach (var file in Directory.GetFiles(path, "*.tmp", SearchOption.AllDirectories))
        {
            TryDelete(file);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup.
        }
    }
}
