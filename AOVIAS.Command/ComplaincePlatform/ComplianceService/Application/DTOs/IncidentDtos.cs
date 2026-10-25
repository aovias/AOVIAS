namespace ComplianceService.Application.DTOs;

public class IncidentDashboardDto
{
    public int OpenIncidents { get; set; }
    public int CriticalIncidents { get; set; }
    public int NearMisses { get; set; }
    public int SentinelEvents { get; set; }
    public int InvestigationsInProgress { get; set; }
    public int RcaPending { get; set; }
    public int RepeatedIncidents { get; set; }
    public int DepartmentsAtRisk { get; set; }
    public decimal AverageClosureDays { get; set; }
    public int OpenCapas { get; set; }
    public decimal PatientSafetyScore { get; set; }
    public IReadOnlyList<IncidentCountByLabelDto> ByCategory { get; set; } = Array.Empty<IncidentCountByLabelDto>();
    public IReadOnlyList<IncidentCountByLabelDto> ByDepartment { get; set; } = Array.Empty<IncidentCountByLabelDto>();
    public IReadOnlyList<IncidentCountByLabelDto> BySeverity { get; set; } = Array.Empty<IncidentCountByLabelDto>();
    public IReadOnlyList<IncidentTrendPointDto> Trend { get; set; } = Array.Empty<IncidentTrendPointDto>();
}

public class IncidentCountByLabelDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class IncidentTrendPointDto
{
    public string Period { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class IncidentListItemDto
{
    public Guid Id { get; set; }
    public string IncidentNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string IncidentType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string Department { get; set; } = string.Empty;
    public string? ReporterName { get; set; }
    public bool IsAnonymous { get; set; }
    public bool PatientInvolved { get; set; }
    public DateTime IncidentDateTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string InvestigationStatus { get; set; } = string.Empty;
    public bool RcaRequired { get; set; }
    public string? OwnerName { get; set; }
    public Guid? FindingId { get; set; }
    public Guid? DeficiencyId { get; set; }
    public Guid? CapaId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime LastUpdated { get; set; }
    public int AttachmentCount { get; set; }
    public int TimelineCount { get; set; }
}

public class IncidentListDto
{
    public IReadOnlyList<IncidentListItemDto> Items { get; set; } = Array.Empty<IncidentListItemDto>();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class IncidentTimelineEntryDto
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ActorName { get; set; }
}

public class IncidentAttachmentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long? FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class IncidentDocumentationLinkDto
{
    public Guid DocumentId { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? CurrentVersion { get; set; }
    public decimal AcknowledgementPercent { get; set; }
}

public class IncidentDetailDto : IncidentListItemDto
{
    public string? StaffInvolvedJson { get; set; }
    public IReadOnlyList<IncidentTimelineEntryDto> Timeline { get; set; } = Array.Empty<IncidentTimelineEntryDto>();
    public IReadOnlyList<IncidentAttachmentDto> Attachments { get; set; } = Array.Empty<IncidentAttachmentDto>();
    public IReadOnlyList<IncidentDocumentationLinkDto> RelatedDocumentation { get; set; } = Array.Empty<IncidentDocumentationLinkDto>();
    public IncidentInvestigationDto? Investigation { get; set; }
    public IncidentRcaDto? RootCauseAnalysis { get; set; }
}

public class IncidentInvestigationDto
{
    public Guid Id { get; set; }
    public string? IncidentSummary { get; set; }
    public string? ContributingFactorsJson { get; set; }
    public string? PeopleFactorsJson { get; set; }
    public string? EnvironmentFactorsJson { get; set; }
    public string? EquipmentFactorsJson { get; set; }
    public string? ProcessFactorsJson { get; set; }
    public string? CommunicationFactorsJson { get; set; }
    public string? TimelineJson { get; set; }
    public string? Recommendations { get; set; }
    public string? ReviewersJson { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? ApprovedAt { get; set; }
}

public class IncidentRcaDto
{
    public Guid Id { get; set; }
    public string Method { get; set; } = string.Empty;

    /// <summary>Typed 5 Whys (preferred).</summary>
    public FiveWhysDto? FiveWhys { get; set; }

    /// <summary>Typed cause categories (preferred).</summary>
    public CauseCategoriesDto? CauseCategories { get; set; }

    /// <summary>Future-ready fishbone schema (preferred).</summary>
    public FishboneSchemaDto? Fishbone { get; set; }

    public string? RootCauseSummary { get; set; }
    public string? CorrectiveActionSummary { get; set; }
    public string? PreventiveActionSummary { get; set; }
    public string? RootCauseCategory { get; set; }

    /// <summary>Legacy persisted JSON — prefer <see cref="FiveWhys"/>.</summary>
    public string? FiveWhysJson { get; set; }

    /// <summary>Legacy persisted JSON — prefer <see cref="Fishbone"/>.</summary>
    public string? FishboneJson { get; set; }

    public string? HumanFactorsJson { get; set; }
    public string? ProcessFactorsJson { get; set; }
    public string? EnvironmentalFactorsJson { get; set; }
    public string? EquipmentFactorsJson { get; set; }
    public string? OrganizationFactorsJson { get; set; }
}

/// <summary>Structured 5 Whys RCA.</summary>
public sealed class FiveWhysDto
{
    public string? Why1 { get; set; }
    public string? Why2 { get; set; }
    public string? Why3 { get; set; }
    public string? Why4 { get; set; }
    public string? Why5 { get; set; }
    public string? FinalRootCause { get; set; }
}

/// <summary>Structured cause-category factors (Ishikawa limbs without full fishbone schema).</summary>
public sealed class CauseCategoriesDto
{
    public IList<string> Human { get; set; } = new List<string>();
    public IList<string> Process { get; set; } = new List<string>();
    public IList<string> Environmental { get; set; } = new List<string>();
    public IList<string> Equipment { get; set; } = new List<string>();
    public IList<string> Organization { get; set; } = new List<string>();
}

/// <summary>Future-ready fishbone (Ishikawa) schema — categories + causes under an effect.</summary>
public sealed class FishboneSchemaDto
{
    public string? Effect { get; set; }
    public IList<FishboneCategoryDto> Categories { get; set; } = new List<FishboneCategoryDto>();
}

public sealed class FishboneCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public IList<string> Causes { get; set; } = new List<string>();
}

public class CreateIncidentRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IncidentType { get; set; } = "AdverseEvent";
    public string Severity { get; set; } = "Moderate";
    public string Priority { get; set; } = "Routine";
    public string Category { get; set; } = "Other";
    public string? Location { get; set; }
    public string Department { get; set; } = "Clinical";
    public bool IsAnonymous { get; set; }
    public bool PatientInvolved { get; set; }
    public string? StaffInvolvedJson { get; set; }
    public DateTime? IncidentDateTime { get; set; }
    public bool RcaRequired { get; set; }
}

public class PatchIncidentRequestDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Severity { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public string? OwnerName { get; set; }
    public Guid? OwnerUserId { get; set; }
    public bool? RcaRequired { get; set; }
}

public class SaveInvestigationRequestDto
{
    public Guid IncidentId { get; set; }
    public string? IncidentSummary { get; set; }
    public string? ContributingFactorsJson { get; set; }
    public string? PeopleFactorsJson { get; set; }
    public string? EnvironmentFactorsJson { get; set; }
    public string? EquipmentFactorsJson { get; set; }
    public string? ProcessFactorsJson { get; set; }
    public string? CommunicationFactorsJson { get; set; }
    public string? TimelineJson { get; set; }
    public string? Recommendations { get; set; }
    public string? ReviewersJson { get; set; }
    public string? Status { get; set; }
}

public class SaveRcaRequestDto
{
    public Guid IncidentId { get; set; }
    public string Method { get; set; } = "FiveWhys";

    public FiveWhysDto? FiveWhys { get; set; }
    public CauseCategoriesDto? CauseCategories { get; set; }
    public FishboneSchemaDto? Fishbone { get; set; }

    public string? RootCauseSummary { get; set; }
    public string? CorrectiveActionSummary { get; set; }
    public string? PreventiveActionSummary { get; set; }
    public string? RootCauseCategory { get; set; }

    /// <summary>Legacy — used only when typed <see cref="FiveWhys"/> is omitted.</summary>
    public string? FiveWhysJson { get; set; }

    /// <summary>Legacy — used only when typed <see cref="Fishbone"/> is omitted.</summary>
    public string? FishboneJson { get; set; }

    public string? HumanFactorsJson { get; set; }
    public string? ProcessFactorsJson { get; set; }
    public string? EnvironmentalFactorsJson { get; set; }
    public string? EquipmentFactorsJson { get; set; }
    public string? OrganizationFactorsJson { get; set; }
}

public class PromoteIncidentFindingRequestDto
{
    public Guid IncidentId { get; set; }
    public string? Observation { get; set; }
    public string? Recommendation { get; set; }
}

public class PromoteIncidentDeficiencyRequestDto
{
    public Guid IncidentId { get; set; }
    public Guid? FindingId { get; set; }
}

public class PromoteIncidentResultDto
{
    public Guid IncidentId { get; set; }
    public Guid? FindingId { get; set; }
    public Guid? DeficiencyId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class CreateLessonLearnedRequestDto
{
    public Guid IncidentId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string? DepartmentsJson { get; set; }
    public string? RelatedSopsJson { get; set; }
    public string? TrainingNeeded { get; set; }
    public string? PreventiveActions { get; set; }
}

public class LessonLearnedDto
{
    public Guid Id { get; set; }
    public string LessonNumber { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public string? DepartmentsJson { get; set; }
    public string? RelatedSopsJson { get; set; }
    public string? TrainingNeeded { get; set; }
    public string? PreventiveActions { get; set; }
    public Guid SourceIncidentId { get; set; }
    public string? SourceIncidentNumber { get; set; }
    public DateTime PublishedAt { get; set; }
}

public class PatientFeedbackDto
{
    public Guid Id { get; set; }
    public string FeedbackNumber { get; set; } = string.Empty;
    public string FeedbackType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string? Department { get; set; }
    public Guid? LinkedIncidentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
}

public class CreatePatientFeedbackRequestDto
{
    public string FeedbackType { get; set; } = "Feedback";
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string? ContactInfo { get; set; }
    public string? Department { get; set; }
    public Guid? LinkedIncidentId { get; set; }
}

public class IncidentAnalyticsDto
{
    public IReadOnlyList<IncidentTrendPointDto> IncidentTrends { get; set; } = Array.Empty<IncidentTrendPointDto>();
    public IReadOnlyList<IncidentCountByLabelDto> SeverityDistribution { get; set; } = Array.Empty<IncidentCountByLabelDto>();
    public IReadOnlyList<IncidentCountByLabelDto> DepartmentHeatmap { get; set; } = Array.Empty<IncidentCountByLabelDto>();
    public IReadOnlyList<IncidentCountByLabelDto> RootCauseTrends { get; set; } = Array.Empty<IncidentCountByLabelDto>();
    public IReadOnlyList<IncidentTrendPointDto> NearMissTrends { get; set; } = Array.Empty<IncidentTrendPointDto>();
    public int RepeatIncidents { get; set; }
    public decimal AverageResolutionDays { get; set; }
    public int LessonsLearnedCount { get; set; }
    public decimal CapaConversionRate { get; set; }
    public decimal DocumentationRevisionRate { get; set; }
}
