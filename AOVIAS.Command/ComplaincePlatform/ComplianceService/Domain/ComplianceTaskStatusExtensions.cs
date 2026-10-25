using ComplianceService.Domain.Enums;

namespace ComplianceService.Domain;

public static class ComplianceTaskStatusExtensions
{
    /// <summary>Strings accepted by legacy scoring for “done” work.</summary>
    public static bool IsCompletedForScoring(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        return status.Equals(nameof(ComplianceTaskStatus.Completed), StringComparison.OrdinalIgnoreCase)
               || status.Equals("Done", StringComparison.OrdinalIgnoreCase)
               || status.Equals("Closed", StringComparison.OrdinalIgnoreCase)
               || status.Equals("Verified", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsCompleted(this ComplianceTaskStatus status) => status == ComplianceTaskStatus.Completed;

    /// <summary>Maps legacy DB string values to enum when reading is not yet enum-backed.</summary>
    public static ComplianceTaskStatus ParseOrPending(string? legacy)
    {
        if (string.IsNullOrWhiteSpace(legacy))
        {
            return ComplianceTaskStatus.Pending;
        }

        if (legacy.Equals(nameof(ComplianceTaskStatus.Completed), StringComparison.OrdinalIgnoreCase)
            || legacy.Equals("Done", StringComparison.OrdinalIgnoreCase)
            || legacy.Equals("Closed", StringComparison.OrdinalIgnoreCase)
            || legacy.Equals("Verified", StringComparison.OrdinalIgnoreCase))
        {
            return ComplianceTaskStatus.Completed;
        }

        if (Enum.TryParse<ComplianceTaskStatus>(legacy, true, out var parsed))
        {
            return parsed;
        }

        return ComplianceTaskStatus.Pending;
    }

    public static string ToScoringStatusString(this ComplianceTaskStatus status) => status.ToString();
}
