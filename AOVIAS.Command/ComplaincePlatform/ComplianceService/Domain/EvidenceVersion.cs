using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
	public class EvidenceVersion : BaseEntity
	{
		public Guid EvidenceId { get; set; }
		public Evidence Evidence { get; set; }

		public int VersionNumber { get; set; }
		public string FileUrl { get; set; } = string.Empty;
		public string Hash { get; set; } = string.Empty;
		public Guid CreatedBy { get; set; }
		public DateTime CreatedAt { get; set; }
		public string? Notes { get; set; }
	}
}

