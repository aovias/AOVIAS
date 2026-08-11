namespace ComplianceService.Application.DTOs;

public sealed class AIContextBundleRequestDto
{
    public Guid HospitalId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? FrameworkId { get; set; }
    public Guid? ObjectiveElementId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? EvidenceId { get; set; }

    /// <summary>Page/module that opened the assistant (Dcc, ComplianceMaturity, AnalyticsHub, Journey, General).</summary>
    public string? PageContext { get; set; }

    /// <summary>Focused entity type (Deficiency, Capa, AnalyticsKpi, StageUnlock, etc.).</summary>
    public string? EntityType { get; set; }

    /// <summary>Focused entity identifier.</summary>
    public string? EntityId { get; set; }

    /// <summary>Short rolling summary of the current conversation session.</summary>
    public string? ConversationSummary { get; set; }

    /// <summary>Recently referenced entity refs (type:id) for session carry-forward.</summary>
    public List<string> RecentEntityRefs { get; set; } = new();
}

public sealed class AIContextSourceDto
{
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public decimal RelevanceScore { get; set; }
}

public sealed class AIContextBundleDto
{
    public string ContextVersionId { get; set; } = string.Empty;
    public Dictionary<string, string> Facts { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<AIContextSourceDto> Sources { get; set; } = new();
}

public sealed class AIContextVersionValidationRequestDto
{
    public Guid HospitalId { get; set; }
    public Guid? ObjectiveElementId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? EvidenceId { get; set; }
    public string ContextVersionId { get; set; } = string.Empty;
}

public sealed class AIContextVersionValidationResponseDto
{
    public bool IsCurrent { get; set; }
    public string CurrentContextVersionId { get; set; } = string.Empty;
}

public sealed class AICommandPreconditionRequestDto
{
    public Guid HospitalId { get; set; }
    public Guid? ObjectiveElementId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? EvidenceId { get; set; }
    public string ExpectedContextVersionId { get; set; } = string.Empty;
    public string? IdempotencyToken { get; set; }
    public bool ApprovalAttested { get; set; }
}

public sealed class AICommandPreconditionResponseDto
{
    public bool IsAllowed { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string CurrentContextVersionId { get; set; } = string.Empty;
}
