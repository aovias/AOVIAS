using System.Text.Json;

namespace Aovias.Configuration.Layout;

/// <summary>
/// Loads and validates the repository layout contract file.
/// </summary>
public static class RepositoryLayoutLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static bool TryLoadFromFile(string manifestPath, out RepositoryLayoutManifest manifest, out string? error)
    {
        manifest = null!;
        error = null;

        try
        {
            if (string.IsNullOrWhiteSpace(manifestPath))
            {
                error = "Manifest path is required.";
                return false;
            }

            if (!File.Exists(manifestPath))
            {
                error = "Repository layout manifest was not found.";
                return false;
            }

            var json = File.ReadAllText(manifestPath);
            return TryLoadFromJson(json, out manifest, out error);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            error = ex.Message;
            return false;
        }
    }

    public static bool TryLoadFromJson(string json, out RepositoryLayoutManifest manifest, out string? error)
    {
        manifest = null!;
        error = null;

        try
        {
            var deserialized = JsonSerializer.Deserialize<RepositoryLayoutManifest>(json, JsonOptions);
            if (deserialized is null)
            {
                error = "Repository layout manifest deserialized to null.";
                return false;
            }

            var errors = Validate(deserialized);
            if (errors.Count > 0)
            {
                error = string.Join("; ", errors);
                return false;
            }

            manifest = deserialized;
            return true;
        }
        catch (JsonException ex)
        {
            error = ex.Message;
            return false;
        }
    }

    public static RepositoryLayoutManifest LoadFromFile(string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            throw new ArgumentException("Manifest path is required.", nameof(manifestPath));
        }

        if (!File.Exists(manifestPath))
        {
            throw new FileNotFoundException("Repository layout manifest was not found.", manifestPath);
        }

        var json = File.ReadAllText(manifestPath);
        return LoadFromJson(json);
    }

    public static RepositoryLayoutManifest LoadFromJson(string json)
    {
        var manifest = JsonSerializer.Deserialize<RepositoryLayoutManifest>(json, JsonOptions)
                       ?? throw new InvalidOperationException("Repository layout manifest deserialized to null.");

        var errors = Validate(manifest);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Repository layout manifest validation failed: " + string.Join("; ", errors));
        }

        return manifest;
    }

    public static IReadOnlyList<string> Validate(RepositoryLayoutManifest manifest)
    {
        var errors = new List<string>();
        if (manifest is null)
        {
            errors.Add("Manifest is null.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(manifest.SchemaVersion))
        {
            errors.Add("schemaVersion is required.");
        }
        else if (!RepositoryLayoutSchema.IsSupported(manifest.SchemaVersion))
        {
            errors.Add($"Unsupported schemaVersion '{manifest.SchemaVersion}'.");
        }

        if (manifest.Discovery.Current.PlatformRoot.RequiredAll.Count == 0)
        {
            errors.Add("discovery.current.platformRoot.requiredAll must not be empty.");
        }

        if (manifest.Discovery.Current.PlatformRoot.RequiredAny.Count == 0)
        {
            errors.Add("discovery.current.platformRoot.requiredAny must not be empty.");
        }

        if (manifest.Discovery.Current.CsmPlatformRoot.RequiredAll.Count == 0)
        {
            errors.Add("discovery.current.csmPlatformRoot.requiredAll must not be empty.");
        }

        if (manifest.Discovery.Target.CsmPlatformRoot.RequiredAll.Count == 0)
        {
            errors.Add("discovery.target.csmPlatformRoot.requiredAll must not be empty.");
        }

        if (manifest.Discovery.Target.PlatformRoot.RequiredAll.Count == 0)
        {
            errors.Add("discovery.target.platformRoot.requiredAll must not be empty.");
        }

        if (manifest.Discovery.Target.PlatformRoot.RequiredAny.Count == 0)
        {
            errors.Add("discovery.target.platformRoot.requiredAny must not be empty.");
        }

        foreach (var segment in EnumeratePathSegments(manifest))
        {
            if (segment.Contains("..", StringComparison.Ordinal))
            {
                errors.Add($"Path segment must not contain '..': '{segment}'.");
            }

            if (Path.IsPathRooted(segment))
            {
                errors.Add($"Path segment must be repository-relative, not absolute: '{segment}'.");
            }
        }

        return errors;
    }

    private static IEnumerable<string> EnumeratePathSegments(RepositoryLayoutManifest manifest)
    {
        foreach (var profile in new[] { manifest.Discovery.Current, manifest.Discovery.Target })
        {
            foreach (var path in profile.CsmPlatformRoot.RequiredAll)
            {
                yield return path;
            }

            foreach (var path in profile.CsmPlatformRoot.RequiredAny)
            {
                yield return path;
            }

            foreach (var path in profile.DefaultApplicationRoots)
            {
                yield return path;
            }

            foreach (var path in profile.DeployableProjectScanRoots)
            {
                yield return path;
            }

            yield return profile.RuntimeAgentProjectRelative;
            yield return profile.WebAppAngularManifestRelative;

            foreach (var path in profile.PlatformRoot.RequiredAll)
            {
                yield return path;
            }

            foreach (var path in profile.PlatformRoot.RequiredAny)
            {
                yield return path;
            }

            foreach (var path in profile.RuntimeAgentDockerCopyRoots)
            {
                yield return path;
            }
        }

        foreach (var product in manifest.Products)
        {
            foreach (var pair in product.Paths.Values)
            {
                if (!string.IsNullOrWhiteSpace(pair.Current))
                {
                    yield return pair.Current;
                }

                if (!string.IsNullOrWhiteSpace(pair.Target))
                {
                    yield return pair.Target;
                }
            }
        }

        foreach (var solution in manifest.Solutions)
        {
            if (!string.IsNullOrWhiteSpace(solution.Path.Current))
            {
                yield return solution.Path.Current;
            }

            if (!string.IsNullOrWhiteSpace(solution.Path.Target))
            {
                yield return solution.Path.Target;
            }
        }
    }
}
