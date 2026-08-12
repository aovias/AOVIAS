namespace ComplianceServiceManager.Services.Workspaces;

/// <summary>Result of a constrained <c>git</c> CLI invocation.</summary>
public sealed class GitCliResult
{
    public required int ExitCode { get; init; }

    public required string StandardOutput { get; init; }

    public required string StandardError { get; init; }

    public bool Succeeded => ExitCode == 0;
}
