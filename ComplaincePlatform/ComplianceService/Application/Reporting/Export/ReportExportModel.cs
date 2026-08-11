namespace ComplianceService.Application.Reporting.Export;

public sealed class ReportExportModel
{
    public string Title { get; init; } = string.Empty;
    public string OrganizationName { get; init; } = string.Empty;
    public string FrameworkName { get; init; } = string.Empty;
    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
    public List<ReportKpiItem> Kpis { get; init; } = new();
    public List<ReportTableSection> Tables { get; init; } = new();
}

public sealed class ReportKpiItem
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

public sealed class ReportTableSection
{
    public string Title { get; init; } = string.Empty;
    public List<string> Headers { get; init; } = new();
    public List<List<string>> Rows { get; init; } = new();
}

public enum ReportExportFormat
{
    Csv,
    Excel,
    Pdf
}
