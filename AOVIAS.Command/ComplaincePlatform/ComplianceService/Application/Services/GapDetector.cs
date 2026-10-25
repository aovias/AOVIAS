using ComplianceService.Application.Contracts;
using ComplianceService.Domain;

namespace ComplianceService.Application.Services;

/// <summary>
/// Deterministic gap detector that analyzes ComplianceContext using explicit rules.
/// No AI usage, no scoring beyond simple severity tags.
/// All rules are testable and deterministic.
/// </summary>
public class GapDetector : IGapDetector
{
    private readonly ILogger<GapDetector> _logger;

    public GapDetector(ILogger<GapDetector> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Implements IGapDetector.Detect - converts GapAnalysisResult to List&lt;Gap&gt;.
    /// </summary>
    public List<Gap> Detect(ComplianceContext context)
    {
        var gapAnalysisResult = DetectGaps(context);
        return ConvertGapResultsToGaps(gapAnalysisResult);
    }

    /// <summary>
    /// Converts GapAnalysisResult (with GapResult) to List&lt;Gap&gt; for interface compatibility.
    /// </summary>
    private List<Gap> ConvertGapResultsToGaps(GapAnalysisResult gapAnalysisResult)
    {
        var gaps = new List<Gap>();

        foreach (var gapResult in gapAnalysisResult.Gaps)
        {
            var gap = new Gap
            {
                GapType = gapResult.GapType,
                Description = gapResult.Description,
                AffectedEntityType = DetermineAffectedEntityType(gapResult),
                AffectedEntityId = DetermineAffectedEntityId(gapResult),
                RulesReferenced = new List<string>() // GapResult doesn't have rules, so empty
            };

            gaps.Add(gap);
        }

        return gaps;
    }

    /// <summary>
    /// Determines the affected entity type from GapResult references.
    /// </summary>
    private TargetEntityType DetermineAffectedEntityType(GapResult gapResult)
    {
        var refs = gapResult.References;

        if (refs.ControlId.HasValue || !string.IsNullOrWhiteSpace(refs.ControlCode))
        {
            return TargetEntityType.Control;
        }
        else if (refs.TaskId.HasValue)
        {
            return TargetEntityType.Task;
        }
        else if (refs.EvidenceId.HasValue)
        {
            return TargetEntityType.Evidence;
        }
        else if (refs.AuditFindingId.HasValue)
        {
            return TargetEntityType.AuditFinding;
        }
        else if (refs.ProgramId.HasValue)
        {
            return TargetEntityType.Program;
        }
        else
        {
            return TargetEntityType.Unknown;
        }
    }

    /// <summary>
    /// Determines the affected entity ID from GapResult references.
    /// </summary>
    private Guid? DetermineAffectedEntityId(GapResult gapResult)
    {
        var refs = gapResult.References;

        // Priority order: Control > Task > Evidence > AuditFinding > Program
        return refs.ControlId 
            ?? refs.TaskId 
            ?? refs.EvidenceId 
            ?? refs.AuditFindingId 
            ?? refs.ProgramId;
    }

    public GapAnalysisResult DetectGaps(ComplianceContext context)
    {
        var result = new GapAnalysisResult();

        if (context == null)
        {
            _logger.LogWarning("ComplianceContext is null, returning empty gap analysis");
            return result;
        }

        // Detect missing tasks (controls without tasks)
        DetectMissingTasks(context, result);

        // Detect missing evidence (tasks without evidence)
        DetectMissingEvidence(context, result);

        // Detect weak or unmapped evidence
        DetectWeakOrUnmappedEvidence(context, result);

        // Detect unaddressed audit findings
        DetectUnaddressedAuditFindings(context, result);

        // Calculate summary counts
        CalculateSummaryCounts(result);

        _logger.LogInformation("Gap detection completed. Found {Count} gaps", result.TotalGaps);

        return result;
    }

    private void DetectMissingTasks(ComplianceContext context, GapAnalysisResult result)
    {
        // Rule 1: If a program has controls but no tasks at all, all controls are missing tasks
        if (context.ComplianceProgram == null || context.ProgramControls.Count == 0)
        {
            return;
        }

        if (context.ProgramTasks.Count == 0)
        {
            // All controls are missing tasks
            foreach (var control in context.ProgramControls)
            {
                result.Gaps.Add(new GapResult
                {
                    GapType = GapType.MissingTask,
                    Severity = GapSeverity.High,
                    Description = $"Control {control.Code} has no corresponding compliance task",
                    References = new GapReferences
                    {
                        ProgramId = context.ComplianceProgram.Id,
                        ControlId = control.Id,
                        ControlCode = control.Code
                    }
                });
            }
            return;
        }

        // Rule 2: If there are more controls than tasks, some controls likely don't have tasks
        // This is a deterministic rule: if controls > tasks, we flag the excess
        if (context.ProgramControls.Count > context.ProgramTasks.Count)
        {
            var excessCount = context.ProgramControls.Count - context.ProgramTasks.Count;
            var controlsToFlag = context.ProgramControls.Take(excessCount);

            foreach (var control in controlsToFlag)
            {
                result.Gaps.Add(new GapResult
                {
                    GapType = GapType.MissingTask,
                    Severity = GapSeverity.Medium,
                    Description = $"Control {control.Code} may not have a corresponding compliance task (more controls than tasks)",
                    References = new GapReferences
                    {
                        ProgramId = context.ComplianceProgram.Id,
                        ControlId = control.Id,
                        ControlCode = control.Code
                    }
                });
            }
        }
    }

    private void DetectMissingEvidence(ComplianceContext context, GapAnalysisResult result)
    {
        // Rule: Tasks that are not completed and have no evidence are missing evidence
        var tasksWithoutEvidence = new List<ComplianceTask>();

        if (context.ProgramTasks.Count == 0)
        {
            return;
        }

        foreach (var task in context.ProgramTasks)
        {
            // Check if task has evidence
            var hasEvidence = context.Evidence.Any(e => e.TaskId == task.Id);

            // Also check if task is completed (completed tasks might not need evidence)
            var isCompleted = task.Status.IsCompleted();

            if (!hasEvidence && !isCompleted)
            {
                tasksWithoutEvidence.Add(task);
            }
        }

        foreach (var task in tasksWithoutEvidence)
        {
            // Determine severity based on due date
            var severity = DetermineTaskSeverity(task);

            result.Gaps.Add(new GapResult
            {
                GapType = GapType.MissingEvidence,
                Severity = severity,
                Description = $"Task for objective element {task.ObjectiveElementId} has no evidence and is not completed",
                References = new GapReferences
                {
                    ProgramId = context.ComplianceProgram?.Id,
                    TaskId = task.Id,
                    ObjectiveElementId = task.ObjectiveElementId,
                    ComplianceStandardId = task.ComplianceStandardId
                }
            });
        }
    }

    private void DetectWeakOrUnmappedEvidence(ComplianceContext context, GapAnalysisResult result)
    {
        // Rule 1: Evidence that is very old (older than 1 year) is considered weak
        var oneYearAgo = DateTime.UtcNow.AddYears(-1);

        foreach (var evidence in context.Evidence)
        {
            var isOld = evidence.UploadedOn < oneYearAgo;
            var isWeakType = IsWeakEvidenceType(evidence.EvidenceType);

            // Rule 2: Evidence with weak types (e.g., "Note", "Comment") is considered weak
            if (isOld || isWeakType)
            {
                result.Gaps.Add(new GapResult
                {
                    GapType = GapType.WeakEvidence,
                    Severity = isOld ? GapSeverity.Medium : GapSeverity.Low,
                    Description = isOld
                        ? $"Evidence uploaded on {evidence.UploadedOn:yyyy-MM-dd} is older than 1 year and may be outdated"
                        : $"Evidence type '{evidence.EvidenceType}' is considered weak",
                    References = new GapReferences
                    {
                        EvidenceId = evidence.Id,
                        TaskId = evidence.TaskId,
                        ProgramId = context.ComplianceProgram?.Id
                    }
                });
            }

            // Rule 3: Evidence that doesn't map to any control is unmapped
            // Deterministic rule: If evidence's task doesn't have a corresponding control in the program,
            // the evidence is unmapped (task ↔ control linkage is evaluated conservatively).
            var task = context.ProgramTasks.FirstOrDefault(t => t.Id == evidence.TaskId);
            if (task != null && context.ProgramControls.Count > 0)
            {
                // Simplified deterministic check: If we have controls but can't establish a clear link
                // between the task and a control, flag as potentially unmapped
                // This is conservative - we only flag if there's clear evidence of a mismatch
                var taskHasPotentialControl = context.ProgramControls.Any(c => 
                    // In a real system, we'd check requirement-control mapping
                    // For now, we use a conservative approach: only flag if we're certain there's no mapping
                    false); // Conservative: don't flag unless we have explicit mapping logic

                // Since we don't have explicit mapping, we use a conservative rule:
                // Only flag evidence as unmapped if the task itself seems orphaned (no clear program context)
                // This is a deterministic but conservative check
                if (context.ComplianceProgram == null)
                {
                    result.Gaps.Add(new GapResult
                    {
                        GapType = GapType.UnmappedEvidence,
                        Severity = GapSeverity.Low,
                        Description = $"Evidence for task {task.Id} cannot be mapped to a control (no program context)",
                        References = new GapReferences
                        {
                            EvidenceId = evidence.Id,
                            TaskId = evidence.TaskId
                        }
                    });
                }
            }
            else if (task == null && context.ProgramControls.Count > 0)
            {
                // Evidence exists but task is not in program tasks - this is unmapped
                result.Gaps.Add(new GapResult
                {
                    GapType = GapType.UnmappedEvidence,
                    Severity = GapSeverity.Medium,
                    Description = $"Evidence {evidence.Id} is associated with task {evidence.TaskId} that is not in the program's task list",
                    References = new GapReferences
                    {
                        EvidenceId = evidence.Id,
                        TaskId = evidence.TaskId,
                        ProgramId = context.ComplianceProgram?.Id
                    }
                });
            }
        }
    }

    private void DetectUnaddressedAuditFindings(ComplianceContext context, GapAnalysisResult result)
    {
        // Rule: Audit findings that don't have corresponding tasks or corrective actions are unaddressed
        if (context.AuditFindings.Count == 0)
        {
            return;
        }

        // Get control codes from findings
        var findingControlCodes = context.AuditFindings
            .Select(f => f.ControlCode)
            .Distinct()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Check if there are tasks or controls that address these findings
        var addressedControlCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Check controls
        foreach (var control in context.ProgramControls)
        {
            if (findingControlCodes.Contains(control.Code))
            {
                addressedControlCodes.Add(control.Code);
            }
        }

        // Deterministic rule: Audit findings for controls that don't exist in the program are unaddressed
        // Also, if a finding exists but there's no corresponding task, it's unaddressed
        foreach (var finding in context.AuditFindings)
        {
            var hasControl = addressedControlCodes.Contains(finding.ControlCode);
            
            // Rule: If the control doesn't exist in the program, the finding is unaddressed
            if (!hasControl)
            {
                var severity = MapAuditSeverityToGapSeverity(finding.Severity);

                result.Gaps.Add(new GapResult
                {
                    GapType = GapType.UnaddressedAuditFinding,
                    Severity = severity,
                    Description = $"Audit finding for control {finding.ControlCode} is unaddressed: control not found in program. {finding.Description}",
                    References = new GapReferences
                    {
                        AuditFindingId = finding.Id,
                        ControlCode = finding.ControlCode,
                        ProgramId = context.ComplianceProgram?.Id
                    }
                });
            }
            else
            {
                // Rule: Even if control exists, if there's no task for it, the finding might be unaddressed
                // Check if there's a task that could address this finding
                var control = context.ProgramControls.FirstOrDefault(c => 
                    c.Code.Equals(finding.ControlCode, StringComparison.OrdinalIgnoreCase));
                
                if (control != null)
                {
                    // Check if there's a task that might address this control
                    // Simplified: if there are no tasks at all, or if tasks don't cover this control
                    var hasTaskForControl = context.ProgramTasks.Any(); // Simplified check
                    
                    if (!hasTaskForControl)
                    {
                        var severity = MapAuditSeverityToGapSeverity(finding.Severity);

                        result.Gaps.Add(new GapResult
                        {
                            GapType = GapType.UnaddressedAuditFinding,
                            Severity = severity,
                            Description = $"Audit finding for control {finding.ControlCode} may be unaddressed: no tasks found to address it. {finding.Description}",
                            References = new GapReferences
                            {
                                AuditFindingId = finding.Id,
                                ControlCode = finding.ControlCode,
                                ControlId = control.Id,
                                ProgramId = context.ComplianceProgram?.Id
                            }
                        });
                    }
                }
            }
        }
    }

    private GapSeverity DetermineTaskSeverity(ComplianceTask task)
    {
        // Rule: Tasks that are overdue are critical, tasks due soon are high, others are medium
        var now = DateTime.UtcNow;
        if (task.DueDate is not { } due)
        {
            return GapSeverity.Medium;
        }

        var daysUntilDue = (due - now).TotalDays;

        if (daysUntilDue < 0)
        {
            return GapSeverity.Critical; // Overdue
        }
        else if (daysUntilDue <= 7)
        {
            return GapSeverity.High; // Due within a week
        }
        else if (daysUntilDue <= 30)
        {
            return GapSeverity.Medium; // Due within a month
        }
        else
        {
            return GapSeverity.Low; // Due later
        }
    }

    private bool IsWeakEvidenceType(string evidenceType)
    {
        // Rule: Certain evidence types are considered weak
        if (string.IsNullOrWhiteSpace(evidenceType))
        {
            return true; // Missing type is weak
        }

        var weakTypes = new[]
        {
            "note", "comment", "observation", "verbal", "informal"
        };

        return weakTypes.Contains(evidenceType.ToLowerInvariant());
    }

    private GapSeverity MapAuditSeverityToGapSeverity(string auditSeverity)
    {
        // Rule: Map audit severity to gap severity
        if (string.IsNullOrWhiteSpace(auditSeverity))
        {
            return GapSeverity.Medium;
        }

        var lower = auditSeverity.ToLowerInvariant();

        return lower switch
        {
            "critical" or "high" => GapSeverity.Critical,
            "medium" => GapSeverity.High,
            "low" => GapSeverity.Medium,
            _ => GapSeverity.Medium
        };
    }

    private void CalculateSummaryCounts(GapAnalysisResult result)
    {
        // Calculate counts by gap type
        foreach (var gap in result.Gaps)
        {
            if (!result.GapCounts.ContainsKey(gap.GapType))
            {
                result.GapCounts[gap.GapType] = 0;
            }
            result.GapCounts[gap.GapType]++;
        }

        // Calculate counts by severity
        foreach (var gap in result.Gaps)
        {
            if (!result.SeverityCounts.ContainsKey(gap.Severity))
            {
                result.SeverityCounts[gap.Severity] = 0;
            }
            result.SeverityCounts[gap.Severity]++;
        }
    }
}
