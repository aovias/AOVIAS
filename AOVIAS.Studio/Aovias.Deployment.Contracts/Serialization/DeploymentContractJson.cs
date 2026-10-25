using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aovias.Deployment.Contracts.Serialization;

/// <summary>
/// Shared JSON serialization settings for deployment contracts.
/// </summary>
public static class DeploymentContractJson
{
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    public static string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, Options);

    public static T? Deserialize<T>(string json)
        => JsonSerializer.Deserialize<T>(json, Options);

    public static T? ReadFile<T>(string path)
        => File.Exists(path) ? Deserialize<T>(File.ReadAllText(path)) : default;

    public static void WriteFile<T>(string path, T value)
        => File.WriteAllText(path, Serialize(value));
}
