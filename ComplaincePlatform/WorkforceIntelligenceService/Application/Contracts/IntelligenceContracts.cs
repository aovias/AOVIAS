namespace WorkforceIntelligenceService.Application.Contracts;

public record IntelligenceScope(Guid OrganizationId, Guid? HospitalId = null, Guid? DepartmentId = null, DateTime? AsOf = null);

public class ExecutiveDashboardDto
{
    public Guid OrganizationId { get; set; }
    public DateTime AsOfUtc { get; set; }
    public int Headcount { get; set; }
    public int OpenVacancies { get; set; }
    public decimal TrainingCompliancePercent { get; set; }
    public int CredentialRiskCount { get; set; }
    public int CriticalSkillGaps { get; set; }
    public int CommitteeActionBacklog { get; set; }
    public decimal AvailabilityCoveragePercent { get; set; }
    public decimal? GrowthRate { get; set; }
    public decimal? AttritionRate { get; set; }
    public decimal? RetentionRate { get; set; }
    public Dictionary<string, int> ByDepartment { get; set; } = new();
    public Dictionary<string, int> ByProfession { get; set; } = new();
    public Dictionary<string, int> ByEmploymentType { get; set; } = new();
}

public class HeadcountAnalyticsDto
{
    public Guid OrganizationId { get; set; }
    public DateTime AsOfUtc { get; set; }
    public int TotalHeadcount { get; set; }
    public decimal TotalFte { get; set; }
    public Dictionary<string, int> ByHospital { get; set; } = new();
    public Dictionary<string, int> ByDepartment { get; set; } = new();
    public Dictionary<string, int> ByProfession { get; set; } = new();
    public Dictionary<string, int> ByEmploymentType { get; set; } = new();
    public Dictionary<string, int> AgeDistribution { get; set; } = new();
    public Dictionary<string, int> ExperienceDistribution { get; set; } = new();
    public Dictionary<string, int> QualificationDistribution { get; set; } = new();
    public decimal? GrowthRate { get; set; }
    public decimal? AttritionRate { get; set; }
    public decimal? RetentionRate { get; set; }
    public IReadOnlyList<HeadcountTrendPointDto> Trend { get; set; } = Array.Empty<HeadcountTrendPointDto>();
}

public class HeadcountTrendPointDto
{
    public DateTime AsOfUtc { get; set; }
    public int Headcount { get; set; }
}

public class SkillMatrixDto
{
    public Guid OrganizationId { get; set; }
    public DateTime AsOfUtc { get; set; }
    public IReadOnlyList<SkillMatrixRowDto> Rows { get; set; } = Array.Empty<SkillMatrixRowDto>();
    public IReadOnlyList<SkillGapSummaryDto> CriticalGaps { get; set; } = Array.Empty<SkillGapSummaryDto>();
    public decimal CoveragePercent { get; set; }
}

public class SkillMatrixRowDto
{
    public string SkillName { get; set; } = string.Empty;
    public string CurrentLevel { get; set; } = string.Empty;
    public string? TargetLevel { get; set; }
    public string? Gap { get; set; }
    public Guid? DepartmentId { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
}

public class SkillGapSummaryDto
{
    public string SkillName { get; set; } = string.Empty;
    public int GapCount { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
}

public class CompetencyMatrixDto
{
    public Guid OrganizationId { get; set; }
    public DateTime AsOfUtc { get; set; }
    public IReadOnlyList<CompetencyMatrixRowDto> Rows { get; set; } = Array.Empty<CompetencyMatrixRowDto>();
    public IReadOnlyList<CompetencyTrendDto> Trends { get; set; } = Array.Empty<CompetencyTrendDto>();
}

public class CompetencyMatrixRowDto
{
    public string CompetencyName { get; set; } = string.Empty;
    public string CurrentLevel { get; set; } = string.Empty;
    public string? TargetLevel { get; set; }
    public string? Gap { get; set; }
    public string? ProfessionalCategory { get; set; }
    public Guid? DepartmentId { get; set; }
    public string PersonName { get; set; } = string.Empty;
}

public class CompetencyTrendDto
{
    public string CompetencyName { get; set; } = string.Empty;
    public int Improving { get; set; }
    public int Declining { get; set; }
    public int Stable { get; set; }
}

public class TrainingMatrixDto
{
    public Guid OrganizationId { get; set; }
    public DateTime AsOfUtc { get; set; }
    public decimal OrganizationCompliancePercent { get; set; }
    public IReadOnlyList<TrainingScopeComplianceDto> ByScope { get; set; } = Array.Empty<TrainingScopeComplianceDto>();
    public IReadOnlyList<TrainingMatrixRowDto> Rows { get; set; } = Array.Empty<TrainingMatrixRowDto>();
}

public class TrainingScopeComplianceDto
{
    public string ScopeType { get; set; } = string.Empty;
    public string ScopeName { get; set; } = string.Empty;
    public decimal CompliancePercent { get; set; }
}

public class TrainingMatrixRowDto
{
    public string CourseName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
}

public class LicenseDashboardDto
{
    public Guid OrganizationId { get; set; }
    public DateTime AsOfUtc { get; set; }
    public int Active { get; set; }
    public int Expiring { get; set; }
    public int Expired { get; set; }
    public int RenewalRequired { get; set; }
    public IReadOnlyList<CredentialRowDto> ByDepartment { get; set; } = Array.Empty<CredentialRowDto>();
    public IReadOnlyList<CredentialRowDto> Items { get; set; } = Array.Empty<CredentialRowDto>();
}

public class CertificationDashboardDto
{
    public Guid OrganizationId { get; set; }
    public DateTime AsOfUtc { get; set; }
    public int Active { get; set; }
    public int Expiring { get; set; }
    public int Expired { get; set; }
    public int RenewalRequired { get; set; }
    public IReadOnlyList<CredentialRowDto> ByProfessionalCategory { get; set; } = Array.Empty<CredentialRowDto>();
    public IReadOnlyList<CredentialRowDto> Items { get; set; } = Array.Empty<CredentialRowDto>();
}

public class CredentialRowDto
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PersonName { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public string? GroupKey { get; set; }
}

public class ForecastDto
{
    public Guid OrganizationId { get; set; }
    public string ForecastType { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public IReadOnlyList<ForecastDataPointDto> DataPoints { get; set; } = Array.Empty<ForecastDataPointDto>();
    public string ParametersSummary { get; set; } = string.Empty;
}

public class ForecastDataPointDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime? TargetDate { get; set; }
    public string? Category { get; set; }
}

public class RecommendationListDto
{
    public Guid OrganizationId { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public IReadOnlyList<RecommendationItemDto> Items { get; set; } = Array.Empty<RecommendationItemDto>();
}

public class RecommendationItemDto
{
    public Guid Id { get; set; }
    public string RecommendationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public IReadOnlyList<SourceReferenceDto> SourceReferences { get; set; } = Array.Empty<SourceReferenceDto>();
    public decimal Confidence { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string? ModelVersion { get; set; }
}

public class SourceReferenceDto
{
    public string Service { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
}

public class WorkforceSearchResultDto
{
    public int TotalCount { get; set; }
    public IReadOnlyList<WorkforceSearchHitDto> Hits { get; set; } = Array.Empty<WorkforceSearchHitDto>();
}

public class WorkforceSearchHitDto
{
    public Guid PersonId { get; set; }
    public Guid EmploymentId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public string? ProfessionalCategory { get; set; }
    public IReadOnlyList<string> MatchedFields { get; set; } = Array.Empty<string>();
}

public class OrganizationAnalyticsDto
{
    public Guid OrganizationId { get; set; }
    public DateTime AsOfUtc { get; set; }
    public Dictionary<string, int> DepartmentSizes { get; set; } = new();
    public int ReportingDepth { get; set; }
    public decimal SpanOfControlAvg { get; set; }
    public int CrossFunctionalAssignments { get; set; }
    public int Vacancies { get; set; }
    public int TransfersInPeriod { get; set; }
    public int PromotionsInPeriod { get; set; }
    public Dictionary<string, int> LeadershipDistribution { get; set; } = new();
}

public class ResponsibilityMatrixDto
{
    public Guid OrganizationId { get; set; }
    public DateTime AsOfUtc { get; set; }
    public int Covered { get; set; }
    public int Gaps { get; set; }
    public int Duplicates { get; set; }
    public IReadOnlyList<ResponsibilityRowDto> Rows { get; set; } = Array.Empty<ResponsibilityRowDto>();
}

public class ResponsibilityRowDto
{
    public string ResponsibilityName { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    public Guid? ReviewerId { get; set; }
    public Guid? ApproverId { get; set; }
    public Guid? CommitteeId { get; set; }
    public string CoverageStatus { get; set; } = string.Empty;
}

public class CommitteeAnalyticsDto
{
    public Guid OrganizationId { get; set; }
    public DateTime AsOfUtc { get; set; }
    public IReadOnlyList<CommitteeAnalyticsRowDto> Committees { get; set; } = Array.Empty<CommitteeAnalyticsRowDto>();
}

public class CommitteeAnalyticsRowDto
{
    public Guid CommitteeId { get; set; }
    public string CommitteeName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public decimal AttendanceRate { get; set; }
    public decimal MeetingFrequencyPerMonth { get; set; }
    public decimal DecisionCompletionRate { get; set; }
    public decimal ActionClosureRate { get; set; }
}

public class AvailabilityAnalyticsDto
{
    public Guid OrganizationId { get; set; }
    public DateTime AsOfUtc { get; set; }
    public int Working { get; set; }
    public int OnLeave { get; set; }
    public int Vacation { get; set; }
    public int Unavailable { get; set; }
    public int InTraining { get; set; }
    public int InMeeting { get; set; }
    public decimal CoveragePercent { get; set; }
}

// Source service DTOs for HTTP hydration
public class SourceEmploymentDto
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid? MasterDepartmentId { get; set; }
    public string PersonSearchName { get; set; } = string.Empty;
    public string EmploymentStatus { get; set; } = string.Empty;
    public string? EmploymentType { get; set; }
    public decimal? Fte { get; set; }
    public Guid? ManagerEmploymentId { get; set; }
    public Guid? ProfessionalCategoryValueId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

public class SourceProfessionalProfileDto
{
    public Guid Id { get; set; }
    public Guid EmploymentId { get; set; }
    public Guid PersonId { get; set; }
    public Guid? OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public string PersonSearchName { get; set; } = string.Empty;
    public int? YearsOfExperience { get; set; }
    public List<SourceSkillDto> Skills { get; set; } = new();
    public List<SourceCompetencyDto> Competencies { get; set; } = new();
    public List<SourceTrainingDto> TrainingRecords { get; set; } = new();
    public List<SourceLicenseDto> Licenses { get; set; } = new();
    public List<SourceCertificationDto> Certifications { get; set; } = new();
    public List<SourceQualificationDto> Qualifications { get; set; } = new();
}

public class SourceSkillDto
{
    public string SkillName { get; set; } = string.Empty;
    public string? ProficiencyLevel { get; set; }
    public bool IsCritical { get; set; }
}

public class SourceCompetencyDto
{
    public string SkillName { get; set; } = string.Empty;
    public string? Level { get; set; }
    public string? TargetLevel { get; set; }
    public DateTime? AssessmentDate { get; set; }
}

public class SourceTrainingDto
{
    public string CourseName { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public DateTime? CompletionDate { get; set; }
    public DateTime? DueDate { get; set; }
}

public class SourceLicenseDto
{
    public Guid Id { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SourceCertificationDto
{
    public Guid Id { get; set; }
    public string CertificationName { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public string VerificationStatus { get; set; } = string.Empty;
}

public class SourceQualificationDto
{
    public string Title { get; set; } = string.Empty;
    public DateTime? CompletionDate { get; set; }
}

public class SourceGovernanceSummaryDto
{
    public int TotalResponsibilities { get; set; }
    public int AssignedResponsibilities { get; set; }
    public int TotalCommittees { get; set; }
    public int OpenActions { get; set; }
    public List<SourceResponsibilityDto> Responsibilities { get; set; } = new();
    public List<SourceCommitteeDto> Committees { get; set; } = new();
}

public class SourceResponsibilityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    public Guid? ReviewerId { get; set; }
    public Guid? ApproverId { get; set; }
    public Guid? CommitteeId { get; set; }
}

public class SourceCommitteeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public decimal AttendanceRate { get; set; }
    public decimal DecisionCompletionRate { get; set; }
    public decimal ActionClosureRate { get; set; }
}

public class SourceRenewalItemDto
{
    public Guid PersonId { get; set; }
    public Guid? EmploymentId { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public string CredentialType { get; set; } = string.Empty;
    public string CredentialName { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public int DaysUntilExpiry { get; set; }
}

public class IntelligenceEventEnvelope
{
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public DateTime OccurredOn { get; set; }
}
