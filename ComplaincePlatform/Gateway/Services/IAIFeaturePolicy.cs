namespace Gateway.Services;

public interface IAIFeaturePolicy
{
    bool CanUseRead();
    bool CanUseAssist();
    bool CanUseAction();
    bool CanUseProactive();
    bool CanUseAutonomousWorkflows();
    bool CanUseEventDrivenAutonomy();
    bool CanUseAutoExecution();
}
