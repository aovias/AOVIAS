using System.Text.Json;
using System.Text.Json.Nodes;
using Gateway.DTOs.Setup;
using Microsoft.Extensions.Caching.Memory;

namespace Gateway.Services.Installation;

public interface IInstallationStateService
{
    SetupStatusDto GetStatus();
    bool IsCompleted();
    Task<SetupConfigureResultDto> SaveConfigurationAsync(SetupConfigureRequest request, CancellationToken cancellationToken);
    Task<SetupCompleteResultDto> CompleteAsync(IReadOnlyList<HealthPingResultDto> healthPings, CancellationToken cancellationToken);
}

/// <summary>
/// Persists installation state under App_Data (lock marker + installation JSON).
/// Never writes secrets into source-controlled appsettings*.json.
/// </summary>
public sealed class InstallationStateService : IInstallationStateService
{
    public const string MemoryKey = "Installation:Completed";
    public const string LockFileName = "installation.lock";
    public const string ConfigFileName = "aovias.installation.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<InstallationStateService> _logger;

    public InstallationStateService(
        IWebHostEnvironment env,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<InstallationStateService> logger)
    {
        _env = env;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public string AppDataDirectory => Path.Combine(_env.ContentRootPath, "App_Data");
    public string LockFilePath => Path.Combine(AppDataDirectory, LockFileName);
    public string ConfigFilePath => Path.Combine(AppDataDirectory, ConfigFileName);

    public bool IsCompleted()
    {
        if (_cache.TryGetValue(MemoryKey, out bool cached) && cached)
            return true;

        if (_configuration.GetValue<bool>("Installation:Completed"))
            return true;

        if (File.Exists(LockFilePath))
            return true;

        return false;
    }

    public SetupStatusDto GetStatus()
    {
        var lockPresent = File.Exists(LockFilePath);
        var configPresent = File.Exists(ConfigFilePath);
        var configCompleted = _configuration.GetValue<bool>("Installation:Completed");
        var memoryCompleted = _cache.TryGetValue(MemoryKey, out bool mem) && mem;
        var completed = lockPresent || configCompleted || memoryCompleted;

        string? completedAt = null;
        if (lockPresent)
        {
            try
            {
                var text = File.ReadAllText(LockFilePath).Trim();
                if (!string.IsNullOrWhiteSpace(text))
                    completedAt = text;
            }
            catch
            {
                // ignore read failures for status
            }
        }

        var source = lockPresent ? "lock-file"
            : memoryCompleted ? "memory"
            : configCompleted ? "configuration"
            : "none";

        return new SetupStatusDto
        {
            Completed = completed,
            LockFilePresent = lockPresent,
            ConfigurationFilePresent = configPresent,
            CompletedAtUtc = completedAt,
            MarkerSource = source
        };
    }

    public async Task<SetupConfigureResultDto> SaveConfigurationAsync(
        SetupConfigureRequest request,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(AppDataDirectory);

        JsonObject root;
        if (File.Exists(ConfigFilePath))
        {
            var existing = await File.ReadAllTextAsync(ConfigFilePath, cancellationToken);
            root = JsonNode.Parse(string.IsNullOrWhiteSpace(existing) ? "{}" : existing) as JsonObject
                   ?? new JsonObject();
        }
        else
        {
            root = new JsonObject();
        }

        var sections = new List<string>();

        if (request.Database is not null)
        {
            root["ConnectionStrings"] = new JsonObject
            {
                ["DefaultConnection"] = request.Database.ConnectionString
            };
            root["Database"] = new JsonObject
            {
                ["Provider"] = request.Database.Provider ?? "SqlServer"
            };
            sections.Add("Database");
        }

        if (request.Smtp is not null)
        {
            root["Smtp"] = JsonSerializer.SerializeToNode(request.Smtp, JsonOptions);
            sections.Add("Smtp");
        }

        if (request.Storage is not null)
        {
            root["Storage"] = JsonSerializer.SerializeToNode(request.Storage, JsonOptions);
            sections.Add("Storage");
        }

        if (request.Ai is not null)
        {
            root["Ai"] = JsonSerializer.SerializeToNode(request.Ai, JsonOptions);
            sections.Add("Ai");
        }

        if (request.Auth is not null)
        {
            var jwt = root["JwtSettings"] as JsonObject ?? new JsonObject();
            if (!string.IsNullOrWhiteSpace(request.Auth.JwtIssuer))
                jwt["Issuer"] = request.Auth.JwtIssuer;
            if (!string.IsNullOrWhiteSpace(request.Auth.JwtAudience))
                jwt["Audience"] = request.Auth.JwtAudience;
            if (!string.IsNullOrWhiteSpace(request.Auth.JwtSecretKey))
                jwt["SecretKey"] = request.Auth.JwtSecretKey;
            if (request.Auth.ExpirationMinutes.HasValue)
                jwt["ExpirationMinutes"] = request.Auth.ExpirationMinutes.Value;
            root["JwtSettings"] = jwt;
            sections.Add("Auth");
        }

        root["Installation"] = new JsonObject
        {
            ["ConfiguredAtUtc"] = DateTime.UtcNow.ToString("O"),
            ["Completed"] = IsCompleted()
        };

        await File.WriteAllTextAsync(ConfigFilePath, root.ToJsonString(JsonOptions), cancellationToken);
        _logger.LogInformation("Wrote installation configuration to {Path} sections={Sections}", ConfigFilePath, string.Join(',', sections));

        return new SetupConfigureResultDto
        {
            Saved = true,
            Path = ConfigFilePath,
            SectionsWritten = sections
        };
    }

    public async Task<SetupCompleteResultDto> CompleteAsync(
        IReadOnlyList<HealthPingResultDto> healthPings,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(AppDataDirectory);
        var stamp = DateTime.UtcNow.ToString("O");
        await File.WriteAllTextAsync(LockFilePath, stamp, cancellationToken);

        if (File.Exists(ConfigFilePath))
        {
            try
            {
                var node = JsonNode.Parse(await File.ReadAllTextAsync(ConfigFilePath, cancellationToken)) as JsonObject
                           ?? new JsonObject();
                node["Installation"] = new JsonObject
                {
                    ["Completed"] = true,
                    ["CompletedAtUtc"] = stamp
                };
                await File.WriteAllTextAsync(ConfigFilePath, node.ToJsonString(JsonOptions), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not update Installation.Completed inside configuration file");
            }
        }

        _cache.Set(MemoryKey, true);

        return new SetupCompleteResultDto
        {
            Completed = true,
            LockPath = LockFilePath,
            HealthPings = healthPings
        };
    }
}
