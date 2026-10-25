namespace ComplianceService.Domain
{
    public record ChapterSnapshot(
        Guid ChapterId,
        decimal Weight
    );
}
