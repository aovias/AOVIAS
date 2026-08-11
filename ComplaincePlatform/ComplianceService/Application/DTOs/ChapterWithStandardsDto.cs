namespace ComplianceService.Application.DTOs
{
	public class ChapterWithStandardsDto
	{
		public Guid Id { get; set; }
		public string Code { get; set; } = string.Empty;
		public string Name { get; set; } = string.Empty;
		public int Sequence { get; set; }
		public decimal Weight { get; set; }
		public IReadOnlyCollection<ComplianceStandardDto> Standards { get; set; } = Array.Empty<ComplianceStandardDto>();

		public decimal? ComplianceScorePercent { get; set; }
		public string? Risk { get; set; }
		public string? CoreComplianceStatus { get; set; }
		public int? OpenIssuesCount { get; set; }
		public bool? PendingAudit { get; set; }
		public bool? CoreFailureFlag { get; set; }
		public decimal? AverageScore { get; set; }
	public int FailedCoreOeCount { get; set; }
	public decimal ObligationReadinessScore { get; set; }
}
}

