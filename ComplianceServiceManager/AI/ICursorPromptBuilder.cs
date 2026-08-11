using ComplianceServiceManager.Models;

namespace ComplianceServiceManager.AI;

public interface ICursorPromptBuilder
{
    string BuildPrompt(ServiceInfoModel service, DiagnosticBundle bundle);
}
