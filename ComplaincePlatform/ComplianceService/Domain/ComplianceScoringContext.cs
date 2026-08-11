namespace ComplianceService.Domain
{
    public record ComplianceScoringContext(
        Guid HospitalId,
        Guid FrameworkId,
        IReadOnlyCollection<ObjectiveElementWorkSnapshot> ObjectiveElements,
        IReadOnlyCollection<RiskSnapshot> Risks,
        IReadOnlyCollection<ChapterSnapshot> Chapters
    );
}
