namespace ComplianceService.Application.Analytics;

public sealed class DeficiencyMetricCountsDto
{
    public int Total { get; init; }
    public int Open { get; init; }
    public int Closed { get; init; }
    public int Cancelled { get; init; }
    public int Verified { get; init; }
    public int CriticalOpen { get; init; }
    public int BlockingOpen { get; init; }
    public int RecurringOpen { get; init; }
    public int EscalatedOpen { get; init; }
    public int CriticalEscalatedOpen { get; init; }
    public int AuditOriginOpen { get; init; }
}
