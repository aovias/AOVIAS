using ComplianceService.Application.Contracts;
using ComplianceService.Domain;

namespace ComplianceService.Application.Services;

/// <summary>
/// Detects gaps related to evidence.
/// Uses explicit, deterministic rules only - no AI, no inference.
/// </summary>
public class EvidenceGapDetector : IGapDetector
{
    private readonly ILogger<EvidenceGapDetector> _logger;

    public EvidenceGapDetector(ILogger<EvidenceGapDetector> logger)
    {
        _logger = logger;
    }

    public List<Gap> Detect(ComplianceContext context)
    {
        var gaps = new List<Gap>();

        if (context == null)
        {
            _logger.LogWarning("ComplianceContext is null, returning empty gap list");
            return gaps;
        }

        // Rule 1: Detect evidence not linked to any task
        DetectUnlinkedEvidence(context, gaps);

        // Rule 2: Detect evidence marked weak or incomplete
        DetectWeakOrIncompleteEvidence(context, gaps);

        return gaps;
    }

    /// <summary>
    /// Rule: Evidence is not linked to any task.
    /// Detection Logic:
    /// - For each evidence item, check if TaskId exists and matches a task in ProgramTasks
    /// - Evidence is unlinked if:
    ///   1. TaskId is empty/Guid.Empty, OR
    ///   2. TaskId does not match any task in ProgramTasks
    /// - This is a deterministic check: TaskId lookup only
    /// </summary>
    private void DetectUnlinkedEvidence(ComplianceContext context, List<Gap> gaps)
    {
        // Rule Check: Only process if we have evidence
        if (context.Evidence.Count == 0)
        {
            return; // No evidence to check
        }

        // Rule Check: If no tasks exist, all evidence is unlinked
        if (context.ProgramTasks.Count == 0)
        {
            foreach (var evidence in context.Evidence)
            {
                gaps.Add(new Gap
                {
                    GapType = GapType.UnmappedEvidence,
                    Description = $"Evidence {evidence.Id} is not linked to any task (no tasks exist)",
                    AffectedEntityType = TargetEntityType.Task, // Evidence is affected, but relates to task
                    AffectedEntityId = evidence.TaskId, // May be empty, but that's the point
                    RulesReferenced = new List<string> { "EvidenceGapDetector.Rule1: Evidence must be linked to a task" }
                });
            }
            return;
        }

        // Rule Check: For each evidence, verify TaskId matches a task
        foreach (var evidence in context.Evidence)
        {
            // Deterministic check: TaskId must not be empty
            if (evidence.TaskId == Guid.Empty)
            {
                gaps.Add(new Gap
                {
                    GapType = GapType.UnmappedEvidence,
                    Description = $"Evidence {evidence.Id} has no TaskId (not linked to any task)",
                    AffectedEntityType = TargetEntityType.Task,
                    AffectedEntityId = null, // No task ID
                    RulesReferenced = new List<string> { "EvidenceGapDetector.Rule1: Evidence must be linked to a task" }
                });
                continue;
            }

            // Deterministic check: TaskId must match a task in ProgramTasks
            var taskExists = context.ProgramTasks.Any(t => t.Id == evidence.TaskId);

            if (!taskExists)
            {
                gaps.Add(new Gap
                {
                    GapType = GapType.UnmappedEvidence,
                    Description = $"Evidence {evidence.Id} is linked to task {evidence.TaskId} which does not exist in the program",
                    AffectedEntityType = TargetEntityType.Task,
                    AffectedEntityId = evidence.TaskId,
                    RulesReferenced = new List<string> { "EvidenceGapDetector.Rule1: Evidence must be linked to a valid task" }
                });
            }
        }
    }

    /// <summary>
    /// Rule: Evidence is marked weak or incomplete.
    /// Detection Logic:
    /// - Check EvidenceType for weak indicators (e.g., "note", "comment", "draft")
    /// - Check if evidence is very old (older than 1 year)
    /// - Check if Metadata indicates incomplete status
    /// - This is a deterministic check: string matching and date comparison only
    /// </summary>
    private void DetectWeakOrIncompleteEvidence(ComplianceContext context, List<Gap> gaps)
    {
        // Rule Check: Only process if we have evidence
        if (context.Evidence.Count == 0)
        {
            return;
        }

        var oneYearAgo = DateTime.UtcNow.AddYears(-1);

        foreach (var evidence in context.Evidence)
        {
            var isWeak = false;
            var reasons = new List<string>();

            // Rule Check 1: EvidenceType indicates weak evidence
            // Deterministic check: Exact string matching (case-insensitive)
            if (IsWeakEvidenceType(evidence.EvidenceType))
            {
                isWeak = true;
                reasons.Add($"Evidence type '{evidence.EvidenceType}' is considered weak");
            }

            // Rule Check 2: Evidence is very old (older than 1 year)
            // Deterministic check: DateTime comparison
            if (evidence.UploadedOn < oneYearAgo)
            {
                isWeak = true;
                var daysOld = (DateTime.UtcNow - evidence.UploadedOn).Days;
                reasons.Add($"Evidence is {daysOld} days old (older than 1 year)");
            }

            // Rule Check 3: Metadata indicates incomplete status
            // Deterministic check: String contains check (case-insensitive)
            if (!string.IsNullOrWhiteSpace(evidence.Metadata))
            {
                var metadata = evidence.Metadata.ToLowerInvariant();
                if (metadata.Contains("incomplete", StringComparison.OrdinalIgnoreCase) ||
                    metadata.Contains("draft", StringComparison.OrdinalIgnoreCase) ||
                    metadata.Contains("pending", StringComparison.OrdinalIgnoreCase))
                {
                    isWeak = true;
                    reasons.Add("Evidence metadata indicates incomplete status");
                }
            }

            if (isWeak)
            {
                gaps.Add(new Gap
                {
                    GapType = GapType.WeakEvidence,
                    Description = $"Evidence {evidence.Id} is weak or incomplete: {string.Join("; ", reasons)}",
                    AffectedEntityType = TargetEntityType.Task,
                    AffectedEntityId = evidence.TaskId,
                    RulesReferenced = new List<string> { "EvidenceGapDetector.Rule2: Evidence must be complete and current" }
                });
            }
        }
    }

    /// <summary>
    /// Helper: Determines if evidence type is considered weak.
    /// Rule: Evidence types "note", "comment", "observation", "verbal", "informal" are weak.
    /// This is a deterministic check: exact string matching (case-insensitive).
    /// </summary>
    private static bool IsWeakEvidenceType(string evidenceType)
    {
        if (string.IsNullOrWhiteSpace(evidenceType))
        {
            return true; // Missing type is considered weak
        }

        var weakTypes = new[]
        {
            "note", "comment", "observation", "verbal", "informal", "draft", "placeholder"
        };

        return weakTypes.Contains(evidenceType.Trim().ToLowerInvariant());
    }
}
