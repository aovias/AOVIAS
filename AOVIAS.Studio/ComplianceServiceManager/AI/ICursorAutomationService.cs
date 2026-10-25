using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.AI;

public interface ICursorAutomationService
{
    Task<IReadOnlyList<string>> ExecuteFixAsync(ServiceInfoModel service, DiagnosticBundle bundle, CancellationToken cancellationToken);
}
