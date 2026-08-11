namespace ComplianceService.Application.Reporting.Dtos;

public sealed record ReportFilter
{
    public Guid HospitalId { get; init; }
    public Guid? FrameworkId { get; init; }
    public Guid? ChapterId { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? Severity { get; init; }
    public string? TaskStatus { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public Guid? OrganizationId { get; init; }
}
