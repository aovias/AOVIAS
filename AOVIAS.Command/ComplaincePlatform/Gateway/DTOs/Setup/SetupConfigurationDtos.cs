using System.ComponentModel.DataAnnotations;

namespace Gateway.DTOs.Setup;

public sealed class SetupStatusDto
{
    public bool Completed { get; init; }
    public bool LockFilePresent { get; init; }
    public bool ConfigurationFilePresent { get; init; }
    public string? CompletedAtUtc { get; init; }
    public string MarkerSource { get; init; } = "none";
}

public sealed class DatabaseConnectionTestRequest
{
    [Required]
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class DatabaseConnectionTestResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int? ServerVersionMajor { get; init; }
}

public sealed class SetupConfigureRequest
{
    public DatabaseSetupSection? Database { get; set; }
    public SmtpSetupSection? Smtp { get; set; }
    public StorageSetupSection? Storage { get; set; }
    public AiSetupSection? Ai { get; set; }
    public AuthSetupSection? Auth { get; set; }
}

public sealed class DatabaseSetupSection
{
    public string? ConnectionString { get; set; }
    public string? Provider { get; set; } = "SqlServer";
}

public sealed class SmtpSetupSection
{
    public string? Host { get; set; }
    public int? Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string? FromAddress { get; set; }
    public bool UseSsl { get; set; } = true;
}

public sealed class StorageSetupSection
{
    public string? Provider { get; set; } = "Local";
    public string? RootPath { get; set; }
    public string? ConnectionString { get; set; }
    public string? ContainerName { get; set; }
}

public sealed class AiSetupSection
{
    public string? Provider { get; set; }
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class AuthSetupSection
{
    public string? JwtIssuer { get; set; }
    public string? JwtAudience { get; set; }
    public string? JwtSecretKey { get; set; }
    public int? ExpirationMinutes { get; set; }
}

public sealed class SetupConfigureResultDto
{
    public bool Saved { get; init; }
    public string Path { get; init; } = string.Empty;
    public IReadOnlyList<string> SectionsWritten { get; init; } = Array.Empty<string>();
}

public sealed class SetupCompleteResultDto
{
    public bool Completed { get; init; }
    public string LockPath { get; init; } = string.Empty;
    public IReadOnlyList<HealthPingResultDto> HealthPings { get; init; } = Array.Empty<HealthPingResultDto>();
}

public sealed class HealthPingResultDto
{
    public string Service { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public bool Success { get; init; }
    public int? StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
}
