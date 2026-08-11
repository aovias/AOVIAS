namespace Gateway.Configuration;

/// <summary>CRP Phase 6 — entitlement feature gates (fail-closed on mutating APIs).</summary>
public sealed class LicensingFeatureGateOptions
{
    public const string SectionName = "Licensing";

    /// <summary>When true, mutating requests matching FeatureGates are denied without entitlement.</summary>
    public bool FailClosedWrites { get; set; } = true;

    public int EntitlementCacheSeconds { get; set; } = 30;

    /// <summary>Path substring (lower) → entitlement code. First match wins.</summary>
    public Dictionary<string, string> FeatureGates { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/api/v1/ai/"] = "ai.write",
        ["/api/v1/frameworks"] = "frameworks"
    };

    /// <summary>Path substrings that skip entitlement enforcement.</summary>
    public List<string> AllowlistPathFragments { get; set; } =
    [
        "/commercial-admin",
        "/licenses/",
        "/licensing/",
        "/identity/auth",
        "/auth/",
        "/health",
        "/ops/",
        "password",
        "refresh",
        "/subscriptions"
    ];
}
