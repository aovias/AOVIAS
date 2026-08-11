using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.AI;

public interface IAIFixOrchestrator
{
    Task<FixExecutionResult> RunAsync(ServiceInfoModel service, AIFixOptions options, IProgress<string>? progress, CancellationToken cancellationToken);

    Task<IReadOnlyList<FixExecutionResult>> FixAllFailedAsync(IReadOnlyList<ServiceInfoModel> failedServices, AIFixOptions options, IProgress<string>? progress, CancellationToken cancellationToken);
}
