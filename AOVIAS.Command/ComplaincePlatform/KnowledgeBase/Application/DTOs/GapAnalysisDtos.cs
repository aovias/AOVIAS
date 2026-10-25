namespace KnowledgeBase.Application.DTOs;

public enum GapCategory
{
    Critical,
    Major,
    Minor,
    Compliant
}

public sealed record GenerateGapAnalysisCommand(Guid AssessmentId);

public sealed record GetGapAnalysisByAssessmentQuery(Guid AssessmentId);

public sealed record GapAnalysisItemDto(
    Guid NodeId,
    string NodeCode,
    string NodeTitle,
    Guid? ChapterId,
    Guid? StandardId,
    int Score,
    string ComplianceStatus,
    string Classification,
    GapCategory Category,
    string Recommendation);

public sealed record GapAnalysisReportDto(
    Guid Id,
    Guid AssessmentId,
    Guid FrameworkId,
    Guid FacilityId,
    DateTime GeneratedAt,
    decimal OverallPercent,
    bool MeetsOverallPercent,
    bool CoreRulePassed,
    bool ChapterAveragesPassed,
    bool StandardAveragesPassed,
    bool CommitmentLevelPassed,
    int CriticalCount,
    int MajorCount,
    int MinorCount,
    int CompliantCount,
    AssessmentSummaryResponseDto Summary,
    IReadOnlyList<GapAnalysisItemDto> Items);
