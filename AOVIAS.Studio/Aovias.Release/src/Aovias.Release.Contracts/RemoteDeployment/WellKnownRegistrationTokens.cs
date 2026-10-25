namespace Aovias.Release.Contracts.RemoteDeployment;

/// <summary>
/// The pre-shared registration tokens the platform recognizes, as one shared source of truth. The
/// Runtime Agent's own <c>EnvironmentRegistrationStore</c> seeds its accepted-token set from this
/// list, and AOVIAS Studio's Bootstrap flow pre-fills its Registration Token field from
/// <see cref="ForEnvironmentType"/> — before this class existed, the two sides each had their own
/// idea of the tokens (the agent's hardcoded seeds vs. a free-text field), and a mismatch
/// surfaced only at Stage 8 as an HTTP 500 on a live VPS. Zero-touch bootstrap does not mint new
/// tokens; it can only use one the platform already recognizes.
/// </summary>
public static class WellKnownRegistrationTokens
{
    public const string LocalDocker = "local-docker-registration-token";

    public const string DemoVps = "demo-vps-registration-token";

    public const string Qa = "qa-registration-token";

    public const string Uat = "uat-registration-token";

    public const string Production = "production-registration-token";

    public const string CustomerA = "customer-a-registration-token";

    public const string CustomerB = "customer-b-registration-token";

    /// <summary>Every recognized token with its environment display name — the exact set <c>EnvironmentRegistrationStore</c> accepts.</summary>
    public static readonly IReadOnlyList<(string Token, string EnvironmentName)> All =
    [
        (LocalDocker, "Local Docker"),
        (DemoVps, "Demo VPS"),
        (Qa, "QA"),
        (Uat, "UAT"),
        (Production, "Production"),
        (CustomerA, "Customer A"),
        (CustomerB, "Customer B")
    ];

    /// <summary>The default token for an environment type. <see cref="EnvironmentType.Customer"/> maps to Customer A — an operator provisioning Customer B overrides the pre-filled value by hand.</summary>
    public static string ForEnvironmentType(EnvironmentType environmentType) => environmentType switch
    {
        EnvironmentType.LocalDocker => LocalDocker,
        EnvironmentType.Demo => DemoVps,
        EnvironmentType.Qa => Qa,
        EnvironmentType.Uat => Uat,
        EnvironmentType.Production => Production,
        EnvironmentType.Customer => CustomerA,
        _ => DemoVps
    };

    public static bool IsWellKnown(string? token)
        => !string.IsNullOrWhiteSpace(token) && All.Any(entry => string.Equals(entry.Token, token, StringComparison.Ordinal));
}
