using System.Text.Json.Serialization;

namespace Gateway.DTOs;

/// <summary>
/// Mirrors ComplianceService.Application.DTOs — same JSON names for proxy pass-through.
/// </summary>
public class OeWizardPersistedState
{
    public int StepIndex { get; set; }

    public bool UnderstandAck { get; set; }

    public bool ExecuteReady { get; set; }

    public bool EvidenceAck { get; set; }

    public bool EvalSubmitted { get; set; }

    public int EvalScore { get; set; }

    public string EvalTone { get; set; } = "compliant";

    public string? EvalJustification { get; set; }

    public bool NeedsImprove { get; set; }

    public bool ImproveReady { get; set; }

    public bool CompleteAck { get; set; }

    public bool[]? AuditChecklistChecked { get; set; }

    public string? SavedAt { get; set; }
}

public enum ObjectiveJourneyStatusDto
{
    [JsonStringEnumMemberName("draft")]
    Draft = 0,

    [JsonStringEnumMemberName("inProgress")]
    InProgress = 1,

    [JsonStringEnumMemberName("completed")]
    Completed = 2
}

public class ObjectiveJourneyStateDto
{
    public Guid? Id { get; set; }

    public Guid JourneyId { get; set; }

    public Guid ObjectiveElementId { get; set; }

    public Guid HospitalId { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ObjectiveJourneyStatusDto Status { get; set; }

    public DateTime LastUpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    public string RowVersion { get; set; } = string.Empty;

    public OeWizardPersistedState Payload { get; set; } = new();
}

public class SaveObjectiveJourneyDraftRequest
{
    public OeWizardPersistedState Payload { get; set; } = new();

    public string? RowVersion { get; set; }
}

public class CompleteObjectiveJourneyRequest
{
    public OeWizardPersistedState Payload { get; set; } = new();

    public string? RowVersion { get; set; }

    public Guid? ComplianceTaskId { get; set; }
}
