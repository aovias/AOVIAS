using Aovias.Configuration.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Aovias.Configuration.Deployment;

/// <summary>
/// Resolves all deployment and runtime paths from platform configuration only.
/// </summary>
public sealed class ConfiguredDeploymentPathResolver : IDeploymentPathResolver
{
    private static readonly string[] ForbiddenWritePrefixes = ["/usr/share", "/etc", "/var"];

    private readonly DeploymentStorageSettings storage;
    private readonly DeploymentHostSettings host;
    private readonly IConfiguration configuration;

    public ConfiguredDeploymentPathResolver(
        IOptions<DeploymentStorageSettings> storageOptions,
        IOptions<DeploymentHostSettings> hostOptions,
        IConfiguration configuration)
    {
        storage = storageOptions.Value;
        host = hostOptions.Value;
        this.configuration = configuration;

        DeploymentRoot = ResolveDeploymentRoot();
        IncomingRoot = ResolveUnderDeploymentRoot(storage.Incoming);
        ReleaseRoot = ResolveUnderDeploymentRoot(storage.Releases);
        TempRoot = ResolveUnderDeploymentRoot(storage.Temp);
        ArchiveRoot = ResolveUnderDeploymentRoot(storage.Archive);
        StagingRoot = ResolveUnderDeploymentRoot(storage.Staging);
        ExtractionRoot = ResolveUnderDeploymentRoot(storage.Extraction);
        ActiveRoot = ResolveUnderDeploymentRoot(storage.Active);
        RollbackRoot = ResolveUnderDeploymentRoot(storage.Rollback);
        AuditRoot = ResolveUnderDeploymentRoot(storage.Audit);

        ConfigRoot = NormalizePath(host.HostConfigRoot);
        LogsRoot = NormalizePath(host.HostLogsRoot);
        PackagesRoot = NormalizePath(host.HostPackagesRoot);
        RuntimeRoot = ResolveRuntimeRoot();
        CertificatesRoot = CombineUnix(ConfigRoot, "certificates");
        ProfilesRoot = NormalizePath(host.ProfilesDirectory);
        SecretsRoot = NormalizePath(host.SecretsDirectory);

        ValidateResolvedPathsUnderRoots();
    }

    public string DeploymentRoot { get; }

    public string IncomingRoot { get; }

    public string ReleaseRoot { get; }

    public string TempRoot { get; }

    public string ArchiveRoot { get; }

    public string StagingRoot { get; }

    public string ExtractionRoot { get; }

    public string ActiveRoot { get; }

    public string RollbackRoot { get; }

    public string AuditRoot { get; }

    public string RuntimeRoot { get; }

    public string ConfigRoot { get; }

    public string LogsRoot { get; }

    public string PackagesRoot { get; }

    public string CertificatesRoot { get; }

    public string ProfilesRoot { get; }

    public string SecretsRoot { get; }

    public string ResolveUnderDeploymentRoot(string relativeSegment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeSegment);
        return ValidateDeploymentPath(CombinePath(DeploymentRoot, relativeSegment));
    }

    public void EnsureDirectory(string path)
    {
        var validated = ValidateDeploymentPath(path);
        DeploymentExecutionDiagnostics.CreateDirectory(
            validated,
            nameof(ConfiguredDeploymentPathResolver),
            nameof(EnsureDirectory));
    }

    public DeploymentStorageValidationResult ValidateStorageLayout(bool createMissing = false)
    {
        var checks = new List<DeploymentStoragePathCheck>();
        var paths = new (string Label, string Path)[]
        {
            ("Deployment Root", DeploymentRoot),
            ("Incoming", IncomingRoot),
            ("Releases", ReleaseRoot),
            ("Temp", TempRoot),
            ("Config", ConfigRoot),
            ("Logs", LogsRoot),
            ("Packages", PackagesRoot)
        };

        foreach (var (label, path) in paths)
        {
            checks.Add(label switch
            {
                "Config" => ValidateReadOnlyDirectoryCheck(label, path, createMissing),
                _ => ValidateWritableDirectoryCheck(label, path, createMissing)
            });
        }

        checks.Add(ValidateRuntimeLayoutCheck());

        var failed = checks.FirstOrDefault(check => !string.Equals(check.Status, "PASS", StringComparison.OrdinalIgnoreCase));
        if (failed is not null)
        {
            return DeploymentStorageValidationResult.Failure(
                checks,
                failed.Detail ?? $"{failed.Label} validation failed.",
                BuildStructuredFailure(failed, "ValidateStorageLayout"));
        }

        return DeploymentStorageValidationResult.Success(checks);
    }

    public DeploymentStorageValidationResult ValidateWritable(string path, string label, string phase, string operation)
    {
        var checks = new List<DeploymentStoragePathCheck>();
        try
        {
            ValidateDeploymentPath(path);
            if (!Directory.Exists(path))
            {
                checks.Add(new DeploymentStoragePathCheck
                {
                    Label = label,
                    ResolvedPath = path,
                    Status = "FAIL",
                    Detail = "Directory does not exist."
                });
            }
            else
            {
                var probe = Path.Combine(path, $".aovias-write-probe-{Guid.NewGuid():N}");
                File.WriteAllText(probe, "probe");
                File.Delete(probe);
                checks.Add(new DeploymentStoragePathCheck
                {
                    Label = label,
                    ResolvedPath = path,
                    Status = "PASS"
                });
            }
        }
        catch (Exception exception)
        {
            checks.Add(new DeploymentStoragePathCheck
            {
                Label = label,
                ResolvedPath = path,
                Status = "FAIL",
                Detail = exception.Message
            });
        }

        var failed = checks.FirstOrDefault(check => check.Status == "FAIL");
        if (failed is null)
        {
            return DeploymentStorageValidationResult.Success(checks);
        }

        return DeploymentStorageValidationResult.Failure(
            checks,
            failed.Detail ?? $"{label} is not writable.",
            new DeploymentFailure
            {
                Category = "Filesystem",
                Phase = phase,
                Component = nameof(ConfiguredDeploymentPathResolver),
                Operation = operation,
                ExceptionType = failed.Detail?.Contains("denied", StringComparison.OrdinalIgnoreCase) == true
                    ? nameof(UnauthorizedAccessException)
                    : nameof(IOException),
                Message = failed.Detail ?? $"{label} is not writable.",
                Recovery = BuildRecovery(path),
                ExpectedPath = ResolveExpectedPath(label),
                ResolvedPath = path,
                DeploymentRoot = DeploymentRoot
            });
    }

    public static string NormalizeLegacyDeploymentRoot(string? configuredRoot, string? legacyReleaseRoot)
    {
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            var normalizedConfigured = NormalizePath(configuredRoot);
            if (IsLegacyReleaseRoot(normalizedConfigured))
            {
                return "/opt/aovias/deployments";
            }

            return normalizedConfigured;
        }

        if (string.IsNullOrWhiteSpace(legacyReleaseRoot))
        {
            throw new InvalidOperationException(
                "DeploymentStorage:Root is required. Configure settings.deployment-storage.json.");
        }

        var legacy = NormalizePath(legacyReleaseRoot);
        if (string.Equals(legacy, "/opt/aovias/releases", StringComparison.OrdinalIgnoreCase)
            || legacy.StartsWith("/opt/aovias/releases/", StringComparison.OrdinalIgnoreCase))
        {
            return "/opt/aovias/deployments";
        }

        if (legacy.EndsWith("/releases", StringComparison.OrdinalIgnoreCase))
        {
            return legacy[..^"/releases".Length];
        }

        return legacy;
    }

    private string ResolveDeploymentRoot()
    {
        var configuredRoot = storage.Root;
        if (string.IsNullOrWhiteSpace(configuredRoot))
        {
            configuredRoot = configuration[$"{DeploymentStorageSettings.SectionName}:Root"];
        }

        var legacyReleaseRoot = configuration[$"{ReleaseManagementSettings.SectionName}:ReleaseRoot"];
        return ValidateDeploymentPath(
            NormalizeLegacyDeploymentRoot(configuredRoot, legacyReleaseRoot));
    }

    private string ResolveRuntimeRoot()
    {
        var parent = Path.GetDirectoryName(ConfigRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (!string.IsNullOrWhiteSpace(parent))
        {
            return NormalizePath(parent);
        }

        return "/opt/aovias/runtime";
    }

    private void ValidateResolvedPathsUnderRoots()
    {
        foreach (var path in new[]
                 {
                     IncomingRoot, ReleaseRoot, TempRoot, ArchiveRoot, StagingRoot,
                     ExtractionRoot, ActiveRoot, RollbackRoot, AuditRoot
                 })
        {
            if (!IsUnderRoot(path, DeploymentRoot))
            {
                throw new InvalidOperationException(
                    $"Resolved path '{path}' is outside configured deployment root '{DeploymentRoot}'.");
            }
        }
    }

    private string ValidateDeploymentPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalized = NormalizePath(Path.GetFullPath(path));
        if (normalized.Contains("/usr/share/", StringComparison.OrdinalIgnoreCase)
            || normalized.EndsWith("/usr/share", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Path '{path}' is not allowed under /usr/share.");
        }

        foreach (var forbidden in ForbiddenWritePrefixes)
        {
            if (normalized.StartsWith(forbidden, StringComparison.OrdinalIgnoreCase)
                && !IsUnderConfiguredRoots(normalized))
            {
                throw new InvalidOperationException($"Path '{path}' is not allowed under '{forbidden}'.");
            }
        }

        if (UsesCommonApplicationData(path))
        {
            throw new InvalidOperationException(
                $"Path '{path}' must not use Environment.SpecialFolder.CommonApplicationData.");
        }

        if (IsLegacyReleaseRoot(normalized))
        {
            throw new InvalidOperationException(
                $"Path '{path}' uses legacy '/opt/aovias/releases'. Configure DeploymentStorage:Root='/opt/aovias/deployments'.");
        }

        return path;
    }

    private bool IsUnderConfiguredRoots(string normalizedPath)
        => IsUnderRoot(normalizedPath, DeploymentRoot)
           || IsUnderRoot(normalizedPath, RuntimeRoot);

    private static bool IsLegacyReleaseRoot(string normalizedPath)
        => normalizedPath.Contains("/opt/aovias/releases", StringComparison.OrdinalIgnoreCase);

    private static string CombinePath(string root, string segment)
    {
        if (root.StartsWith("/", StringComparison.Ordinal) && !root.Contains(':', StringComparison.Ordinal))
        {
            return NormalizePath($"{root.TrimEnd('/')}/{segment.Trim('/')}");
        }

        return Path.Combine(root, segment);
    }

    private static bool IsUnderRoot(string path, string root)
    {
        var normalizedPath = NormalizePath(Path.GetFullPath(path)).TrimEnd('/');
        var normalizedRoot = NormalizePath(Path.GetFullPath(root)).TrimEnd('/');
        return normalizedPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase)
               || normalizedPath.StartsWith(normalizedRoot + "/", StringComparison.OrdinalIgnoreCase);
    }

    private static bool UsesCommonApplicationData(string path)
    {
        var commonApplicationData = NormalizePath(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
        if (string.IsNullOrWhiteSpace(commonApplicationData))
        {
            return false;
        }

        return NormalizePath(Path.GetFullPath(path)).StartsWith(commonApplicationData, StringComparison.OrdinalIgnoreCase);
    }

    private static DeploymentStoragePathCheck ValidateWritableDirectoryCheck(string label, string path, bool createMissing)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                if (createMissing)
                {
                    DeploymentExecutionDiagnostics.CreateDirectory(
                        path,
                        nameof(ConfiguredDeploymentPathResolver),
                        nameof(ValidateWritableDirectoryCheck));
                }
                else
                {
                    return new DeploymentStoragePathCheck
                    {
                        Label = label,
                        ResolvedPath = path,
                        Status = "FAIL",
                        Detail = "Directory does not exist."
                    };
                }
            }

            var probe = Path.Combine(path, $".aovias-startup-probe-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "probe");
            File.Delete(probe);

            return new DeploymentStoragePathCheck
            {
                Label = label,
                ResolvedPath = path,
                Status = "PASS"
            };
        }
        catch (Exception exception)
        {
            return new DeploymentStoragePathCheck
            {
                Label = label,
                ResolvedPath = path,
                Status = "FAIL",
                Detail = exception.Message
            };
        }
    }

    private static DeploymentStoragePathCheck ValidateReadOnlyDirectoryCheck(string label, string path, bool createMissing)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                if (createMissing)
                {
                    DeploymentExecutionDiagnostics.CreateDirectory(
                        path,
                        nameof(ConfiguredDeploymentPathResolver),
                        nameof(ValidateReadOnlyDirectoryCheck));
                }
                else
                {
                    return new DeploymentStoragePathCheck
                    {
                        Label = label,
                        ResolvedPath = path,
                        Status = "FAIL",
                        Detail = "Directory does not exist."
                    };
                }
            }

            _ = Directory.EnumerateFileSystemEntries(path).Take(1).ToList();

            return new DeploymentStoragePathCheck
            {
                Label = label,
                ResolvedPath = path,
                Status = "PASS",
                Detail = "ReadOnly"
            };
        }
        catch (Exception exception)
        {
            return new DeploymentStoragePathCheck
            {
                Label = label,
                ResolvedPath = path,
                Status = "FAIL",
                Detail = exception.Message
            };
        }
    }

    private DeploymentStoragePathCheck ValidateRuntimeLayoutCheck()
    {
        var missing = new List<string>();
        foreach (var path in new[] { ConfigRoot, LogsRoot, PackagesRoot })
        {
            if (!Directory.Exists(path))
            {
                missing.Add(path);
            }
        }

        if (missing.Count > 0)
        {
            return new DeploymentStoragePathCheck
            {
                Label = "Runtime",
                ResolvedPath = RuntimeRoot,
                Status = "FAIL",
                Detail = $"Missing mounted runtime paths: {string.Join(", ", missing)}."
            };
        }

        return new DeploymentStoragePathCheck
        {
            Label = "Runtime",
            ResolvedPath = RuntimeRoot,
            Status = "PASS",
            Detail = "Validated via mounted config, logs, and packages paths."
        };
    }

    private DeploymentFailure BuildStructuredFailure(DeploymentStoragePathCheck failed, string operation)
        => new()
        {
            Category = "Filesystem",
            Phase = "Startup",
            Component = nameof(ConfiguredDeploymentPathResolver),
            Operation = operation,
            ExceptionType = failed.Detail?.Contains("denied", StringComparison.OrdinalIgnoreCase) == true
                ? nameof(UnauthorizedAccessException)
                : nameof(IOException),
            Message = failed.Detail ?? $"{failed.Label} validation failed.",
            Recovery = BuildRecovery(failed.ResolvedPath),
            ExpectedPath = ResolveExpectedPath(failed.Label),
            ResolvedPath = failed.ResolvedPath,
            DeploymentRoot = DeploymentRoot
        };

    private string? ResolveExpectedPath(string label)
        => label switch
        {
            "Deployment Root" => DeploymentRoot,
            "Incoming" => IncomingRoot,
            "Releases" => ReleaseRoot,
            "Temp" => TempRoot,
            "Runtime" => RuntimeRoot,
            "Config" => ConfigRoot,
            "Logs" => LogsRoot,
            "Packages" => PackagesRoot,
            _ => null
        };

    private static string BuildRecovery(string path)
    {
        if (path.Contains("/opt/aovias/releases", StringComparison.OrdinalIgnoreCase))
        {
            return "DeploymentPathResolver returned a legacy path. Set DeploymentStorage:Root to /opt/aovias/deployments and redeploy configuration.";
        }

        if (path.Contains("/opt/aovias/deployments", StringComparison.OrdinalIgnoreCase))
        {
            return "Grant the container user write access to the host deployment directory, for example: "
                   + "sudo chown --reference=/opt/aovias/runtime/logs /opt/aovias/deployments && "
                   + "sudo find /opt/aovias/deployments -type d -exec chmod 775 {} +.";
        }

        if (path.Contains("/opt/aovias/runtime/config", StringComparison.OrdinalIgnoreCase))
        {
            return "Runtime configuration is mounted read-only. Ensure the host config directory exists and is readable by the container user.";
        }

        return "Verify deployment configuration, directory ownership, and write permissions for the resolved path.";
    }

    private static string NormalizePath(string path)
        => path.Replace('\\', '/');

    private static string CombineUnix(string root, string segment)
        => $"{NormalizePath(root).TrimEnd('/')}/{segment.Trim('/')}";
}
