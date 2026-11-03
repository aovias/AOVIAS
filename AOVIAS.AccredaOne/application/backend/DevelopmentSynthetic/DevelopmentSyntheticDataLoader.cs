using System.Text.Json;

namespace AccredaOne.Api.DevelopmentSynthetic;

internal static class DevelopmentSyntheticDataLoader
{
    public const string SyntheticClassification = "synthetic";
    public const string ExpectedPackId = "mvp-development-synthetic-discovery";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
    };

    public static DevelopmentSyntheticDataset LoadFromDirectory(string directoryPath)
    {
        var manifest = DeserializeFile<DevelopmentSyntheticManifest>(Path.Combine(directoryPath, "manifest.json"));
        if (!string.Equals(manifest.PackId, ExpectedPackId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Unexpected development synthetic pack id '{manifest.PackId}'.");
        }

        if (!string.Equals(manifest.Environment, "development", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Development synthetic dataset must be marked for development environment.");
        }

        return new DevelopmentSyntheticDataset
        {
            Manifest = manifest,
            Frameworks = DeserializeFile<List<DevelopmentSyntheticFrameworkRecord>>(Path.Combine(directoryPath, "frameworks.json")),
            Sources = DeserializeFile<List<DevelopmentSyntheticSourceRecord>>(Path.Combine(directoryPath, "sources.json")),
            Requirements = DeserializeFile<List<DevelopmentSyntheticRequirementRecord>>(Path.Combine(directoryPath, "requirements.json")),
            Explanations = DeserializeFile<List<DevelopmentSyntheticExplanationRecord>>(Path.Combine(directoryPath, "explanations.json")),
        };
    }

    public static string ResolveDatasetDirectory()
    {
        var baseDirectory = AppContext.BaseDirectory;
        var candidate = Path.Combine(baseDirectory, "Data", "DevelopmentSynthetic");
        if (Directory.Exists(candidate))
        {
            return candidate;
        }

        var projectRelative = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", "Data", "DevelopmentSynthetic"));
        if (Directory.Exists(projectRelative))
        {
            return projectRelative;
        }

        throw new DirectoryNotFoundException("Development synthetic dataset directory was not found.");
    }

    private static T DeserializeFile<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Development synthetic file not found: {path}");
        }

        var json = File.ReadAllText(path);
        var value = JsonSerializer.Deserialize<T>(json, JsonOptions);
        if (value is null)
        {
            throw new InvalidOperationException($"Development synthetic file '{path}' did not deserialize.");
        }

        return value;
    }
}
