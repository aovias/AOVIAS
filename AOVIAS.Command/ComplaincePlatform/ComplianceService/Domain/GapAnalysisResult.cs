namespace ComplianceService.Domain;

/// <summary>
/// Complete result of gap analysis containing all detected gaps.
/// </summary>
public class GapAnalysisResult
{
    /// <summary>
    /// List of all detected gaps.
    /// </summary>
    public List<GapResult> Gaps { get; set; } = new();

    /// <summary>
    /// Summary counts by gap type.
    /// </summary>
    public Dictionary<GapType, int> GapCounts { get; set; } = new();

    /// <summary>
    /// Summary counts by severity.
    /// </summary>
    public Dictionary<GapSeverity, int> SeverityCounts { get; set; } = new();

    /// <summary>
    /// Total number of gaps detected.
    /// </summary>
    public int TotalGaps => Gaps.Count;

    /// <summary>
    /// Indicates if any critical gaps were found.
    /// </summary>
    public bool HasCriticalGaps => Gaps.Any(g => g.Severity == GapSeverity.Critical);
}
