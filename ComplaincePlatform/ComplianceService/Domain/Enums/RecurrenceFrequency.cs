namespace ComplianceService.Domain.Enums;

/// <summary>How often a <see cref="RecurringTaskDefinition"/> materializes a new task.</summary>
public enum RecurrenceFrequency
{
    Daily = 0,
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Yearly = 4
}
