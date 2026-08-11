using ComplianceService.Domain;

namespace ComplianceService.Application.DTOs
{
	public class ObjectiveElementDto
	{
		public Guid Id { get; set; }

		/// <summary>Parent standard (journey screen / wizard).</summary>
		public Guid? StandardId { get; set; }

		public string Code { get; set; } = string.Empty;
		public string Title { get; set; } = string.Empty;
		public string Description { get; set; } = string.Empty;
		public ObjectiveElementClassification Classification { get; set; }
		public bool IsMandatory { get; set; }
		public bool IsScorable { get; set; }
		public bool IsApplicable { get; set; }
		public bool IsDocumentationRequired { get; set; }
		public decimal Weight { get; set; }
		public string? Tags { get; set; }
		public string? Department { get; set; }
		public string? ResponsibleRole { get; set; }
		public string? Frequency { get; set; }
		public string? EvidenceCategory { get; set; }

		// Journey workspace — optional for backward-compatible reads
		public int? Score { get; set; }
		public string? ScoreLabel { get; set; }
		public bool IsNa { get; set; }
		public string? NaJustification { get; set; }
		public bool IsCoreCompliant { get; set; }
		public IReadOnlyList<OeObservationDto> ObservationList { get; set; } = Array.Empty<OeObservationDto>();
		public bool EvidenceVerifiedFlag { get; set; }
		public string? EvidenceStatus { get; set; }
		public string? AuditRemarks { get; set; }
		public string? ScoreRationale { get; set; }
		public string? AuditStatus { get; set; }
		public IReadOnlyList<OeActionPlanDto> ActionPlans { get; set; } = Array.Empty<OeActionPlanDto>();
		public IReadOnlyList<OeEvidenceItemDto> Evidence { get; set; } = Array.Empty<OeEvidenceItemDto>();
		public IReadOnlyList<OeHistoryEventDto> History { get; set; } = Array.Empty<OeHistoryEventDto>();

		// Phase 48 / journey screen spec
		public string? JourneyWorkflowStatus { get; set; }
		public bool? ChangedSinceLastAudit { get; set; }
		public string? ExcellenceNote { get; set; }
		public int? Version { get; set; }
		public DateTime? LastUpdatedAt { get; set; }
		public DateTime? DueAt { get; set; }
		public DateTime? NextReviewAt { get; set; }
		public string? AssigneeDisplayName { get; set; }
		public string? DepartmentLabel { get; set; }

		/// <summary>Obligation compliance state — populated when obligation data is available (Phase 4).</summary>
		public ObligationStateDto? ObligationState { get; set; }
	}
}

