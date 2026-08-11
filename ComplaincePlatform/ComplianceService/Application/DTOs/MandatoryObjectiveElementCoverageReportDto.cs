namespace ComplianceService.Application.DTOs;

/// <summary>
/// Report of mandatory objective-element coverage for a compliance journey.
/// </summary>
public class MandatoryObjectiveElementCoverageReportDto
{
    public Guid ComplianceJourneyId { get; set; }
    public Guid FrameworkId { get; set; }
    public int TotalRequiredObjectiveElements { get; set; }
    public int CoveredObjectiveElements { get; set; }
    public int MissingObjectiveElementsCount => MissingObjectiveElements.Count;

    public List<MandatoryObjectiveElementDto> MissingObjectiveElements { get; set; } = new();
}

public class MandatoryObjectiveElementDto
{
    public Guid ObjectiveElementId { get; set; }
    public string ChapterCode { get; set; } = string.Empty;
    public string ElementCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    /// <summary>
    /// OE classification label for display.
    /// </summary>
    public string Classification { get; set; } = string.Empty;
}
