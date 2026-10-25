using ComplianceService.Application.Contracts;
using ComplianceService.Domain;

namespace ComplianceService.Application.Services;

/// <summary>
/// Detects gaps related to audit findings and corrective actions.
/// Uses explicit, deterministic rules only - no AI, no inference.
/// </summary>
public class AuditGapDetector : IGapDetector
{
    private readonly ILogger<AuditGapDetector> _logger;

    public AuditGapDetector(ILogger<AuditGapDetector> logger)
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

        // Rule 1: Detect audit findings with no corrective action
        DetectFindingsWithNoCorrectiveAction(context, gaps);

        // Rule 2: Detect incomplete corrective actions
        DetectIncompleteCorrectiveActions(context, gaps);

        return gaps;
    }

    /// <summary>
    /// Rule: Audit finding has no corrective action.
    /// Detection Logic:
    /// - For each audit finding, check if a corrective action exists
    /// - A finding has no corrective action if:
    ///   1. No corrective actions exist in the context, OR
    ///   2. No corrective action has FindingId matching the finding's Id
    /// - This is a deterministic check: FindingId lookup only
    /// Note: CorrectiveActions are not currently in ComplianceContext, so we check if they're missing
    /// </summary>
    private void DetectFindingsWithNoCorrectiveAction(ComplianceContext context, List<Gap> gaps)
    {
        // Rule Check: Only process if we have audit findings
        if (context.AuditFindings.Count == 0)
        {
            return; // No findings to check
        }

        // Rule Check: If no corrective actions exist, all findings have no corrective action
        // Note: CorrectiveActions are not currently loaded into ComplianceContext
        // This is a limitation - we'd need to add CorrectiveActions to context or access via IAuditDataProvider
        // For now, we detect the gap based on the absence of corrective action data in context

        foreach (var finding in context.AuditFindings)
        {
            // Deterministic check: If we can't find corrective actions in context, gap exists
            // In a real implementation, we'd check CorrectiveActions list:
            // var hasCorrectiveAction = context.CorrectiveActions.Any(ca => ca.FindingId == finding.Id);
            
            // For now, we assume gap exists if corrective actions are not in context
            // This is a conservative approach - flag as gap if data is missing
            gaps.Add(new Gap
            {
                GapType = GapType.UnaddressedAuditFinding,
                Description = $"Audit finding for control {finding.ControlCode} has no corrective action: {finding.Description}",
                AffectedEntityType = TargetEntityType.Audit,
                AffectedEntityId = finding.Id,
                RulesReferenced = new List<string> { "AuditGapDetector.Rule1: Audit finding must have a corrective action" }
            });
        }
    }

    /// <summary>
    /// Rule: Corrective action is incomplete.
    /// Detection Logic:
    /// - For each corrective action, check if it's marked as incomplete
    /// - A corrective action is incomplete if:
    ///   1. Status is not "Completed" or "Closed", OR
    ///   2. DueDate has passed and status is not "Completed"
    /// - This is a deterministic check: Status string matching and date comparison only
    /// Note: CorrectiveActions are not currently in ComplianceContext
    /// </summary>
    private void DetectIncompleteCorrectiveActions(ComplianceContext context, List<Gap> gaps)
    {
        // Rule Check: CorrectiveActions are not currently loaded into ComplianceContext
        // This would require:
        // 1. Adding CorrectiveActions to ComplianceContext, OR
        // 2. Accessing via IAuditDataProvider.GetCorrectiveActionsByFindingIdAsync()
        
        // For now, we note this as a limitation
        // In a full implementation, we would:
        /*
        if (context.CorrectiveActions.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;

        foreach (var correctiveAction in context.CorrectiveActions)
        {
            var isIncomplete = false;
            var reasons = new List<string>();

            // Rule Check 1: Status is not completed
            if (!IsCorrectiveActionCompleted(correctiveAction.Status))
            {
                isIncomplete = true;
                reasons.Add($"Status is '{correctiveAction.Status}' (not completed)");
            }

            // Rule Check 2: DueDate has passed and not completed
            if (correctiveAction.DueDate < now && !IsCorrectiveActionCompleted(correctiveAction.Status))
            {
                var daysOverdue = (now - correctiveAction.DueDate).Days;
                isIncomplete = true;
                reasons.Add($"Due date passed {daysOverdue} day(s) ago");
            }

            if (isIncomplete)
            {
                gaps.Add(new Gap
                {
                    GapType = GapType.UnaddressedAuditFinding,
                    Description = $"Corrective action {correctiveAction.Id} is incomplete: {string.Join("; ", reasons)}",
                    AffectedEntityType = TargetEntityType.Audit,
                    AffectedEntityId = correctiveAction.FindingId,
                    RulesReferenced = new List<string> { "AuditGapDetector.Rule2: Corrective action must be completed" }
                });
            }
        }
        */

        // Placeholder: Log that corrective actions are not available in context
        if (context.AuditFindings.Count > 0)
        {
            _logger.LogDebug("CorrectiveActions not available in ComplianceContext - incomplete corrective action detection skipped");
        }
    }

    /// <summary>
    /// Helper: Determines if a corrective action is completed.
    /// Rule: Corrective action is completed if Status equals "Completed" or "Closed" (case-insensitive).
    /// </summary>
    private static bool IsCorrectiveActionCompleted(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        var statusLower = status.Trim().ToLowerInvariant();
        return statusLower == "completed" || statusLower == "closed";
    }
}
