using System.Text.Json;
using Aovias.Publishing.Contracts;

namespace Aovias.Publishing.Core.Discovery;

/// <summary>
/// Loads <see cref="DeploymentMetadata"/> from <c>deployment.metadata.json</c>
/// adjacent to the discovered project.
/// </summary>
public static class DeploymentMetadataReader
{
    public const string FileName = "deployment.metadata.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static DeploymentMetadata LoadRequired(ProjectDiscoveryResult discovery)
    {
        var path = ResolvePath(discovery.ProjectPath, discovery.ProjectRoot);
        if (path is null)
        {
            throw new InvalidOperationException(
                $"Required {FileName} was not found for project '{discovery.ProjectPath}'. "
                + "Every deployable service must declare deployment metadata.");
        }

        return Load(path);
    }

    public static DeploymentMetadata? TryLoad(string projectPath, string? projectRoot = null)
    {
        var path = ResolvePath(projectPath, projectRoot);
        if (path is null)
        {
            return null;
        }

        try
        {
            return Load(path);
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            // "Try" contract: callers (e.g. SolutionDeployableProjectDiscoverer) treat a null
            // return as "not a deployable project" and skip it with a warning, the same as a
            // missing file. A malformed or legacy-schema deployment.metadata.json discovered
            // during a solution-wide scan must not crash discovery for every other project;
            // LoadRequired/Load remain strict for the single-service required-metadata path.
            return null;
        }
    }

    public static string? ResolvePath(string projectPath, string? projectRoot = null)
    {
        var candidates = new List<string>();
        var projectDirectory = Path.GetDirectoryName(projectPath);
        if (!string.IsNullOrWhiteSpace(projectDirectory))
        {
            candidates.Add(Path.Combine(projectDirectory, FileName));
        }

        if (!string.IsNullOrWhiteSpace(projectRoot))
        {
            candidates.Add(Path.Combine(projectRoot, FileName));
        }

        return candidates.Distinct(StringComparer.OrdinalIgnoreCase).FirstOrDefault(File.Exists);
    }

    private static DeploymentMetadata Load(string path)
    {
        var json = File.ReadAllText(path);
        var metadata = JsonSerializer.Deserialize<DeploymentMetadata>(json, JsonOptions)
                       ?? throw new InvalidOperationException($"Unable to deserialize {FileName} at '{path}'.");

        if (metadata.SchemaVersion <= 0)
        {
            throw new InvalidOperationException($"{FileName} at '{path}' must declare a positive schemaVersion.");
        }

        if (string.IsNullOrWhiteSpace(metadata.ServiceId))
        {
            throw new InvalidOperationException($"{FileName} at '{path}' must declare serviceId.");
        }

        if (metadata.Ports.ContainerPort <= 0)
        {
            throw new InvalidOperationException($"{FileName} at '{path}' must declare ports.containerPort.");
        }

        return metadata;
    }
}
