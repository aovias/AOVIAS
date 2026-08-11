using AIOrchestratorService.Application.AI.Configuration;
using AIOrchestratorService.Application.AI.Models;
using Microsoft.Extensions.Options;

namespace AIOrchestratorService.Application.AI.Autonomy;

public sealed class AutonomyDecision
{
    public bool Allowed { get; set; }
    public bool RequiresApproval { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public interface IAutonomyPolicyEngine
{
    AutonomyDecision Evaluate(WorkflowStepTemplate step, decimal confidenceScore, string tenantKey);
}

public sealed class AutonomyPolicyEngine : IAutonomyPolicyEngine
{
    private readonly IOptionsMonitor<AIConfidencePolicyOptions> _confidencePolicy;
    private readonly IOptionsMonitor<AutonomyControlOptions> _autonomyControl;

    public AutonomyPolicyEngine(
        IOptionsMonitor<AIConfidencePolicyOptions> confidencePolicy,
        IOptionsMonitor<AutonomyControlOptions> autonomyControl)
    {
        _confidencePolicy = confidencePolicy;
        _autonomyControl = autonomyControl;
    }

    public AutonomyDecision Evaluate(WorkflowStepTemplate step, decimal confidenceScore, string tenantKey)
    {
        var control = _autonomyControl.CurrentValue;
        if (!control.EnableAutonomousWorkflows)
        {
            return new AutonomyDecision { Allowed = false, RequiresApproval = true, Reason = "Autonomous workflows disabled." };
        }

        if (!control.EnableAutoExecution)
        {
            return new AutonomyDecision { Allowed = true, RequiresApproval = true, Reason = "Auto execution disabled by policy." };
        }

        if (control.ForceApprovalForAllActions ||
            string.Equals(control.RolloutStage, "D0", StringComparison.OrdinalIgnoreCase))
        {
            return new AutonomyDecision
            {
                Allowed = true,
                RequiresApproval = true,
                Reason = "D0 hardening mode requires approval for every action."
            };
        }

        var thresholds = _confidencePolicy.CurrentValue;
        var requiresApproval = step.RequiresApproval ||
                               step.RiskClass == AIActionRiskClass.High ||
                               confidenceScore < thresholds.WarnThreshold;

        return new AutonomyDecision
        {
            Allowed = true,
            RequiresApproval = requiresApproval,
            Reason = requiresApproval ? "Approval required by risk/confidence policy." : "Step allowed for auto execution."
        };
    }
}
