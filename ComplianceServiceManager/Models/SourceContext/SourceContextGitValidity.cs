namespace ComplianceServiceManager.Models.SourceContext;

/// <summary>Observed Git accessibility/validity for a source context (read-only inspection).</summary>
public enum SourceContextGitValidity
{
    Accessible = 0,
    Unavailable = 1,
    Detached = 2,
    InvalidBranch = 3
}
