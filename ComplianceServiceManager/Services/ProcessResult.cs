namespace ComplianceServiceManager.Services;

public sealed class ProcessResult
{
    public int ExitCode { get; init; }

    public bool Succeeded => ExitCode == 0;
}
