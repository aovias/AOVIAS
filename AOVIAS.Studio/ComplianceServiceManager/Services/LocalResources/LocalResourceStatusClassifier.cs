using ComplianceServiceManager.Models.LocalResources;

namespace ComplianceServiceManager.Services.LocalResources;

/// <summary>
/// Classifies local host metric percentages into visualization-only status values.
/// Does not imply orchestration decisions.
/// </summary>
public static class LocalResourceStatusClassifier
{
    public static LocalResourceStatus Classify(double? percent, double warningThreshold, double criticalThreshold)
    {
        if (percent is null || double.IsNaN(percent.Value) || percent.Value < 0)
        {
            return LocalResourceStatus.Unknown;
        }

        var value = Math.Clamp(percent.Value, 0, 100);
        if (value >= criticalThreshold)
        {
            return LocalResourceStatus.Critical;
        }

        if (value >= warningThreshold)
        {
            return LocalResourceStatus.Warning;
        }

        return LocalResourceStatus.Normal;
    }

    public static double? ComputePercent(long used, long total)
    {
        if (total <= 0 || used < 0)
        {
            return null;
        }

        return Math.Clamp(used * 100.0 / total, 0, 100);
    }
}
