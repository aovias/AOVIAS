namespace ComplianceServiceManager.Models.SourceContext;

/// <summary>Identifies whether a source context is the shared integration tree or an agent workspace.</summary>
public enum SourceContextKind
{
    Shared = 0,
    Workspace = 1
}
