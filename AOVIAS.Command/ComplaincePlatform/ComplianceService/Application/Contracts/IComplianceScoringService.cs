using ComplianceService.Application.ComplianceScoring;

namespace ComplianceService.Application.Contracts;

/// <summary>
/// Service interface for calculating compliance scores for hospitals.
/// Provides deterministic, weighted scoring based on task completion, objective-element completion,
/// chapter scores, and risk penalties.
/// </summary>
public interface IComplianceScoringService
{
    /// <summary>
    /// Calculates the overall compliance score for a hospital.
    /// </summary>
    /// <param name="hospitalId">The unique identifier of the hospital.</param>
    /// <returns>A <see cref="ComplianceScoreResult"/> containing the overall score, breakdown by chapter, and risk metrics.</returns>
    Task<ComplianceScoreResult> CalculateHospitalScoreAsync(Guid hospitalId);

    /// <summary>
    /// Calculates the compliance score for a hospital and persists it as a snapshot.
    /// Uses upsert logic: updates existing snapshot or creates a new one.
    /// </summary>
    /// <param name="hospitalId">The unique identifier of the hospital.</param>
    /// <returns>A <see cref="ComplianceScoreResult"/> containing the overall score, breakdown by chapter, and risk metrics.</returns>
    Task<ComplianceScoreResult> CalculateAndPersistHospitalScoreAsync(Guid hospitalId);

    /// <summary>
    /// Calculates the compliance score for a specific framework within a hospital.
    /// </summary>
    /// <param name="hospitalId">The unique identifier of the hospital.</param>
    /// <param name="frameworkId">The unique identifier of the compliance framework.</param>
    /// <returns>A <see cref="ComplianceScoreResult"/> containing the framework-specific score, breakdown by chapter, and risk metrics.</returns>
    Task<ComplianceScoreResult> CalculateFrameworkScoreAsync(Guid hospitalId, Guid frameworkId);
}
