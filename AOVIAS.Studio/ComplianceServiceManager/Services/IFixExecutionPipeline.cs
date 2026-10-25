using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Services;

public interface IFixExecutionPipeline
{
    Task<FixExecutionResult> ExecuteAsync(ServiceInfoModel service, DiagnosticBundle bundle, IReadOnlyList<string> modifiedFiles, string checkpointReference, AIFixOptions options, CancellationToken cancellationToken);
}
