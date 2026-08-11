using Aovias.Runtime.Contracts.Deployment;

namespace ComplianceServiceManager.ViewModels.Deployment;

/// <summary>
/// Guided single-service deployment wizard steps (Sprint 12.1 D2).
/// </summary>
public enum DeploymentWizardStep
{
    Environment = 0,
    Package = 1,
    Analyze = 2,
    Review = 3,
    Deploy = 4,
    Monitor = 5,
    Verify = 6
}

/// <summary>
/// Analyze checklist row (Sprint 12.1 D5).
/// </summary>
public sealed class DeploymentAnalyzeCheckItem
{
    public required string Name { get; init; }

    public required string Status { get; set; }
}

/// <summary>
/// Review summary row (Sprint 12.1 D6).
/// </summary>
public sealed class DeploymentReviewRowItem
{
    public required string Label { get; init; }

    public required string Value { get; init; }
}

/// <summary>
/// Live deployment stage row (Sprint 12.1 D8).
/// </summary>
public sealed class DeploymentLiveStageItem
{
    public required string Name { get; init; }

    public required string Status { get; set; }

    public int ProgressPercent { get; set; }
}

/// <summary>
/// Intelligent progress sub-step (Sprint 12.1 D9).
/// </summary>
public sealed class DeploymentProgressSubStepItem
{
    public required string Label { get; init; }

    public required string Status { get; set; }

    public int ProgressPercent { get; set; }
}

/// <summary>
/// Verification checklist row (Sprint 12.1 D14).
/// </summary>
public sealed class DeploymentVerificationCheckItem
{
    public required string Name { get; init; }

    public required string Status { get; set; }
}

/// <summary>
/// Classified operator error (Sprint 12.1 D13, extended Sprint 11.4).
/// </summary>
public sealed class DeploymentOperatorErrorPresentation
{
    public required string Headline { get; init; }

    public required string Category { get; init; }

    public required string Phase { get; init; }

    public required string Component { get; init; }

    public required string Reason { get; init; }

    public required string Details { get; init; }

    public required string Recovery { get; init; }

    public string? ServiceId { get; init; }

    public string? CorrelationId { get; init; }

    public string? DeploymentId { get; init; }

    public string DocumentationLink { get; init; } = "https://docs.aovias.io/deployment/troubleshooting";

    public static DeploymentOperatorErrorPresentation FromHttp(Exception exception, string? serviceId = null)
    {
        var mapped = Services.Deployment.DeploymentHttpErrorPresenter.Map(exception, serviceId);
        return new DeploymentOperatorErrorPresentation
        {
            Headline = "Deployment Failed",
            Category = mapped.Category,
            Phase = mapped.Phase,
            Component = mapped.Component,
            Reason = mapped.Reason,
            Details = mapped.Details,
            Recovery = mapped.Recovery,
            ServiceId = mapped.ServiceId,
            CorrelationId = mapped.CorrelationId,
            DeploymentId = mapped.DeploymentId
        };
    }

    public static DeploymentOperatorErrorPresentation FromSignalR(DeploymentOperatorErrorDto failure)
        => new()
        {
            Headline = "Deployment Failed",
            Category = failure.Category,
            Phase = failure.Phase,
            Component = failure.Component,
            Reason = failure.Reason,
            Details = BuildSignalRDetails(failure),
            Recovery = failure.RecoveryAction,
            ServiceId = failure.ServiceId,
            CorrelationId = failure.CorrelationId,
            DeploymentId = failure.DeploymentId,
            DocumentationLink = string.IsNullOrWhiteSpace(failure.DocumentationHint)
                ? "https://docs.aovias.io/deployment/troubleshooting"
                : failure.DocumentationHint
        };

    public static DeploymentOperatorErrorPresentation FromFailure(Aovias.Release.Contracts.Deployment.DeploymentFailure failure)
    {
        var mapped = Services.Deployment.DeploymentHttpErrorPresenter.FromFailure(failure);
        return new DeploymentOperatorErrorPresentation
        {
            Headline = "Deployment Failed",
            Category = mapped.Category,
            Phase = mapped.Phase,
            Component = mapped.Component,
            Reason = mapped.Reason,
            Details = mapped.Details,
            Recovery = mapped.Recovery,
            ServiceId = mapped.ServiceId,
            CorrelationId = mapped.CorrelationId,
            DeploymentId = mapped.DeploymentId
        };
    }

    private static string BuildSignalRDetails(DeploymentOperatorErrorDto failure)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(failure.ExceptionType))
        {
            parts.Add($"Exception: {failure.ExceptionType}");
        }

        if (!string.IsNullOrWhiteSpace(failure.InnerExceptionMessage))
        {
            parts.Add($"Inner: {failure.InnerExceptionMessage}");
        }

        if (!string.IsNullOrWhiteSpace(failure.Operation))
        {
            parts.Add($"Operation: {failure.Operation}");
        }

        return parts.Count == 0 ? failure.Reason : string.Join("; ", parts);
    }
}

/// <summary>
/// Maps workflow and platform events to session state (Sprint 12.1 D17).
/// </summary>
public static class DeploymentSessionStateMapper
{
    public static DeploymentSessionState FromWorkflowEvent(string eventType, string? stepName)
    {
        if (eventType is "WorkflowCompleted")
        {
            return DeploymentSessionState.Completed;
        }

        if (eventType is "WorkflowFailed")
        {
            return DeploymentSessionState.Failed;
        }

        if (eventType is not "StepStarted" and not "StepCompleted")
        {
            return DeploymentSessionState.Deploying;
        }

        var step = stepName ?? string.Empty;
        if (step.Contains("Upload", StringComparison.OrdinalIgnoreCase)
            || step.Contains("Extraction", StringComparison.OrdinalIgnoreCase))
        {
            return DeploymentSessionState.Uploading;
        }

        if (step.Contains("Provision", StringComparison.OrdinalIgnoreCase)
            || step.Contains("Stage", StringComparison.OrdinalIgnoreCase)
            || step.Contains("Folders", StringComparison.OrdinalIgnoreCase))
        {
            return DeploymentSessionState.Provisioning;
        }

        if (step.Contains("Health", StringComparison.OrdinalIgnoreCase)
            || step.Contains("Smoke", StringComparison.OrdinalIgnoreCase))
        {
            return DeploymentSessionState.HealthChecking;
        }

        if (step.Contains("Acceptance", StringComparison.OrdinalIgnoreCase)
            || step.Contains("Certification", StringComparison.OrdinalIgnoreCase)
            || step.Contains("Audit", StringComparison.OrdinalIgnoreCase))
        {
            return DeploymentSessionState.Certifying;
        }

        if (step.Contains("Docker", StringComparison.OrdinalIgnoreCase)
            || step.Contains("Compose", StringComparison.OrdinalIgnoreCase)
            || step.Contains("Nginx", StringComparison.OrdinalIgnoreCase)
            || step.Contains("Activate", StringComparison.OrdinalIgnoreCase))
        {
            return DeploymentSessionState.Deploying;
        }

        return DeploymentSessionState.Deploying;
    }

    public static DeploymentSessionState FromPlatformPhase(DeploymentPhaseKind phase, string status)
    {
        if (status.Equals("Failed", StringComparison.OrdinalIgnoreCase))
        {
            return DeploymentSessionState.Failed;
        }

        return phase switch
        {
            DeploymentPhaseKind.Upload or DeploymentPhaseKind.Extract => DeploymentSessionState.Uploading,
            DeploymentPhaseKind.Provision => DeploymentSessionState.Provisioning,
            DeploymentPhaseKind.Compose or DeploymentPhaseKind.DockerLoad => DeploymentSessionState.Deploying,
            DeploymentPhaseKind.Health or DeploymentPhaseKind.Smoke => DeploymentSessionState.HealthChecking,
            DeploymentPhaseKind.Certification => DeploymentSessionState.Certifying,
            DeploymentPhaseKind.Package => DeploymentSessionState.Analyzing,
            _ when status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                || status.Equals("Passed", StringComparison.OrdinalIgnoreCase) => DeploymentSessionState.Completed,
            _ => DeploymentSessionState.Deploying
        };
    }
}
