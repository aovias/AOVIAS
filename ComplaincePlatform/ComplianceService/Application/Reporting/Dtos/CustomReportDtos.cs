namespace ComplianceService.Application.Reporting.Dtos;

public sealed class CustomReportRequest
{
    public List<string> Dimensions { get; init; } = new();
    public Guid? FrameworkId { get; init; }
    public Guid? ChapterId { get; init; }
    public Guid? DepartmentId { get; init; }
    public string? Severity { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
}

public sealed class CustomReportResultDto
{
    public List<ColumnMetadataDto> Columns { get; init; } = new();
    public List<Dictionary<string, object?>> Rows { get; init; } = new();
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}

public sealed class ColumnMetadataDto
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Type { get; init; } = "string";
}
