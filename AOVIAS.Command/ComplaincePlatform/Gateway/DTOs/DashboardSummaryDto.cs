namespace Gateway.DTOs;

/// <summary>
/// DTO representing dashboard summary data.
/// </summary>
public class DashboardSummaryDto
{
    public string HospitalName { get; set; } = string.Empty;
    public decimal ReadinessPercentage { get; set; }
    public int TotalTasks { get; set; }
    public int CriticalPending { get; set; }
    public int UpcomingAudits { get; set; }
}
