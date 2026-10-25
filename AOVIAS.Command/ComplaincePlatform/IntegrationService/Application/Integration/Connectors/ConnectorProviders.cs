using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using IntegrationService.Application.Contracts;
using IntegrationService.Application.Integration.Platform;
using IntegrationService.Domain.ReadModels;

namespace IntegrationService.Application.Integration.Connectors;

public sealed class ConnectorRegistry(IEnumerable<IConnectorProvider> providers) : IConnectorRegistry
{
    private readonly Dictionary<string, IConnectorProvider> _map = providers.ToDictionary(p => p.ProviderType, StringComparer.OrdinalIgnoreCase);
    public IConnectorProvider Resolve(string providerType) =>
        _map.TryGetValue(providerType, out var p) ? p : throw new InvalidOperationException($"Unknown connector provider: {providerType}");
    public IReadOnlyList<string> ListProviderTypes() => _map.Keys.OrderBy(x => x).ToList();
}

public abstract class ConnectorProviderBase : IConnectorProvider
{
    public abstract string ProviderType { get; }
    public virtual Task<ConnectorValidationResult> ValidateConfigurationAsync(string configurationJson, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(configurationJson)) return Task.FromResult(new ConnectorValidationResult { IsValid = false, Errors = ["Configuration required"] });
        return Task.FromResult(new ConnectorValidationResult { IsValid = true });
    }
    public virtual Task<ConnectorHealthResult> CheckHealthAsync(IntegrationConnector connector, CancellationToken ct = default)
        => Task.FromResult(new ConnectorHealthResult { Status = "Healthy", LatencyMs = 1, Message = "OK" });
    public virtual Task<ConnectorPullResult> PullAsync(IntegrationConnector connector, SyncContext context, CancellationToken ct = default)
        => Task.FromResult(new ConnectorPullResult());
}

public sealed class CsvImportConnectorProvider : ConnectorProviderBase
{
    public override string ProviderType => "csv-import";
    public override Task<ConnectorPullResult> PullAsync(IntegrationConnector connector, SyncContext context, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(connector.ConfigurationJson);
        var content = doc.RootElement.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (lines.Length < 2) return Task.FromResult(new ConnectorPullResult());
        var headers = lines[0].Split(',').Select(h => h.Trim()).ToArray();
        var records = new List<ExternalRecord>();
        for (var i = 1; i < lines.Length; i++)
        {
            var vals = lines[i].Split(',');
            var fields = new Dictionary<string, string?>();
            for (var j = 0; j < headers.Length && j < vals.Length; j++) fields[headers[j]] = vals[j].Trim();
            records.Add(new ExternalRecord { ExternalId = fields.GetValueOrDefault("ExternalEmployeeNumber") ?? $"row-{i}", Fields = fields, RecordHash = Hash(fields) });
        }
        return Task.FromResult(new ConnectorPullResult { Records = records });
    }
    private static string Hash(Dictionary<string, string?> f) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(f))));
}

public sealed class JsonImportConnectorProvider : ConnectorProviderBase
{
    public override string ProviderType => "json-import";
    public override Task<ConnectorPullResult> PullAsync(IntegrationConnector connector, SyncContext context, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(connector.ConfigurationJson);
        if (!doc.RootElement.TryGetProperty("records", out var arr) || arr.ValueKind != JsonValueKind.Array)
            return Task.FromResult(new ConnectorPullResult());
        var records = arr.EnumerateArray().Select((el, i) =>
        {
            var fields = new Dictionary<string, string?>();
            foreach (var p in el.EnumerateObject()) fields[p.Name] = p.Value.ToString();
            return new ExternalRecord { ExternalId = fields.GetValueOrDefault("id") ?? $"json-{i}", Fields = fields };
        }).ToList();
        return Task.FromResult(new ConnectorPullResult { Records = records });
    }
}

public sealed class CsvExportConnectorProvider : ConnectorProviderBase
{
    public override string ProviderType => "csv-export";
}

public sealed class WebhookInboundConnectorProvider : ConnectorProviderBase
{
    public override string ProviderType => "webhook-inbound";
}

public sealed class WebhookOutboundConnectorProvider : ConnectorProviderBase
{
    public override string ProviderType => "webhook-outbound";
}

public sealed class LdapConnectorProvider : ConnectorProviderBase
{
    public override string ProviderType => "ldap";
    public override Task<ConnectorValidationResult> ValidateConfigurationAsync(string configurationJson, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(configurationJson);
        var errors = new List<string>();
        if (!doc.RootElement.TryGetProperty("host", out _)) errors.Add("host required");
        if (!doc.RootElement.TryGetProperty("baseDn", out _)) errors.Add("baseDn required");
        return Task.FromResult(new ConnectorValidationResult { IsValid = errors.Count == 0, Errors = errors });
    }
    public override async Task<ConnectorHealthResult> CheckHealthAsync(IntegrationConnector connector, CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();
        await Task.Delay(10, ct);
        return new ConnectorHealthResult { Status = "Degraded", LatencyMs = sw.ElapsedMilliseconds, Message = "LDAP connection test simulated" };
    }
    public override Task<ConnectorPullResult> PullAsync(IntegrationConnector connector, SyncContext context, CancellationToken ct = default)
    {
        var sample = new ExternalRecord
        {
            ExternalId = "ldap-sample-1",
            Fields = new Dictionary<string, string?> { ["ExternalEmployeeNumber"] = "LDAP001", ["displayName"] = "LDAP Sample User", ["mail"] = "ldap@example.com", ["department"] = "IT" }
        };
        return Task.FromResult(new ConnectorPullResult { Records = [sample], Watermark = DateTime.UtcNow.ToString("O") });
    }
}

public sealed class AzureAdConnectorProvider : ConnectorProviderBase
{
    public override string ProviderType => "azure-ad";
    public override Task<ConnectorValidationResult> ValidateConfigurationAsync(string configurationJson, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(configurationJson);
        var errors = new List<string>();
        if (!doc.RootElement.TryGetProperty("tenantId", out _)) errors.Add("tenantId required");
        if (!doc.RootElement.TryGetProperty("clientId", out _)) errors.Add("clientId required");
        return Task.FromResult(new ConnectorValidationResult { IsValid = errors.Count == 0, Errors = errors });
    }
    public override Task<ConnectorPullResult> PullAsync(IntegrationConnector connector, SyncContext context, CancellationToken ct = default)
    {
        var sample = new ExternalRecord
        {
            ExternalId = "aad-sample-1",
            Fields = new Dictionary<string, string?> { ["ExternalEmployeeNumber"] = "AAD001", ["displayName"] = "Azure AD User", ["userPrincipalName"] = "user@contoso.com", ["department"] = "Clinical" }
        };
        return Task.FromResult(new ConnectorPullResult { Records = [sample], Watermark = DateTime.UtcNow.ToString("O") });
    }
}

public sealed class StubConnectorProvider(string providerType) : ConnectorProviderBase
{
    public override string ProviderType => providerType;
    public override Task<ConnectorHealthResult> CheckHealthAsync(IntegrationConnector connector, CancellationToken ct = default)
        => Task.FromResult(new ConnectorHealthResult { Status = "Unknown", LatencyMs = 0, Message = "Stub provider — configure and extend" });
}

public static class ConnectorProviderRegistration
{
    public static readonly string[] StubTypes =
    [
        "hrms", "payroll", "attendance", "biometric", "lms", "his", "emr", "erp", "dms",
        "email", "sms", "whatsapp", "teams", "slack", "fhir", "hl7", "openEHR", "government-portal"
    ];
}
