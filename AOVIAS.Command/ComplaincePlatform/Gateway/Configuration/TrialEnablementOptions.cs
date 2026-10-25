namespace Gateway.Configuration;

/// <summary>CRP Phase 2 — SaaS trial license enablement on signup.</summary>
public sealed class TrialEnablementOptions
{
    public const string SectionName = "TrialEnablement";

    public bool AutoIssueOnSignup { get; set; } = true;
    public int ValidDays { get; set; } = 30;
    public int MaxSeats { get; set; } = 10;
    public int GracePeriodDays { get; set; } = 7;
    public bool AutoActivate { get; set; } = true;

    /// <summary>SignupHost → Environment.MachineName for SaaS auto-activate.</summary>
    public string MachineFingerprintMode { get; set; } = "SignupHost";

    public List<string> Entitlements { get; set; } =
    [
        "core.compliance", "core.documents", "core.capa", "ai.assist", "governance.risk", "ace.insights"
    ];

    public bool DualWriteOrganizationSubscription { get; set; }

    /// <summary>
    /// CRP Phase 6 — after license issue/activate, sync OrganizationSubscription catalogue from LicenseKey (preferred over DualWrite).
    /// </summary>
    public bool SyncCatalogFromLicense { get; set; } = true;

    public Guid? DefaultTrialPlanId { get; set; }
    public int MaxExtensions { get; set; } = 2;
    public int ExtensionDays { get; set; } = 15;
}
