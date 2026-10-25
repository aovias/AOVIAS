namespace IdentityService.Application.Configuration;

public sealed class IdentityRuntimeOptions
{
    public const string SectionName = "Identity";

    public bool EnableBackgroundJobs { get; set; } = true;

    /// <summary>
    /// When true, skips adaptive-auth blocks and MFA policy gates that would otherwise return 403 during local development.
    /// </summary>
    public bool RelaxLoginSecurityGates { get; set; }
}
