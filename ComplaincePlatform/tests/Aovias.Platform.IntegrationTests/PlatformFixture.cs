using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Aovias.Platform.IntegrationTests;

/// <summary>A signed-in tenant, with the identifiers its token actually asserts.</summary>
public sealed record TenantSession(
    string Email,
    string AccessToken,
    Guid OrganizationId,
    Guid HospitalId)
{
    public HttpClient CreateClient(string baseUrl)
    {
        var client = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromMinutes(3) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AccessToken);
        return client;
    }
}

/// <summary>
/// Shared platform state for the integration suite.
///
/// <para>
/// **These tests run against a live platform on purpose.** Tenant isolation is an emergent property
/// of the Gateway, the token, the query filters and the database working together — a mocked
/// harness would assert only that the mock behaves, which is precisely the class of false assurance
/// that let the platform ship with filters that silently disabled themselves. See ADR-002.
/// </para>
///
/// <para>
/// The fixture is read-only against tenant data: it authenticates and reads. Nothing here mutates
/// business records, so the suite is safe to run repeatedly and is deterministic.
/// </para>
/// </summary>
public sealed class PlatformFixture : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    public string GatewayBaseUrl { get; private set; } = "http://localhost:5212";

    /// <summary>The primary tenant. Null when the platform is unreachable or login failed.</summary>
    public TenantSession? TenantA { get; private set; }

    /// <summary>A second, different tenant. Null unless credentials are configured.</summary>
    public TenantSession? TenantB { get; private set; }

    /// <summary>An organization no configured user belongs to.</summary>
    public Guid ForeignOrganizationId { get; private set; } = Guid.Parse("11111111-2222-3333-4444-555555555555");

    /// <summary>Why the platform is unusable, when it is — surfaced so tests fail loudly, not silently.</summary>
    public string? UnavailableReason { get; private set; }

    public bool PlatformAvailable => TenantA is not null;
    public bool TwoTenantsAvailable => TenantA is not null && TenantB is not null;

    public async Task InitializeAsync()
    {
        var settings = LoadSettings();
        GatewayBaseUrl = settings.GatewayBaseUrl;
        ForeignOrganizationId = settings.ForeignOrganizationId;

        using var probe = new HttpClient { BaseAddress = new Uri(GatewayBaseUrl), Timeout = TimeSpan.FromSeconds(30) };

        TenantA = await SignInAsync(probe, settings.TenantA.Email, settings.TenantA.Password);
        if (TenantA is null)
        {
            UnavailableReason =
                $"Could not authenticate '{settings.TenantA.Email}' against {GatewayBaseUrl}. " +
                "Is the platform running?";
            return;
        }

        if (!string.IsNullOrWhiteSpace(settings.TenantB.Email))
        {
            TenantB = await SignInAsync(probe, settings.TenantB.Email, settings.TenantB.Password);
        }

        // A "second tenant" in the same organization would make isolation tests vacuously pass.
        if (TenantB is not null && TenantB.OrganizationId == TenantA.OrganizationId)
        {
            throw new InvalidOperationException(
                "tenantB resolves to the same organization as tenantA. Cross-tenant tests would " +
                "pass without proving anything — configure a user in a different organization.");
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private static async Task<TenantSession?> SignInAsync(HttpClient client, string email, string password)
    {
        try
        {
            var response = await client.PostAsJsonAsync(
                "/api/v1/identity/login",
                new { email, password, deviceId = "integration-tests" });

            if (!response.IsSuccessStatusCode) return null;

            var payload = await response.Content.ReadFromJsonAsync<LoginEnvelope>(Json);
            var token = payload?.Data?.Token;
            if (string.IsNullOrWhiteSpace(token)) return null;

            var claims = JwtReader.ReadClaims(token);
            return new TenantSession(
                email,
                token,
                JwtReader.GuidClaim(claims, "organization_id"),
                JwtReader.GuidClaim(claims, "hospital_id"));
        }
        catch (HttpRequestException) { return null; }
        catch (TaskCanceledException) { return null; }
    }

    private static TestSettings LoadSettings()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "testsettings.json");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "testsettings.json was not copied to the output directory.", path);
        }

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;

        return new TestSettings(
            root.GetProperty("gatewayBaseUrl").GetString() ?? "http://localhost:5212",
            ReadCredentials(root, "tenantA"),
            ReadCredentials(root, "tenantB"),
            Guid.TryParse(root.GetProperty("foreignOrganizationId").GetString(), out var foreign)
                ? foreign
                : Guid.Parse("11111111-2222-3333-4444-555555555555"));
    }

    private static Credentials ReadCredentials(JsonElement root, string name) =>
        root.TryGetProperty(name, out var node)
            ? new Credentials(
                node.TryGetProperty("email", out var e) ? e.GetString() ?? "" : "",
                node.TryGetProperty("password", out var p) ? p.GetString() ?? "" : "")
            : new Credentials("", "");

    private sealed record TestSettings(
        string GatewayBaseUrl, Credentials TenantA, Credentials TenantB, Guid ForeignOrganizationId);

    private sealed record Credentials(string Email, string Password);

    private sealed record LoginEnvelope(LoginData? Data);
    private sealed record LoginData(string? Token);
}

[CollectionDefinition(nameof(PlatformCollection))]
public sealed class PlatformCollection : ICollectionFixture<PlatformFixture>;
