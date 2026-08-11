namespace Platform.Dashboard.Models;

/// <summary>
/// Progressive loading phases for dashboard widgets and sections.
/// Phase assignment is metadata only in H1.1; loading orchestration is a future sprint.
/// </summary>
public enum DashboardLoadingPhase
{
    /// <summary>Application shell — navigation, layout, context chrome.</summary>
    Shell = 0,

    /// <summary>Lightweight platform summary — KPIs and critical signals.</summary>
    Summary = 1,

    /// <summary>Critical operational widgets required for first meaningful paint.</summary>
    CriticalWidgets = 2,

    /// <summary>Operational widgets — tasks, alerts, heatmaps.</summary>
    OperationalWidgets = 3,

    /// <summary>Analytics — trends, charts, breakdowns.</summary>
    Analytics = 4,

    /// <summary>AI insights and copilot surfaces.</summary>
    Ai = 5,
}
