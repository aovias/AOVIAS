namespace Aovias.Configuration.Settings;

/// <summary>
/// Sprint M4.3 — configuration for a Runtime Agent's automatic self-registration with the
/// Operations Command Center's Fleet Registry (<c>EnvironmentRegistrationHostedService</c>). This
/// is a new settings surface (no existing section covers "which Command Center do I register
/// with"), consistent with every other cross-service address already being its own settings class
/// (compare <see cref="ApiSettings"/>, <see cref="DeploymentHostSettings"/>).
///
/// <see cref="CommandCenterBaseUrl"/> and <see cref="RegistrationToken"/> are required only when
/// <see cref="Enabled"/> is <see langword="true"/> (enforced by a conditional validator at
/// registration time, not <c>[Required]</c>) — most existing profiles (single-environment,
/// local-docker development) leave this feature off entirely and must not be forced to supply a
/// Command Center address they do not have.
/// </summary>
public sealed class EnvironmentRegistrationSettings
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "EnvironmentRegistration";

    /// <summary>When <see langword="false"/> (the default), the hosted service does not run.</summary>
    public bool Enabled { get; set; }

    public string CommandCenterBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// The pre-shared registration token this agent presents to its own
    /// <c>EnvironmentRegistrationStore</c> (the same store/token vocabulary
    /// <c>POST /api/deployment/register</c> already validates against — no second token concept).
    /// </summary>
    public string RegistrationToken { get; set; } = string.Empty;

    /// <summary>Seconds between registration attempts/health re-verifications. Applies both while not-yet-registered (retry) and after successful registration (ongoing health verification against Command Center).</summary>
    public int RetryIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// The fleet environment classification (<c>LocalDocker</c>/<c>Demo</c>/<c>Qa</c>/<c>Uat</c>/
    /// <c>Production</c>/<c>Customer</c>) reported at registration. Held as a plain string here
    /// rather than the <c>Aovias.Release.Contracts.RemoteDeployment.EnvironmentType</c> enum so
    /// this foundational configuration project does not take on a new project dependency for one
    /// field — <c>EnvironmentRegistrationHostedService</c> (which already references that enum for
    /// the registration calls themselves) parses it.
    /// </summary>
    public string EnvironmentType { get; set; } = "LocalDocker";

    /// <summary>
    /// Sprint M4.4 — the <c>Aovias.Deployment.Contracts.Capabilities.EnvironmentCapabilityProfile</c>
    /// id (Sprint M2.3 D6 catalog: <c>docker-host</c>, <c>demo-vps</c>, <c>production-vps</c>,
    /// <c>kubernetes-cluster</c>, <c>air-gapped</c>) this environment declares itself to be for
    /// admission purposes. Deliberately separate from <see cref="EnvironmentType"/> (the two
    /// catalogs are not 1:1 today — see the M4.4 report) and deliberately not validated against the
    /// embedded profile catalog here (this project has no dependency on
    /// <c>Aovias.Deployment.Contracts</c>); an unknown id surfaces as a startup log warning from
    /// <c>EnvironmentAdmissionHostedService</c>, not a hard failure, so a typo never blocks the
    /// Runtime Agent from starting. Defaults to <c>docker-host</c>, the existing catalog's baseline
    /// local-development profile, so no existing deployment profile needs a new setting to keep
    /// working.
    /// </summary>
    public string CapabilityProfileId { get; set; } = "docker-host";
}
