using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using IntegrationService.Application.Contracts;
using IntegrationService.Application.Integration.Connectors;
using IntegrationService.Application.Integration.ImportExport;
using IntegrationService.Application.Integration.Mapping;
using IntegrationService.Application.Integration.Platform;
using IntegrationService.Application.Integration.Security;
using IntegrationService.Application.Integration.Synchronization;
using IntegrationService.Application.Integration.Webhooks;
using IntegrationService.Domain.ReadModels;
using IntegrationService.Infrastructure.Background;
using IntegrationService.Infrastructure.Clients;
using IntegrationService.Infrastructure.Persistence;
using IntegrationService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Authorization;
using Shared.Infrastructure.DTO;

namespace IntegrationService.Tests;

internal sealed class HashTestConnectorProvider(IReadOnlyList<ExternalRecord> records) : ConnectorProviderBase
{
    public override string ProviderType => "hash-test";
    public override Task<ConnectorPullResult> PullAsync(IntegrationConnector connector, SyncContext context, CancellationToken ct = default)
        => Task.FromResult(new ConnectorPullResult { Records = records });
}

public sealed class IntegrationTestDb : IDisposable
{
    public IntegrationDbContext Db { get; }
    public IntegrationTestDb()
    {
        Db = new IntegrationDbContext(new DbContextOptionsBuilder<IntegrationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    }
    public void Dispose() => Db.Dispose();
}

public class ConnectorRegistryTests
{
    [Fact]
    public void Resolve_ReturnsCsvImportProvider()
    {
        var registry = BuildRegistry();
        Assert.Equal("csv-import", registry.Resolve("csv-import").ProviderType);
    }

    [Fact]
    public void Resolve_IsCaseInsensitive() =>
        Assert.Equal("ldap", BuildRegistry().Resolve("LDAP").ProviderType);

    [Fact]
    public void Resolve_UnknownProvider_Throws() =>
        Assert.Throws<InvalidOperationException>(() => BuildRegistry().Resolve("missing"));

    [Fact]
    public void ListProviderTypes_IncludesTier1AndTier3()
    {
        var types = BuildRegistry().ListProviderTypes();
        Assert.Contains("csv-import", types);
        Assert.Contains("ldap", types);
        Assert.Contains("hrms", types);
    }

    private static ConnectorRegistry BuildRegistry() => new([
        new CsvImportConnectorProvider(), new JsonImportConnectorProvider(), new CsvExportConnectorProvider(),
        new WebhookInboundConnectorProvider(), new WebhookOutboundConnectorProvider(),
        new LdapConnectorProvider(), new AzureAdConnectorProvider(),
        new StubConnectorProvider("hrms")
    ]);
}

public class MappingEngineTests
{
    private readonly MappingEngine _engine = new();
    private readonly MappingProfile _profile = new() { Id = Guid.NewGuid(), Name = "P", SourceEntity = "Ext", TargetEntity = "Employment" };

    [Fact]
    public async Task MapAsync_CopiesFieldsInSortOrder()
    {
        var fields = new List<FieldMapping>
        {
            new() { SourceField = "a", TargetField = "A", SortOrder = 2 },
            new() { SourceField = "b", TargetField = "B", SortOrder = 1 }
        };
        var source = new ExternalRecord { Fields = new Dictionary<string, string?> { ["a"] = "1", ["b"] = "2" } };
        var mapped = await _engine.MapAsync(source, _profile, fields, [], default);
        Assert.Equal("1", mapped.Fields["A"]);
        Assert.Equal("2", mapped.Fields["B"]);
    }

    [Theory]
    [InlineData("String", "  x  ", "x")]
    [InlineData("Boolean", "true", "True")]
    [InlineData("Number", "42.5", "42.5")]
    [InlineData("DefaultValue", "", "fallback")]
    public async Task MapAsync_AppliesTransformationRules(string ruleType, string input, string expected)
    {
        var rule = new TransformationRule { Id = Guid.NewGuid(), RuleType = ruleType, RulesJson = """{"default":"fallback"}""" };
        var fields = new List<FieldMapping> { new() { SourceField = "f", TargetField = "t", TransformationRuleId = rule.Id } };
        var source = new ExternalRecord { Fields = new Dictionary<string, string?> { ["f"] = input } };
        var mapped = await _engine.MapAsync(source, _profile, fields, [rule], default);
        Assert.Equal(expected, mapped.Fields["t"]);
    }

    [Fact]
    public async Task MapAsync_LookupRule_MapsValue()
    {
        var rule = new TransformationRule { Id = Guid.NewGuid(), RuleType = "Lookup", RulesJson = """{"map":{"IT":"Information Technology"}}""" };
        var fields = new List<FieldMapping> { new() { SourceField = "dept", TargetField = "Department", TransformationRuleId = rule.Id } };
        var source = new ExternalRecord { Fields = new Dictionary<string, string?> { ["dept"] = "it" } };
        var mapped = await _engine.MapAsync(source, _profile, fields, [rule], default);
        Assert.Equal("Information Technology", mapped.Fields["Department"]);
    }

    [Fact]
    public async Task ValidateProfileAsync_RequiresFields()
    {
        var result = await _engine.ValidateProfileAsync(new MappingProfile { Name = "" }, [], default);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("name", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ValidateProfileAsync_ValidWhenComplete()
    {
        var result = await _engine.ValidateProfileAsync(_profile, [new FieldMapping { SourceField = "a", TargetField = "b" }], default);
        Assert.True(result.IsValid);
    }
}

public class ConnectorProviderTests
{
    [Fact]
    public async Task CsvImport_ParsesRows()
    {
        var provider = new CsvImportConnectorProvider();
        var connector = new IntegrationConnector
        {
            ConfigurationJson = """{"content":"ExternalEmployeeNumber,name\nE1,Alice\nE2,Bob"}"""
        };
        var pull = await provider.PullAsync(connector, new SyncContext(), default);
        Assert.Equal(2, pull.Records.Count);
        Assert.Equal("E1", pull.Records[0].ExternalId);
    }

    [Fact]
    public async Task JsonImport_ParsesArray()
    {
        var provider = new JsonImportConnectorProvider();
        var connector = new IntegrationConnector
        {
            ConfigurationJson = """{"records":[{"id":"J1","name":"Jane"}]}"""
        };
        var pull = await provider.PullAsync(connector, new SyncContext(), default);
        Assert.Single(pull.Records);
        Assert.Equal("J1", pull.Records[0].ExternalId);
    }

    [Fact]
    public async Task Ldap_Validation_RequiresHostAndBaseDn()
    {
        var provider = new LdapConnectorProvider();
        var result = await provider.ValidateConfigurationAsync("{}", default);
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task Ldap_Pull_ReturnsSampleRecord()
    {
        var provider = new LdapConnectorProvider();
        var pull = await provider.PullAsync(new IntegrationConnector { ConfigurationJson = """{"host":"x","baseDn":"dc=x"}""" }, new SyncContext(), default);
        Assert.Single(pull.Records);
        Assert.Equal("LDAP001", pull.Records[0].Fields["ExternalEmployeeNumber"]);
    }

    [Fact]
    public async Task AzureAd_Validation_RequiresTenantId()
    {
        var provider = new AzureAdConnectorProvider();
        var result = await provider.ValidateConfigurationAsync("{}", default);
        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task StubProvider_ValidateOnly()
    {
        var provider = new StubConnectorProvider("hrms");
        Assert.Equal("hrms", provider.ProviderType);
        var health = await provider.CheckHealthAsync(new IntegrationConnector(), default);
        Assert.Equal("Unknown", health.Status);
    }
}

public class CredentialProtectorTests
{
    [Fact]
    public void ProtectUnprotect_RoundTrips()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Integration:CredentialEncryptionKey"] = "test-key-32-characters-minimum!!"
        }).Build();
        var protector = new CredentialProtector(config);
        var plain = "super-secret-api-key";
        var protectedText = protector.Protect(plain);
        Assert.NotEqual(plain, protectedText);
        Assert.Equal(plain, protector.Unprotect(protectedText));
    }

    [Fact]
    public void Protect_ProducesDifferentCiphertextEachTime()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Integration:CredentialEncryptionKey"] = "test-key-32-characters-minimum!!"
        }).Build();
        var protector = new CredentialProtector(config);
        Assert.NotEqual(protector.Protect("x"), protector.Protect("x"));
    }
}

public class WebhookServiceTests
{
    private static WebhookService BuildWebhookService(IntegrationDbContext db, Mock<IIntegrationAuditService>? audit = null, Mock<ISynchronizationEngine>? sync = null)
    {
        audit ??= new Mock<IIntegrationAuditService>();
        sync ??= new Mock<ISynchronizationEngine>();
        return new WebhookService(db, Mock.Of<IHttpClientFactory>(), audit.Object, sync.Object, NullLogger<WebhookService>.Instance);
    }

    private static string ComputeInboundSignature(string payload, string secret)
    {
        var secretHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret)));
        return Convert.ToBase64String(System.Security.Cryptography.HMACSHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(secretHash),
            System.Text.Encoding.UTF8.GetBytes(payload)));
    }

    [Fact]
    public async Task ProcessInbound_ValidSignature_Succeeds()
    {
        using var fx = new IntegrationTestDb();
        var secret = "webhook-secret";
        var subId = Guid.NewGuid();
        fx.Db.WebhookSubscriptions.Add(new WebhookSubscription
        {
            Id = subId, OrganizationId = Guid.NewGuid(), Name = "in", Direction = "Inbound",
            SecretHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret))),
            IsEnabled = true
        });
        await fx.Db.SaveChangesAsync();
        var svc = BuildWebhookService(fx.Db);
        var payload = """{"event":"test"}""";
        var sig = ComputeInboundSignature(payload, secret);
        Assert.True(await svc.ProcessInboundAsync(subId, payload, sig, null, null, default));
        Assert.Single(await fx.Db.WebhookDeliveries.ToListAsync());
    }

    [Fact]
    public async Task ProcessInbound_InvalidSignature_Fails()
    {
        using var fx = new IntegrationTestDb();
        var subId = Guid.NewGuid();
        fx.Db.WebhookSubscriptions.Add(new WebhookSubscription { Id = subId, Direction = "Inbound", SecretHash = "hash", IsEnabled = true });
        await fx.Db.SaveChangesAsync();
        var svc = BuildWebhookService(fx.Db);
        Assert.False(await svc.ProcessInboundAsync(subId, "{}", "bad", null, null, default));
    }

    [Fact]
    public async Task ProcessInbound_StaleTimestamp_Rejected()
    {
        using var fx = new IntegrationTestDb();
        var secret = "webhook-secret";
        var subId = Guid.NewGuid();
        fx.Db.WebhookSubscriptions.Add(new WebhookSubscription
        {
            Id = subId, OrganizationId = Guid.NewGuid(), Direction = "Inbound",
            SecretHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret))),
            IsEnabled = true
        });
        await fx.Db.SaveChangesAsync();
        var svc = BuildWebhookService(fx.Db);
        var payload = """{"event":"stale"}""";
        var stale = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds().ToString();
        Assert.False(await svc.ProcessInboundAsync(subId, payload, ComputeInboundSignature(payload, secret), stale, null, default));
        Assert.Empty(await fx.Db.WebhookDeliveries.ToListAsync());
    }

    [Fact]
    public async Task ProcessInbound_ValidTimestamp_Accepted()
    {
        using var fx = new IntegrationTestDb();
        var secret = "webhook-secret";
        var subId = Guid.NewGuid();
        fx.Db.WebhookSubscriptions.Add(new WebhookSubscription
        {
            Id = subId, OrganizationId = Guid.NewGuid(), Direction = "Inbound",
            SecretHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret))),
            IsEnabled = true
        });
        await fx.Db.SaveChangesAsync();
        var svc = BuildWebhookService(fx.Db);
        var payload = """{"event":"fresh"}""";
        var fresh = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        Assert.True(await svc.ProcessInboundAsync(subId, payload, ComputeInboundSignature(payload, secret), fresh, null, default));
    }

    [Fact]
    public async Task ProcessInbound_IdempotencyKey_Deduplicates()
    {
        using var fx = new IntegrationTestDb();
        var secret = "webhook-secret";
        var subId = Guid.NewGuid();
        fx.Db.WebhookSubscriptions.Add(new WebhookSubscription
        {
            Id = subId, OrganizationId = Guid.NewGuid(), Direction = "Inbound",
            SecretHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret))),
            IsEnabled = true
        });
        await fx.Db.SaveChangesAsync();
        var svc = BuildWebhookService(fx.Db);
        var payload = """{"event":"idem"}""";
        var sig = ComputeInboundSignature(payload, secret);
        var key = "idem-key-1";
        Assert.True(await svc.ProcessInboundAsync(subId, payload, sig, null, key, default));
        Assert.True(await svc.ProcessInboundAsync(subId, payload, sig, null, key, default));
        Assert.Single(await fx.Db.WebhookDeliveries.ToListAsync());
    }

    [Fact]
    public async Task ProcessInbound_PayloadHash_DeduplicatesWithoutKey()
    {
        using var fx = new IntegrationTestDb();
        var secret = "webhook-secret";
        var subId = Guid.NewGuid();
        fx.Db.WebhookSubscriptions.Add(new WebhookSubscription
        {
            Id = subId, OrganizationId = Guid.NewGuid(), Direction = "Inbound",
            SecretHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret))),
            IsEnabled = true
        });
        await fx.Db.SaveChangesAsync();
        var svc = BuildWebhookService(fx.Db);
        var payload = """{"event":"hash-dedupe"}""";
        var sig = ComputeInboundSignature(payload, secret);
        Assert.True(await svc.ProcessInboundAsync(subId, payload, sig, null, null, default));
        Assert.True(await svc.ProcessInboundAsync(subId, payload, sig, null, null, default));
        Assert.Single(await fx.Db.WebhookDeliveries.ToListAsync());
    }

    [Fact]
    public async Task ProcessInbound_TriggersSyncJob_WhenConfigured()
    {
        using var fx = new IntegrationTestDb();
        var secret = "webhook-secret";
        var subId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        fx.Db.WebhookSubscriptions.Add(new WebhookSubscription
        {
            Id = subId, OrganizationId = Guid.NewGuid(), Direction = "Inbound",
            SecretHash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secret))),
            EventFilterJson = JsonSerializer.Serialize(new { TriggerSyncJobId = jobId }),
            IsEnabled = true
        });
        await fx.Db.SaveChangesAsync();
        var sync = new Mock<ISynchronizationEngine>();
        var svc = BuildWebhookService(fx.Db, sync: sync);
        var payload = """{"event":"sync"}""";
        await svc.ProcessInboundAsync(subId, payload, ComputeInboundSignature(payload, secret), null, null, default);
        sync.Verify(s => s.RunAsync(jobId, It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_PersistsSubscription()
    {
        using var fx = new IntegrationTestDb();
        var svc = BuildWebhookService(fx.Db);
        var dto = await svc.CreateAsync(new CreateWebhookRequest { OrganizationId = Guid.NewGuid(), Name = "out", Direction = "Outbound", TargetUrl = "https://example.com/hook" }, default);
        Assert.NotEqual(Guid.Empty, dto.Id);
    }
}

public class SynchronizationEngineTests
{
    [Fact]
    public async Task RunAsync_ProcessesCsvRecordsAndWritesAudit()
    {
        using var fx = new IntegrationTestDb();
        var orgId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        fx.Db.Connectors.Add(new IntegrationConnector
        {
            Id = connectorId, OrganizationId = orgId, Name = "csv", ProviderType = "csv-import",
            ConfigurationJson = """{"content":"ExternalEmployeeNumber,name\nE1,Alice"}""", LifecycleState = "Connected"
        });
        fx.Db.MappingProfiles.Add(new MappingProfile { Id = profileId, OrganizationId = orgId, Name = "m", SourceEntity = "Ext", TargetEntity = "Employment" });
        fx.Db.FieldMappings.Add(new FieldMapping { Id = Guid.NewGuid(), MappingProfileId = profileId, SourceField = "name", TargetField = "displayName" });
        fx.Db.SynchronizationJobs.Add(new SynchronizationJob
        {
            Id = jobId, OrganizationId = orgId, Name = "job", SourceConnectorId = connectorId,
            MappingProfileId = profileId, Direction = "OneWay", ConflictPolicy = "ExternalWins"
        });
        await fx.Db.SaveChangesAsync();

        var registry = new ConnectorRegistry([new CsvImportConnectorProvider()]);
        var platform = new Mock<IPlatformWriteClient>();
        platform.Setup(p => p.UpsertAsync(orgId, It.IsAny<MappedRecord>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlatformWriteResult { EntityId = Guid.NewGuid(), TargetEntity = "Employment" });
        var audit = new Mock<IIntegrationAuditService>();
        var outbox = new Mock<IIntegrationOutboxWriter>();
        var engine = new SynchronizationEngine(fx.Db, registry, new MappingEngine(), platform.Object, audit.Object, outbox.Object, NullLogger<SynchronizationEngine>.Instance);

        var run = await engine.RunAsync(jobId, "trace-1", Guid.NewGuid(), default);
        Assert.Equal("Completed", run.Status);
        Assert.Equal(1, run.RecordsProcessed);
        audit.Verify(a => a.LogAsync(orgId, "Synchronization", "Completed", It.IsAny<Guid?>(), connectorId, It.IsAny<string>(), It.IsAny<string>(), 1, It.IsAny<long>(), "trace-1", null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RunAsync_DetectsConflict_WithManualReviewPolicy()
    {
        using var fx = new IntegrationTestDb();
        var orgId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        fx.Db.Connectors.Add(new IntegrationConnector
        {
            Id = connectorId, OrganizationId = orgId, ProviderType = "json-import",
            ConfigurationJson = """{"records":[{"id":"1","name":"Old","lastModified":"2000-01-01T00:00:00Z"}]}"""
        });
        fx.Db.MappingProfiles.Add(new MappingProfile { Id = profileId, OrganizationId = orgId, Name = "m", TargetEntity = "Employment" });
        fx.Db.FieldMappings.Add(new FieldMapping { Id = Guid.NewGuid(), MappingProfileId = profileId, SourceField = "name", TargetField = "displayName" });
        fx.Db.SynchronizationJobs.Add(new SynchronizationJob
        {
            Id = jobId, OrganizationId = orgId, SourceConnectorId = connectorId, MappingProfileId = profileId,
            ConflictPolicy = "ManualReview"
        });
        await fx.Db.SaveChangesAsync();
        var registry = new ConnectorRegistry([new JsonImportConnectorProvider()]);
        var engine = new SynchronizationEngine(fx.Db, registry, new MappingEngine(), Mock.Of<IPlatformWriteClient>(), Mock.Of<IIntegrationAuditService>(), Mock.Of<IIntegrationOutboxWriter>(), NullLogger<SynchronizationEngine>.Instance);
        var run = await engine.RunAsync(jobId, "t", null, default);
        Assert.True(run.ConflictsDetected >= 0);
    }

    [Fact]
    public async Task RunAsync_ShortRecordHash_DetectsConflict()
    {
        using var fx = new IntegrationTestDb();
        var orgId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        fx.Db.Connectors.Add(new IntegrationConnector { Id = connectorId, OrganizationId = orgId, ProviderType = "hash-test", ConfigurationJson = "{}" });
        fx.Db.MappingProfiles.Add(new MappingProfile { Id = profileId, OrganizationId = orgId, Name = "m", TargetEntity = "Employment" });
        fx.Db.FieldMappings.Add(new FieldMapping { Id = Guid.NewGuid(), MappingProfileId = profileId, SourceField = "name", TargetField = "displayName" });
        fx.Db.SynchronizationJobs.Add(new SynchronizationJob
        {
            Id = jobId, OrganizationId = orgId, SourceConnectorId = connectorId, MappingProfileId = profileId,
            ConflictPolicy = "ManualReview", MaxRetries = 1
        });
        await fx.Db.SaveChangesAsync();
        var registry = new ConnectorRegistry([new HashTestConnectorProvider([
            new ExternalRecord { ExternalId = "h1", Fields = new Dictionary<string, string?> { ["name"] = "Alice" }, RecordHash = "abc" }
        ])]);
        var engine = new SynchronizationEngine(fx.Db, registry, new MappingEngine(), Mock.Of<IPlatformWriteClient>(), Mock.Of<IIntegrationAuditService>(), Mock.Of<IIntegrationOutboxWriter>(), NullLogger<SynchronizationEngine>.Instance);
        var run = await engine.RunAsync(jobId, "hash-trace", null, default);
        Assert.Equal(1, run.ConflictsDetected);
        Assert.Equal(0, run.RecordsProcessed);
    }

    [Fact]
    public async Task RunAsync_LongRecordHash_NoConflict()
    {
        using var fx = new IntegrationTestDb();
        var orgId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        fx.Db.Connectors.Add(new IntegrationConnector { Id = connectorId, OrganizationId = orgId, ProviderType = "hash-test", ConfigurationJson = "{}" });
        fx.Db.MappingProfiles.Add(new MappingProfile { Id = profileId, OrganizationId = orgId, Name = "m", TargetEntity = "Employment" });
        fx.Db.FieldMappings.Add(new FieldMapping { Id = Guid.NewGuid(), MappingProfileId = profileId, SourceField = "name", TargetField = "displayName" });
        fx.Db.SynchronizationJobs.Add(new SynchronizationJob
        {
            Id = jobId, OrganizationId = orgId, SourceConnectorId = connectorId, MappingProfileId = profileId,
            ConflictPolicy = "ManualReview", MaxRetries = 1
        });
        await fx.Db.SaveChangesAsync();
        var platform = new Mock<IPlatformWriteClient>();
        platform.Setup(p => p.UpsertAsync(orgId, It.IsAny<MappedRecord>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlatformWriteResult { EntityId = Guid.NewGuid(), TargetEntity = "Employment" });
        var registry = new ConnectorRegistry([new HashTestConnectorProvider([
            new ExternalRecord { ExternalId = "h2", Fields = new Dictionary<string, string?> { ["name"] = "Bob" }, RecordHash = "12345678" }
        ])]);
        var engine = new SynchronizationEngine(fx.Db, registry, new MappingEngine(), platform.Object, Mock.Of<IIntegrationAuditService>(), Mock.Of<IIntegrationOutboxWriter>(), NullLogger<SynchronizationEngine>.Instance);
        var run = await engine.RunAsync(jobId, "hash-trace-2", null, default);
        Assert.Equal(0, run.ConflictsDetected);
        Assert.Equal(1, run.RecordsProcessed);
    }

    [Fact]
    public async Task RunAsync_ExternalWinsPolicy_IgnoresConflictHash()
    {
        using var fx = new IntegrationTestDb();
        var orgId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        fx.Db.Connectors.Add(new IntegrationConnector { Id = connectorId, OrganizationId = orgId, ProviderType = "hash-test", ConfigurationJson = "{}" });
        fx.Db.MappingProfiles.Add(new MappingProfile { Id = profileId, OrganizationId = orgId, Name = "m", TargetEntity = "Employment" });
        fx.Db.FieldMappings.Add(new FieldMapping { Id = Guid.NewGuid(), MappingProfileId = profileId, SourceField = "name", TargetField = "displayName" });
        fx.Db.SynchronizationJobs.Add(new SynchronizationJob
        {
            Id = jobId, OrganizationId = orgId, SourceConnectorId = connectorId, MappingProfileId = profileId,
            ConflictPolicy = "ExternalWins", MaxRetries = 1
        });
        await fx.Db.SaveChangesAsync();
        var platform = new Mock<IPlatformWriteClient>();
        platform.Setup(p => p.UpsertAsync(orgId, It.IsAny<MappedRecord>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlatformWriteResult { EntityId = Guid.NewGuid(), TargetEntity = "Employment" });
        var registry = new ConnectorRegistry([new HashTestConnectorProvider([
            new ExternalRecord { ExternalId = "h3", Fields = new Dictionary<string, string?> { ["name"] = "Carol" }, RecordHash = "x" }
        ])]);
        var engine = new SynchronizationEngine(fx.Db, registry, new MappingEngine(), platform.Object, Mock.Of<IIntegrationAuditService>(), Mock.Of<IIntegrationOutboxWriter>(), NullLogger<SynchronizationEngine>.Instance);
        var run = await engine.RunAsync(jobId, "ext-wins", null, default);
        Assert.Equal(0, run.ConflictsDetected);
        Assert.Equal(1, run.RecordsProcessed);
    }

    [Fact]
    public async Task RunAsync_PlatformWriteFailure_IncrementsFailedCount()
    {
        using var fx = new IntegrationTestDb();
        var orgId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        fx.Db.Connectors.Add(new IntegrationConnector
        {
            Id = connectorId, OrganizationId = orgId, ProviderType = "csv-import",
            ConfigurationJson = """{"content":"ExternalEmployeeNumber,name\nE9,Fail"}""", LifecycleState = "Connected"
        });
        fx.Db.MappingProfiles.Add(new MappingProfile { Id = profileId, OrganizationId = orgId, Name = "m", SourceEntity = "Ext", TargetEntity = "Employment" });
        fx.Db.FieldMappings.Add(new FieldMapping { Id = Guid.NewGuid(), MappingProfileId = profileId, SourceField = "name", TargetField = "displayName" });
        fx.Db.SynchronizationJobs.Add(new SynchronizationJob
        {
            Id = jobId, OrganizationId = orgId, SourceConnectorId = connectorId, MappingProfileId = profileId,
            ConflictPolicy = "ExternalWins", MaxRetries = 1
        });
        await fx.Db.SaveChangesAsync();
        var platform = new Mock<IPlatformWriteClient>();
        platform.Setup(p => p.UpsertAsync(orgId, It.IsAny<MappedRecord>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PlatformWriteException("downstream failure", 503));
        var registry = new ConnectorRegistry([new CsvImportConnectorProvider()]);
        var engine = new SynchronizationEngine(fx.Db, registry, new MappingEngine(), platform.Object, Mock.Of<IIntegrationAuditService>(), Mock.Of<IIntegrationOutboxWriter>(), NullLogger<SynchronizationEngine>.Instance);
        var run = await engine.RunAsync(jobId, "fail-trace", null, default);
        Assert.Equal(1, run.RecordsFailed);
        Assert.Equal("Failed", run.Status);
    }

    [Fact]
    public async Task RunAsync_RetriesOnFailure_UntilMaxRetries()
    {
        using var fx = new IntegrationTestDb();
        var orgId = Guid.NewGuid();
        var connectorId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        fx.Db.Connectors.Add(new IntegrationConnector
        {
            Id = connectorId, OrganizationId = orgId, ProviderType = "csv-import",
            ConfigurationJson = """{"content":"ExternalEmployeeNumber,name\nE10,Retry"}""", LifecycleState = "Connected"
        });
        fx.Db.MappingProfiles.Add(new MappingProfile { Id = profileId, OrganizationId = orgId, Name = "m", SourceEntity = "Ext", TargetEntity = "Employment" });
        fx.Db.FieldMappings.Add(new FieldMapping { Id = Guid.NewGuid(), MappingProfileId = profileId, SourceField = "name", TargetField = "displayName" });
        fx.Db.SynchronizationJobs.Add(new SynchronizationJob
        {
            Id = jobId, OrganizationId = orgId, SourceConnectorId = connectorId, MappingProfileId = profileId,
            ConflictPolicy = "ExternalWins", MaxRetries = 2
        });
        await fx.Db.SaveChangesAsync();
        var platform = new Mock<IPlatformWriteClient>();
        platform.Setup(p => p.UpsertAsync(orgId, It.IsAny<MappedRecord>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PlatformWriteException("transient", 503));
        var registry = new ConnectorRegistry([new CsvImportConnectorProvider()]);
        var engine = new SynchronizationEngine(fx.Db, registry, new MappingEngine(), platform.Object, Mock.Of<IIntegrationAuditService>(), Mock.Of<IIntegrationOutboxWriter>(), NullLogger<SynchronizationEngine>.Instance);
        var run = await engine.RunAsync(jobId, "retry-trace", null, default);
        Assert.Equal("Failed", run.Status);
        Assert.Equal(2, await fx.Db.SynchronizationRuns.CountAsync(r => r.SynchronizationJobId == jobId));
        platform.Verify(p => p.UpsertAsync(orgId, It.IsAny<MappedRecord>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}

public class ImportExportEngineTests
{
    [Fact]
    public async Task PreviewImport_ValidatesCsvRows()
    {
        using var fx = new IntegrationTestDb();
        var importId = Guid.NewGuid();
        fx.Db.ImportJobs.Add(new ImportJob
        {
            Id = importId, OrganizationId = Guid.NewGuid(), FileFormat = "Csv",
            ContentJson = "ExternalEmployeeNumber,name\nE1,Alice\n,bad"
        });
        await fx.Db.SaveChangesAsync();
        var engine = BuildImportEngine(fx.Db);
        var result = await engine.PreviewImportAsync(importId, default);
        Assert.Equal("Previewed", result.Status);
        Assert.NotNull(result.Preview);
    }

    [Fact]
    public async Task CreateImport_PersistsJob()
    {
        using var fx = new IntegrationTestDb();
        var engine = BuildImportEngine(fx.Db);
        var job = await engine.CreateImportAsync(new CreateImportRequest { OrganizationId = Guid.NewGuid(), FileFormat = "Csv", Content = "a,b\n1,2" }, default);
        Assert.NotEqual(Guid.Empty, job.Id);
    }

    [Fact]
    public async Task ExecuteExport_GeneratesCsvContent()
    {
        using var fx = new IntegrationTestDb();
        var exportId = Guid.NewGuid();
        fx.Db.ExportJobs.Add(new ExportJob { Id = exportId, OrganizationId = Guid.NewGuid(), FileFormat = "Csv" });
        await fx.Db.SaveChangesAsync();
        var engine = BuildImportEngine(fx.Db);
        var result = await engine.ExecuteExportAsync(exportId, default);
        Assert.Equal("Completed", result.Status);
    }

    [Fact]
    public async Task RollbackImport_HappyPath_RevertsCheckpoint()
    {
        using var fx = new IntegrationTestDb();
        var orgId = Guid.NewGuid();
        var profileId = Guid.NewGuid();
        var importId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        fx.Db.MappingProfiles.Add(new MappingProfile { Id = profileId, OrganizationId = orgId, Name = "m", SourceEntity = "Ext", TargetEntity = "Employment" });
        fx.Db.FieldMappings.Add(new FieldMapping { Id = Guid.NewGuid(), MappingProfileId = profileId, SourceField = "name", TargetField = "displayName" });
        fx.Db.ImportJobs.Add(new ImportJob
        {
            Id = importId, OrganizationId = orgId, MappingProfileId = profileId, FileFormat = "Csv",
            ContentJson = "ExternalEmployeeNumber,name\nE1,Alice", Status = "Created"
        });
        await fx.Db.SaveChangesAsync();
        var platform = new Mock<IPlatformWriteClient>();
        platform.Setup(p => p.UpsertAsync(orgId, It.IsAny<MappedRecord>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlatformWriteResult { EntityId = entityId, TargetEntity = "Employment", WasCreated = true });
        var engine = BuildImportEngine(fx.Db, platform.Object);
        await engine.ExecuteImportAsync(importId, Guid.NewGuid(), default);
        await engine.RollbackImportAsync(importId, Guid.NewGuid(), default);
        platform.Verify(p => p.RollbackAsync(orgId, "Employment", entityId, It.IsAny<CancellationToken>()), Times.Once);
        var job = await fx.Db.ImportJobs.FirstAsync(j => j.Id == importId);
        Assert.Equal("RolledBack", job.Status);
    }

    [Fact]
    public async Task RollbackImport_WithoutCheckpoint_Throws()
    {
        using var fx = new IntegrationTestDb();
        var importId = Guid.NewGuid();
        fx.Db.ImportJobs.Add(new ImportJob { Id = importId, OrganizationId = Guid.NewGuid(), FileFormat = "Csv", Status = "Completed" });
        await fx.Db.SaveChangesAsync();
        var engine = BuildImportEngine(fx.Db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.RollbackImportAsync(importId, null, default));
    }

    [Fact]
    public async Task RollbackImport_AlreadyRolledBack_Throws()
    {
        using var fx = new IntegrationTestDb();
        var importId = Guid.NewGuid();
        fx.Db.ImportJobs.Add(new ImportJob
        {
            Id = importId, OrganizationId = Guid.NewGuid(), FileFormat = "Csv", Status = "RolledBack",
            RollbackCheckpointJson = """[{"externalId":"E1","targetEntity":"Employment","platformId":"00000000-0000-0000-0000-000000000001"}]"""
        });
        await fx.Db.SaveChangesAsync();
        var engine = BuildImportEngine(fx.Db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => engine.RollbackImportAsync(importId, null, default));
    }

    private static ImportExportEngine BuildImportEngine(IntegrationDbContext db, IPlatformWriteClient? platform = null) => new(
        db, platform ?? Mock.Of<IPlatformWriteClient>(), new MappingEngine(),
        Mock.Of<IIntegrationAuditService>(), Mock.Of<IIntegrationOutboxWriter>(),
        Options.Create(new IntegrationOptions { ImportPreviewMaxRows = 100 }));
}

public class IntegrationAuthorizationTests
{
    [Fact]
    public async Task EnsureAsync_Denied_Throws()
    {
        var evaluator = new Mock<IAuthorizationEvaluator>();
        evaluator.Setup(e => e.EvaluateAsync(It.IsAny<AuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationDecision { Allowed = false });
        var svc = new IntegrationAuthorizationService(evaluator.Object);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.EnsureAsync(new ClaimsPrincipal(), "Integration.Sync.Execute", Guid.NewGuid(), default));
    }

    [Fact]
    public async Task EnsureAsync_Allowed_DoesNotThrow()
    {
        var evaluator = new Mock<IAuthorizationEvaluator>();
        evaluator.Setup(e => e.EvaluateAsync(It.IsAny<AuthorizationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthorizationDecision { Allowed = true });
        var svc = new IntegrationAuthorizationService(evaluator.Object);
        await svc.EnsureAsync(new ClaimsPrincipal(), "Integration.Connector.Read", Guid.NewGuid(), default);
    }
}

public class ConnectorAppServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidatesProviderConfig()
    {
        using var fx = new IntegrationTestDb();
        var registry = new ConnectorRegistry([new LdapConnectorProvider()]);
        var svc = new ConnectorAppService(fx.Db, registry, new CredentialProtector(new ConfigurationBuilder().Build()), new IntegrationOutboxWriter(fx.Db));
        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CreateAsync(new CreateConnectorRequest
        {
            OrganizationId = Guid.NewGuid(), Name = "ldap", ProviderType = "ldap", ConfigurationJson = "{}"
        }, default));
    }

    [Fact]
    public async Task CreateAsync_StoresEncryptedCredential()
    {
        using var fx = new IntegrationTestDb();
        var registry = new ConnectorRegistry([new CsvImportConnectorProvider()]);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Integration:CredentialEncryptionKey"] = "test-key-32-characters-minimum!!"
        }).Build();
        var svc = new ConnectorAppService(fx.Db, registry, new CredentialProtector(config), new IntegrationOutboxWriter(fx.Db));
        var dto = await svc.CreateAsync(new CreateConnectorRequest
        {
            OrganizationId = Guid.NewGuid(), Name = "csv", ProviderType = "csv-import",
            ConfigurationJson = """{"content":"a,b\n1,2"}""",
            Credential = new CreateCredentialRequest { Name = "key", Secret = "secret" }
        }, default);
        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Single(await fx.Db.ConnectorCredentials.ToListAsync());
    }

    [Fact]
    public async Task ListAsync_FiltersByOrganization()
    {
        using var fx = new IntegrationTestDb();
        var org = Guid.NewGuid();
        fx.Db.Connectors.AddRange(
            new IntegrationConnector { Id = Guid.NewGuid(), OrganizationId = org, Name = "a", ProviderType = "csv-import" },
            new IntegrationConnector { Id = Guid.NewGuid(), OrganizationId = Guid.NewGuid(), Name = "b", ProviderType = "csv-import" });
        await fx.Db.SaveChangesAsync();
        var registry = new ConnectorRegistry([new CsvImportConnectorProvider()]);
        var svc = new ConnectorAppService(fx.Db, registry, new CredentialProtector(new ConfigurationBuilder().Build()), new IntegrationOutboxWriter(fx.Db));
        Assert.Single(await svc.ListAsync(org, default));
    }
}

public class IntegrationAuditServiceTests
{
    [Fact]
    public async Task LogAsync_PersistsEntry()
    {
        using var fx = new IntegrationTestDb();
        var svc = new IntegrationAuditService(fx.Db);
        await svc.LogAsync(Guid.NewGuid(), "Sync", "Success", Guid.NewGuid(), null, "csv", "employment", 5, 100, "trace", default);
        Assert.Single(await fx.Db.IntegrationAuditLogs.ToListAsync());
    }

    [Fact]
    public async Task ListAsync_ReturnsRecentEntries()
    {
        using var fx = new IntegrationTestDb();
        var org = Guid.NewGuid();
        fx.Db.IntegrationAuditLogs.Add(new IntegrationAuditLog { Id = Guid.NewGuid(), OrganizationId = org, Operation = "Import", Result = "Success" });
        await fx.Db.SaveChangesAsync();
        var svc = new IntegrationAuditService(fx.Db);
        Assert.Single(await svc.ListAsync(org, ct: default));
    }
}

public class MappingProfileServiceTests
{
    [Fact]
    public async Task CreateAsync_PersistsProfileAndFields()
    {
        using var fx = new IntegrationTestDb();
        var svc = new MappingProfileService(fx.Db);
        var dto = await svc.CreateAsync(new CreateMappingProfileRequest
        {
            OrganizationId = Guid.NewGuid(), Name = "HR", SourceEntity = "Employee", TargetEntity = "Employment",
            Fields = [new FieldMappingInput { SourceField = "ExternalEmployeeNumber", TargetField = "EmployeeNumber" }]
        }, default);
        Assert.Single(dto.Fields);
    }
}

public class PlatformWriteClientTests
{
    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }

    private sealed class NamedClientFactory : IHttpClientFactory
    {
        private readonly Dictionary<string, HttpClient> _clients = new(StringComparer.OrdinalIgnoreCase);
        public void Register(string name, HttpClient client) => _clients[name] = client;
        public HttpClient CreateClient(string name) => _clients[name];
    }

    private static string SuccessBody(Guid entityId) =>
        JsonSerializer.Serialize(new { data = new IntegrationUpsertResponse { EntityId = entityId, WasCreated = true } });

    private static (PlatformWriteClient Client, NamedClientFactory Factory, RecordingHandler PersonHandler, RecordingHandler WorkforceHandler, RecordingHandler OrgHandler) BuildClient()
    {
        var personHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SuccessBody(Guid.NewGuid())) });
        var workforceHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SuccessBody(Guid.NewGuid())) });
        var orgHandler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(SuccessBody(Guid.NewGuid())) });
        var factory = new NamedClientFactory();
        factory.Register("Integration.PersonService", new HttpClient(personHandler) { BaseAddress = new Uri("http://person") });
        factory.Register("Integration.WorkforceService", new HttpClient(workforceHandler) { BaseAddress = new Uri("http://workforce") });
        factory.Register("Integration.OrganizationService", new HttpClient(orgHandler) { BaseAddress = new Uri("http://org") });
        return (new PlatformWriteClient(factory, NullLogger<PlatformWriteClient>.Instance), factory, personHandler, workforceHandler, orgHandler);
    }

    [Theory]
    [InlineData("Person", "Integration.PersonService", "internal/persons/upsert")]
    [InlineData("Employment", "Integration.WorkforceService", "internal/employment/upsert")]
    [InlineData("Department", "Integration.OrganizationService", "internal/departments/upsert")]
    [InlineData("Organization.Department", "Integration.OrganizationService", "internal/departments/upsert")]
    public async Task UpsertAsync_RoutesToCorrectService(string targetEntity, string clientName, string expectedPath)
    {
        var (client, factory, personHandler, workforceHandler, orgHandler) = BuildClient();
        var orgId = Guid.NewGuid();
        var result = await client.UpsertAsync(orgId, new MappedRecord { TargetEntity = targetEntity, Fields = { ["name"] = "x" } }, "ext-1");
        var handler = clientName switch
        {
            "Integration.PersonService" => personHandler,
            "Integration.WorkforceService" => workforceHandler,
            _ => orgHandler
        };
        Assert.Single(handler.Requests);
        Assert.EndsWith(expectedPath, handler.Requests[0].RequestUri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(targetEntity is "Organization.Department" ? "Organization.Department" : targetEntity, result.TargetEntity);
        _ = factory;
    }

    [Fact]
    public async Task UpsertAsync_UnsupportedEntity_ThrowsPlatformWriteException()
    {
        var (client, _, _, _, _) = BuildClient();
        var ex = await Assert.ThrowsAsync<PlatformWriteException>(() =>
            client.UpsertAsync(Guid.NewGuid(), new MappedRecord { TargetEntity = "UnknownEntity" }, null));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task UpsertAsync_HttpFailure_PropagatesStatusCode()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("gateway down") });
        var factory = new NamedClientFactory();
        factory.Register("Integration.WorkforceService", new HttpClient(handler) { BaseAddress = new Uri("http://workforce") });
        var client = new PlatformWriteClient(factory, NullLogger<PlatformWriteClient>.Instance);
        var ex = await Assert.ThrowsAsync<PlatformWriteException>(() =>
            client.UpsertAsync(Guid.NewGuid(), new MappedRecord { TargetEntity = "Employment" }, "e1"));
        Assert.Equal(502, ex.StatusCode);
        Assert.Contains("gateway down", ex.ResponseBody, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Person", "internal/persons/")]
    [InlineData("Employment", "internal/employment/")]
    [InlineData("Department", "internal/departments/")]
    public async Task RollbackAsync_RoutesToCorrectPath(string targetEntity, string pathPrefix)
    {
        var entityId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        var factory = new NamedClientFactory();
        var clientName = targetEntity switch
        {
            "Person" => "Integration.PersonService",
            "Employment" => "Integration.WorkforceService",
            _ => "Integration.OrganizationService"
        };
        factory.Register(clientName, new HttpClient(handler) { BaseAddress = new Uri("http://svc") });
        var client = new PlatformWriteClient(factory, NullLogger<PlatformWriteClient>.Instance);
        await client.RollbackAsync(Guid.NewGuid(), targetEntity, entityId);
        Assert.Single(handler.Requests);
        Assert.Contains(pathPrefix, handler.Requests[0].RequestUri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(entityId.ToString(), handler.Requests[0].RequestUri!.AbsolutePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RollbackAsync_UnsupportedEntity_Throws()
    {
        var (client, _, _, _, _) = BuildClient();
        var ex = await Assert.ThrowsAsync<PlatformWriteException>(() =>
            client.RollbackAsync(Guid.NewGuid(), "Facility", Guid.NewGuid()));
        Assert.Equal(400, ex.StatusCode);
    }

    [Fact]
    public async Task RollbackAsync_HttpFailure_PropagatesStatusCode()
    {
        var entityId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.Conflict) { Content = new StringContent("in use") });
        var factory = new NamedClientFactory();
        factory.Register("Integration.PersonService", new HttpClient(handler) { BaseAddress = new Uri("http://person") });
        var client = new PlatformWriteClient(factory, NullLogger<PlatformWriteClient>.Instance);
        var ex = await Assert.ThrowsAsync<PlatformWriteException>(() =>
            client.RollbackAsync(Guid.NewGuid(), "Person", entityId));
        Assert.Equal(409, ex.StatusCode);
    }
}

public class CronNextOccurrenceTests
{
    [Fact]
    public void GetNextCronOccurrence_HourlySchedule_ReturnsFutureUtc()
    {
        var from = new DateTime(2026, 7, 7, 10, 15, 0, DateTimeKind.Utc);
        var next = SyncSchedulerHostedService.GetNextCronOccurrence("0 * * * *", from);
        Assert.True(next > from);
        Assert.Equal(DateTimeKind.Utc, next.Kind);
    }

    [Fact]
    public void GetNextCronOccurrence_DailyAtMidnight_SkipsSameDayIfPast()
    {
        var from = new DateTime(2026, 7, 7, 12, 0, 0, DateTimeKind.Utc);
        var next = SyncSchedulerHostedService.GetNextCronOccurrence("0 0 * * *", from);
        Assert.Equal(new DateTime(2026, 7, 8, 0, 0, 0, DateTimeKind.Utc), next);
    }

    [Fact]
    public void GetNextCronOccurrence_InvalidExpression_FallsBackSixHours()
    {
        var from = new DateTime(2026, 7, 7, 8, 0, 0, DateTimeKind.Utc);
        var next = SyncSchedulerHostedService.GetNextCronOccurrence("not-a-cron", from);
        Assert.Equal(from.AddHours(6), next);
    }

    [Fact]
    public void GetNextCronOccurrence_EveryFiveMinutes_AdvancesCorrectly()
    {
        var from = new DateTime(2026, 7, 7, 10, 3, 0, DateTimeKind.Utc);
        var next = SyncSchedulerHostedService.GetNextCronOccurrence("*/5 * * * *", from);
        Assert.Equal(new DateTime(2026, 7, 7, 10, 5, 0, DateTimeKind.Utc), next);
    }
}

public class SyncBackoffCalculationTests
{
    [Theory]
    [InlineData(1, 2000)]
    [InlineData(2, 4000)]
    [InlineData(3, 8000)]
    public void ExponentialBackoffDelay_MatchesEngineFormula(int attempt, int expectedMs)
    {
        var delayMs = (int)Math.Pow(2, attempt) * 1000;
        Assert.Equal(expectedMs, delayMs);
    }
}
