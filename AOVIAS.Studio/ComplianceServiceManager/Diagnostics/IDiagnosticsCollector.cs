using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.Diagnostics;

public interface IDiagnosticsCollector
{
    Task<DiagnosticBundle> CollectAsync(ServiceInfoModel service, CancellationToken cancellationToken);
}
