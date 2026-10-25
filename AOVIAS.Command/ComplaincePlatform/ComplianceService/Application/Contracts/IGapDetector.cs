using ComplianceService.Domain;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Detects compliance gaps using explicit, deterministic rules.
/// No AI, no scoring, no prioritization - pure rule-based analysis.
/// All gaps are deterministic and explainable.
/// </summary>
public interface IGapDetector
{
    /// <summary>
    /// Detects compliance gaps in the provided context.
    /// Uses deterministic rules only - no AI, no scoring, no prioritization.
    /// </summary>
    /// <param name="context">The compliance context to analyze.</param>
    /// <returns>List of detected gaps. Each gap is deterministic and explainable.</returns>
    List<Gap> Detect(ComplianceContext context);
}
