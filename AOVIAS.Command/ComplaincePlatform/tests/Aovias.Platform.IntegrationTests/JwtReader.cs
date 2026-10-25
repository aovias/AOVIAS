using System.Text.Json;

namespace Aovias.Platform.IntegrationTests;

/// <summary>
/// Minimal JWT payload reader.
///
/// <para>
/// Deliberately does **not** validate signatures: these tests assert what the platform *asserts*
/// about a caller, and the platform is the thing under test. Validating here would only prove the
/// test agrees with itself.
/// </para>
/// </summary>
public static class JwtReader
{
    public static IReadOnlyDictionary<string, JsonElement> ReadClaims(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2) return new Dictionary<string, JsonElement>();

        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
        return doc.RootElement.EnumerateObject()
            .ToDictionary(p => p.Name, p => p.Value.Clone());
    }

    public static Guid GuidClaim(IReadOnlyDictionary<string, JsonElement> claims, string name) =>
        claims.TryGetValue(name, out var value)
        && Guid.TryParse(value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString(), out var id)
            ? id
            : Guid.Empty;

    public static string StringClaim(IReadOnlyDictionary<string, JsonElement> claims, string name) =>
        claims.TryGetValue(name, out var value) ? value.ToString() : string.Empty;
}
