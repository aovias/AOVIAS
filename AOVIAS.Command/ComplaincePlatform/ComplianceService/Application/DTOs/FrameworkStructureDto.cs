namespace ComplianceService.Application.DTOs
{
	public class FrameworkStructureDto
	{
		public Guid FrameworkId { get; set; }
		public string FrameworkName { get; set; } = string.Empty;
		public string Version { get; set; } = string.Empty;
		public IReadOnlyCollection<ChapterWithStandardsDto> Chapters { get; set; } = Array.Empty<ChapterWithStandardsDto>();
	}
}

