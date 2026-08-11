namespace ComplianceService.Application.Dashboard.Projection;

public sealed class DashboardProjectionOptions
{
    public const string SectionName = "DashboardProjection";

    public bool Enabled { get; set; } = true;

    public bool ExposeDiagnosticsHeaders { get; set; }

    /// <summary>When projection is older than this, trigger background rebuild on read (still serves stale).</summary>
    public int StaleAfterSeconds { get; set; } = 300;
}
