using System.Net.Http.Json;
using AIOrchestratorService.Application.AI.Governance;
using AIOrchestratorService.Application.AI.Models;
using AIOrchestratorService.Application.AI.Retrieval;
using AIOrchestratorService.Application.AI.Configuration;
using Microsoft.Extensions.Options;

namespace AIOrchestratorService.Application.AI.Actions;

public sealed class ToolCompensationStrategy
{
    public string UndoAction { get; set; } = string.Empty;
    public string ReverseOperation { get; set; } = string.Empty;
    public string FallbackBehavior { get; set; } = string.Empty;
}

public sealed class ToolDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public string SchemaVersion { get; set; } = "1.0";
    public string CompatibilityRules { get; set; } = string.Empty;
    public string InputSchema { get; set; } = "{}";
    public string OutputSchema { get; set; } = "{}";
    public string IdempotencyKeyPattern { get; set; } = string.Empty;
    public AIActionRiskClass RiskLevel { get; set; } = AIActionRiskClass.Low;
    public ToolCompensationStrategy CompensationStrategy { get; set; } = new();
}

public interface IAIToolManifest
{
    bool IsAllowed(string toolName);
}

public interface IAIToolRegistry : IAIToolManifest
{
    ToolDefinition? Get(string toolName);
    bool IsCompatible(string toolName, string? requestedVersion, string? requestedSchemaVersion);
}

public sealed class AIToolRegistry : IAIToolRegistry
{
    private static readonly Dictionary<string, ToolDefinition> Tools = new(StringComparer.OrdinalIgnoreCase)
    {
        ["create_finding"] = new ToolDefinition
        {
            Name = "create_finding",
            Version = "1.0",
            SchemaVersion = "1.0",
            CompatibilityRules = "backward-compatible payload required",
            IdempotencyKeyPattern = "finding:{tenant}:{objectiveElementId}:{titleHash}",
            RiskLevel = AIActionRiskClass.High,
            CompensationStrategy = new ToolCompensationStrategy
            {
                UndoAction = "close_finding",
                ReverseOperation = "mark_finding_reversed",
                FallbackBehavior = "escalate_to_human"
            }
        },
        ["link_evidence"] = new ToolDefinition
        {
            Name = "link_evidence",
            Version = "1.0",
            SchemaVersion = "1.0",
            CompatibilityRules = "objectiveElement and evidence must exist",
            IdempotencyKeyPattern = "evidence-link:{tenant}:{objectiveElementId}:{evidenceId}",
            RiskLevel = AIActionRiskClass.Low,
            CompensationStrategy = new ToolCompensationStrategy
            {
                UndoAction = "unlink_evidence",
                ReverseOperation = "remove_link_record",
                FallbackBehavior = "manual_review"
            }
        },
        ["create_task"] = new ToolDefinition
        {
            Name = "create_task",
            Version = "1.0",
            SchemaVersion = "1.0",
            CompatibilityRules = "assigned user and objective element required",
            IdempotencyKeyPattern = "task:{tenant}:{objectiveElementId}:{descriptionHash}",
            RiskLevel = AIActionRiskClass.High,
            CompensationStrategy = new ToolCompensationStrategy
            {
                UndoAction = "cancel_task",
                ReverseOperation = "mark_task_void",
                FallbackBehavior = "reassign_to_owner"
            }
        },
        ["validate_evidence"] = new ToolDefinition
        {
            Name = "validate_evidence",
            Version = "1.0",
            SchemaVersion = "1.0",
            CompatibilityRules = "read-only operation",
            IdempotencyKeyPattern = "readonly:{tenant}:{evidenceId}",
            RiskLevel = AIActionRiskClass.Low,
            CompensationStrategy = new ToolCompensationStrategy
            {
                UndoAction = "none",
                ReverseOperation = "none",
                FallbackBehavior = "none"
            }
        },
        ["query_tasks"] = new ToolDefinition
        {
            Name = "query_tasks",
            Version = "1.0",
            SchemaVersion = "1.0",
            CompatibilityRules = "read-only operation",
            IdempotencyKeyPattern = "readonly:{tenant}:{goalHash}",
            RiskLevel = AIActionRiskClass.Low,
            CompensationStrategy = new ToolCompensationStrategy
            {
                UndoAction = "none",
                ReverseOperation = "none",
                FallbackBehavior = "none"
            }
        },
        ["analyze_missing_oes"] = new ToolDefinition
        {
            Name = "analyze_missing_oes",
            Version = "1.0",
            SchemaVersion = "1.0",
            CompatibilityRules = "read-only operation",
            IdempotencyKeyPattern = "readonly:{tenant}:{objectiveElementId}",
            RiskLevel = AIActionRiskClass.Low,
            CompensationStrategy = new ToolCompensationStrategy
            {
                UndoAction = "none",
                ReverseOperation = "none",
                FallbackBehavior = "none"
            }
        },
        ["get_deficiencies"] = ReadOnlyTool("get_deficiencies", "hospital framework scope"),
        ["get_capas"] = ReadOnlyTool("get_capas", "hospital framework scope"),
        ["get_sla_health"] = ReadOnlyTool("get_sla_health", "hospital framework scope"),
        ["get_escalations"] = ReadOnlyTool("get_escalations", "hospital framework scope"),
        ["get_stage_unlock_status"] = ReadOnlyTool("get_stage_unlock_status", "optional stage parameter"),
        ["get_analytics_overview"] = ReadOnlyTool("get_analytics_overview", "hospital framework scope"),
        ["get_department_metrics"] = ReadOnlyTool("get_department_metrics", "hospital framework scope"),
        ["get_recurring_issues"] = ReadOnlyTool("get_recurring_issues", "hospital framework scope"),
        // Phase 5.2 — additional READ-only tools via ComplianceClient
        ["list_overdue_capas"] = ReadOnlyTool("list_overdue_capas", "hospital framework scope; overdue CAPA list"),
        ["list_oe_evidence_gaps"] = ReadOnlyTool("list_oe_evidence_gaps", "hospital framework scope; OE evidence gaps"),
        ["get_dept_readiness"] = ReadOnlyTool("get_dept_readiness", "hospital framework scope; department readiness")
    };

    private static ToolDefinition ReadOnlyTool(string name, string rules) => new()
    {
        Name = name,
        Version = "1.0",
        SchemaVersion = "1.0",
        CompatibilityRules = rules,
        IdempotencyKeyPattern = $"readonly:{{tenant}}:{name}",
        RiskLevel = AIActionRiskClass.Low,
        CompensationStrategy = new ToolCompensationStrategy
        {
            UndoAction = "none",
            ReverseOperation = "none",
            FallbackBehavior = "none"
        }
    };

    public bool IsAllowed(string toolName) => Tools.ContainsKey(toolName);

    public ToolDefinition? Get(string toolName)
    {
        return Tools.TryGetValue(toolName, out var tool) ? tool : null;
    }

    public bool IsCompatible(string toolName, string? requestedVersion, string? requestedSchemaVersion)
    {
        if (!Tools.TryGetValue(toolName, out var tool))
        {
            return false;
        }

        var versionCompatible = string.IsNullOrWhiteSpace(requestedVersion) ||
                                string.Equals(requestedVersion, tool.Version, StringComparison.OrdinalIgnoreCase);
        var schemaCompatible = string.IsNullOrWhiteSpace(requestedSchemaVersion) ||
                               string.Equals(requestedSchemaVersion, tool.SchemaVersion, StringComparison.OrdinalIgnoreCase);
        return versionCompatible && schemaCompatible;
    }
}

public interface IToolPreExecutionValidator
{
    Task<AIToolPreviewResult> ValidateAsync(AIRequest request, AISuggestedAction action, bool approved, CancellationToken cancellationToken);
}

public sealed class ToolPreExecutionValidator : IToolPreExecutionValidator
{
    private readonly IAIToolRegistry _manifest;
    private readonly IComplianceContextClient _contextClient;
    private readonly IConfidenceCalibrationService _confidenceCalibration;
    private readonly IOptionsMonitor<AutonomyControlOptions> _autonomyControl;
    private readonly Identity.Contracts.IAIProfilePermissionService? _profilePermissions;
    private readonly Identity.Contracts.IAIAutomationPolicyService? _automationPolicy;

    public ToolPreExecutionValidator(
        IAIToolRegistry manifest,
        IComplianceContextClient contextClient,
        IConfidenceCalibrationService confidenceCalibration,
        IOptionsMonitor<AutonomyControlOptions> autonomyControl,
        Identity.Contracts.IAIProfilePermissionService profilePermissions,
        Identity.Contracts.IAIAutomationPolicyService automationPolicy)
    {
        _manifest = manifest;
        _contextClient = contextClient;
        _confidenceCalibration = confidenceCalibration;
        _autonomyControl = autonomyControl;
        _profilePermissions = profilePermissions;
        _automationPolicy = automationPolicy;
    }

    public async Task<AIToolPreviewResult> ValidateAsync(AIRequest request, AISuggestedAction action, bool approved, CancellationToken cancellationToken)
    {
        if (!_manifest.IsAllowed(action.ToolName))
        {
            return new AIToolPreviewResult { Allowed = false, Reason = "Tool is not in allowed manifest." };
        }

        if (request.AiProfileId.HasValue && _profilePermissions != null)
        {
            var allowsExecute = await _profilePermissions.ProfileAllowsActionAsync(request.AiProfileId.Value, Domain.Enums.AIActionType.Draft, cancellationToken);
            if (!allowsExecute)
                return new AIToolPreviewResult { Allowed = false, Reason = "AI profile does not grant action permission for this tool." };
            if (_automationPolicy != null)
            {
                var mode = await _automationPolicy.ResolveModeAsync(request.AiProfileId.Value, 0.8m, cancellationToken);
                if (mode == Domain.Enums.AutomationMode.ManualOnly)
                    return new AIToolPreviewResult { Allowed = false, Reason = "Profile automation policy is manual-only." };
                if (mode == Domain.Enums.AutomationMode.HumanApprovalRequired && !approved)
                    return new AIToolPreviewResult { Allowed = false, Reason = "Human approval required by automation policy." };
            }
        }

        var requestedVersion = action.Parameters.GetValueOrDefault("__toolVersion");
        var requestedSchemaVersion = action.Parameters.GetValueOrDefault("__schemaVersion");
        if (!_manifest.IsCompatible(action.ToolName, requestedVersion, requestedSchemaVersion))
        {
            return new AIToolPreviewResult
            {
                Allowed = false,
                Reason = $"Tool version/schema compatibility check failed for '{action.ToolName}'."
            };
        }

        var context = await _contextClient.GetContextBundleAsync(new ComplianceContextBundleRequest
        {
            HospitalId = request.HospitalId,
            ObjectiveElementId = request.ObjectiveElementId,
            TaskId = request.TaskId,
            EvidenceId = request.EvidenceId
        }, cancellationToken);

        if (context == null)
        {
            return new AIToolPreviewResult { Allowed = false, Reason = "Unable to validate current context state." };
        }

        if (!string.Equals(context.ContextVersionId, request.ContextVersionId, StringComparison.Ordinal))
        {
            return new AIToolPreviewResult { Allowed = false, Reason = "Context version mismatch. Re-evaluation required." };
        }

        var precondition = await _contextClient.ValidateCommandPreconditionsAsync(new ComplianceCommandPreconditionRequest
        {
            HospitalId = request.HospitalId,
            ObjectiveElementId = request.ObjectiveElementId,
            TaskId = request.TaskId,
            EvidenceId = request.EvidenceId,
            ExpectedContextVersionId = request.ContextVersionId,
            IdempotencyToken = request.IdempotencyToken,
            ApprovalAttested = approved
        }, cancellationToken);
        if (precondition is { IsAllowed: false })
        {
            return new AIToolPreviewResult
            {
                Allowed = false,
                Reason = precondition.Reason,
                Action = action
            };
        }

        var tool = _manifest.Get(action.ToolName);
        var needsApproval = _confidenceCalibration.RequiresHumanApproval(0.7m, action.RiskClass);
        if (tool != null && tool.RiskLevel == AIActionRiskClass.High)
        {
            needsApproval = true;
        }
        var forceApproval = _autonomyControl.CurrentValue.ForceApprovalForAllActions ||
                            string.Equals(_autonomyControl.CurrentValue.RolloutStage, "D0", StringComparison.OrdinalIgnoreCase);
        if (forceApproval)
        {
            needsApproval = true;
        }

        if (needsApproval && !approved)
        {
            return new AIToolPreviewResult
            {
                Allowed = false,
                Reason = forceApproval ? "D0 hardening mode: explicit human approval is mandatory." : "Human approval required by policy.",
                Action = action,
                ImpactSummary = new Dictionary<string, string>
                {
                    ["approvalRequired"] = "true",
                    ["riskClass"] = action.RiskClass.ToString(),
                    ["toolVersion"] = tool?.Version ?? "unknown",
                    ["rolloutStage"] = _autonomyControl.CurrentValue.RolloutStage
                }
            };
        }

        return new AIToolPreviewResult
        {
            Allowed = true,
            Reason = "Action passed policy checks.",
            Action = action,
            ImpactSummary = new Dictionary<string, string>
            {
                ["tool"] = action.ToolName,
                ["riskClass"] = action.RiskClass.ToString(),
                ["idempotencyToken"] = request.IdempotencyToken ?? "none",
                ["schemaVersion"] = tool?.SchemaVersion ?? "unknown",
                ["toolVersion"] = tool?.Version ?? "unknown"
            }
        };
    }
}

public interface IAIToolExecutor
{
    Task<Dictionary<string, string>> ExecuteAsync(AIRequest request, AISuggestedAction action, CancellationToken cancellationToken);
}

public sealed class AIToolExecutor : IAIToolExecutor
{
    private readonly HttpClient _httpClient;
    private readonly Infrastructure.Clients.IComplianceClient _complianceClient;

    public AIToolExecutor(
        IHttpClientFactory httpClientFactory,
        Infrastructure.Clients.IComplianceClient complianceClient)
    {
        _httpClient = httpClientFactory.CreateClient("ComplianceServiceClient");
        _complianceClient = complianceClient;
    }

    public async Task<Dictionary<string, string>> ExecuteAsync(AIRequest request, AISuggestedAction action, CancellationToken cancellationToken)
    {
        return action.ToolName.ToLowerInvariant() switch
        {
            "create_finding" => await ExecuteCreateFindingAsync(request, action, cancellationToken),
            "create_task" => await ExecuteCreateTaskAsync(request, action, cancellationToken),
            "link_evidence" => await ExecuteLinkEvidenceAsync(request, action, cancellationToken),
            "validate_evidence" => BuildReadOnlyResult("validate_evidence"),
            "query_tasks" => BuildReadOnlyResult("query_tasks"),
            "analyze_missing_oes" => BuildReadOnlyResult("analyze_missing_oes"),
            "get_deficiencies" or "get_capas" or "get_sla_health" or "get_escalations"
                or "get_stage_unlock_status" or "get_analytics_overview" or "get_department_metrics"
                or "get_recurring_issues" or "list_overdue_capas" or "list_oe_evidence_gaps"
                or "get_dept_readiness" => await ExecuteReadOnlyComplianceToolAsync(request, action, cancellationToken),
            "cancel_task" => BuildCompensationResult("cancel_task"),
            "unlink_evidence" => BuildCompensationResult("unlink_evidence"),
            "close_finding" => BuildCompensationResult("close_finding"),
            "mark_task_void" => BuildCompensationResult("mark_task_void"),
            "mark_finding_reversed" => BuildCompensationResult("mark_finding_reversed"),
            "remove_link_record" => BuildCompensationResult("remove_link_record"),
            _ => new Dictionary<string, string> { ["status"] = "ignored", ["reason"] = "unknown_tool" }
        };
    }

    private async Task<Dictionary<string, string>> ExecuteReadOnlyComplianceToolAsync(
        AIRequest request,
        AISuggestedAction action,
        CancellationToken cancellationToken)
    {
        var toolName = action.ToolName.ToLowerInvariant() switch
        {
            "get_deficiencies" => "GetDeficiencies",
            "get_capas" => "GetCapas",
            "get_sla_health" => "GetSlaHealth",
            "get_escalations" => "GetEscalations",
            "get_stage_unlock_status" => "GetStageUnlockStatus",
            "get_analytics_overview" => "GetAnalyticsOverview",
            "get_department_metrics" => "GetDepartmentMetrics",
            "get_recurring_issues" => "GetRecurringIssues",
            "list_overdue_capas" => "ListOverdueCapas",
            "list_oe_evidence_gaps" => "ListOeEvidenceGaps",
            "get_dept_readiness" => "GetDeptReadiness",
            _ => action.ToolName
        };

        var result = await _complianceClient.ExecuteReadOnlyToolAsync(
            request.HospitalId,
            request.OrganizationId ?? request.HospitalId,
            request.FrameworkId,
            toolName,
            action.Parameters,
            cancellationToken);

        return new Dictionary<string, string>(result, StringComparer.OrdinalIgnoreCase);
    }

    private static Dictionary<string, string> BuildReadOnlyResult(string toolName)
    {
        return new Dictionary<string, string>
        {
            ["status"] = "executed",
            ["mode"] = "readonly",
            ["tool"] = toolName
        };
    }

    private static Dictionary<string, string> BuildCompensationResult(string toolName)
    {
        return new Dictionary<string, string>
        {
            ["status"] = "compensated",
            ["mode"] = "compensation",
            ["tool"] = toolName
        };
    }

    private async Task<Dictionary<string, string>> ExecuteCreateFindingAsync(AIRequest request, AISuggestedAction action, CancellationToken cancellationToken)
    {
        var payload = new
        {
            title = action.Parameters.GetValueOrDefault("title", "AI Suggested Finding"),
            description = action.Parameters.GetValueOrDefault("description", request.Prompt),
            objectiveElementId = request.ObjectiveElementId
        };

        using var response = await _httpClient.PostAsJsonAsync("/api/v1.0/findings", payload, cancellationToken);
        return new Dictionary<string, string>
        {
            ["status"] = response.IsSuccessStatusCode ? "executed" : "failed",
            ["httpStatus"] = ((int)response.StatusCode).ToString()
        };
    }

    private async Task<Dictionary<string, string>> ExecuteCreateTaskAsync(AIRequest request, AISuggestedAction action, CancellationToken cancellationToken)
    {
        var payload = new
        {
            objectiveElementId = request.ObjectiveElementId,
            description = action.Parameters.GetValueOrDefault("description", "AI suggested task"),
            assignedToUserId = action.Parameters.GetValueOrDefault("assignedToUserId", "system")
        };

        using var response = await _httpClient.PostAsJsonAsync("/api/v1.0/compliance-tasks", payload, cancellationToken);
        return new Dictionary<string, string>
        {
            ["status"] = response.IsSuccessStatusCode ? "executed" : "failed",
            ["httpStatus"] = ((int)response.StatusCode).ToString()
        };
    }

    private async Task<Dictionary<string, string>> ExecuteLinkEvidenceAsync(AIRequest request, AISuggestedAction action, CancellationToken cancellationToken)
    {
        var payload = new
        {
            objectiveElementId = request.ObjectiveElementId,
            evidenceId = request.EvidenceId
        };

        using var response = await _httpClient.PostAsJsonAsync("/api/v1.0/evidence-binder/link", payload, cancellationToken);
        return new Dictionary<string, string>
        {
            ["status"] = response.IsSuccessStatusCode ? "executed" : "failed",
            ["httpStatus"] = ((int)response.StatusCode).ToString()
        };
    }
}
