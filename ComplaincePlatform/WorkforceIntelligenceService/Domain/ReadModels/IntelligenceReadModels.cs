namespace WorkforceIntelligenceService.Domain.ReadModels;

public class WorkforceSnapshot
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public DateTime AsOfUtc { get; set; }
    public int Version { get; set; }
    public string? CorrelationId { get; set; }
    public int TotalHeadcount { get; set; }
    public decimal TotalFte { get; set; }
    public string ByDepartmentJson { get; set; } = "{}";
    public string ByProfessionJson { get; set; } = "{}";
    public string ByEmploymentTypeJson { get; set; } = "{}";
    public string ByHospitalJson { get; set; } = "{}";
    public string AgeDistributionJson { get; set; } = "{}";
    public string ExperienceDistributionJson { get; set; } = "{}";
    public string QualificationDistributionJson { get; set; } = "{}";
    public decimal? GrowthRate { get; set; }
    public decimal? AttritionRate { get; set; }
    public decimal? RetentionRate { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ExecutiveMetrics
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public DateTime AsOfUtc { get; set; }
    public int Headcount { get; set; }
    public int OpenVacancies { get; set; }
    public decimal TrainingCompliancePercent { get; set; }
    public int CredentialRiskCount { get; set; }
    public int CriticalSkillGaps { get; set; }
    public int CommitteeActionBacklog { get; set; }
    public decimal AvailabilityCoveragePercent { get; set; }
    public Guid? SnapshotId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class SkillMatrixEntry
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid PersonId { get; set; }
    public Guid EmploymentId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public string CurrentLevel { get; set; } = string.Empty;
    public string? TargetLevel { get; set; }
    public string? Gap { get; set; }
    public bool IsCritical { get; set; }
    public decimal? CoverageScore { get; set; }
    public DateTime AsOfUtc { get; set; }
}

public class CompetencyMatrixEntry
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid PersonId { get; set; }
    public Guid EmploymentId { get; set; }
    public string CompetencyName { get; set; } = string.Empty;
    public string CurrentLevel { get; set; } = string.Empty;
    public string? TargetLevel { get; set; }
    public string? Gap { get; set; }
    public string? ProfessionalCategory { get; set; }
    public DateTime? AssessmentDate { get; set; }
    public DateTime AsOfUtc { get; set; }
}

public class TrainingMatrixEntry
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid PersonId { get; set; }
    public Guid EmploymentId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public bool IsMandatory { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal CompliancePercent { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime AsOfUtc { get; set; }
}

public class CertificationMatrixEntry
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PersonId { get; set; }
    public Guid EmploymentId { get; set; }
    public string CertificationName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? ProfessionalCategory { get; set; }
    public DateTime AsOfUtc { get; set; }
}

public class LicenseMatrixEntry
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PersonId { get; set; }
    public Guid EmploymentId { get; set; }
    public string LicenseName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? ProfessionalCategory { get; set; }
    public DateTime AsOfUtc { get; set; }
}

public class OrganizationAnalyticsSnapshot
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string DepartmentSizeJson { get; set; } = "{}";
    public int ReportingDepth { get; set; }
    public string LeadershipDistributionJson { get; set; } = "{}";
    public decimal SpanOfControlAvg { get; set; }
    public int CrossFunctionalAssignments { get; set; }
    public int Vacancies { get; set; }
    public int TransfersInPeriod { get; set; }
    public int PromotionsInPeriod { get; set; }
    public DateTime AsOfUtc { get; set; }
}

public class ResponsibilityMatrixEntry
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ResponsibilityId { get; set; }
    public string ResponsibilityName { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    public Guid? ReviewerId { get; set; }
    public Guid? ApproverId { get; set; }
    public Guid? CommitteeId { get; set; }
    public string CoverageStatus { get; set; } = "Covered";
    public Guid? EscalationChainId { get; set; }
    public DateTime AsOfUtc { get; set; }
}

public class CommitteeAnalyticsSnapshot
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid CommitteeId { get; set; }
    public string CommitteeName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public decimal AttendanceRate { get; set; }
    public decimal MeetingFrequencyPerMonth { get; set; }
    public decimal DecisionCompletionRate { get; set; }
    public decimal ActionClosureRate { get; set; }
    public string MemberParticipationJson { get; set; } = "{}";
    public DateTime AsOfUtc { get; set; }
}

public class AvailabilityAnalyticsSnapshot
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public int WorkingCount { get; set; }
    public int OnLeaveCount { get; set; }
    public int VacationCount { get; set; }
    public int UnavailableCount { get; set; }
    public int InTrainingCount { get; set; }
    public int InMeetingCount { get; set; }
    public decimal CoveragePercent { get; set; }
    public DateTime AsOfUtc { get; set; }
}

public class ForecastSnapshot
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string ForecastType { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public string InputSnapshotIdsJson { get; set; } = "[]";
    public string ParametersJson { get; set; } = "{}";
    public string ResultsJson { get; set; } = "{}";
    public string? CorrelationId { get; set; }
}

public class WorkforceRecommendation
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string RecommendationType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string SourceReferencesJson { get; set; } = "[]";
    public decimal Confidence { get; set; }
    public string? ModelVersion { get; set; }
    public string Priority { get; set; } = "Medium";
    public DateTime GeneratedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AnalyticsCache
{
    public Guid Id { get; set; }
    public string CacheKey { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class RecommendationCache
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string PayloadJson { get; set; } = "[]";
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class WorkforceSearchIndex
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid EmploymentId { get; set; }
    public Guid OrganizationId { get; set; }
    public string SearchText { get; set; } = string.Empty;
    public string SkillsJson { get; set; } = "[]";
    public string CertificationsJson { get; set; } = "[]";
    public string? DepartmentName { get; set; }
    public string? ProfessionalCategory { get; set; }
    public string? LanguagesJson { get; set; }
    public int? YearsOfExperience { get; set; }
    public DateTime IndexedAtUtc { get; set; }
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public DateTime OccurredOn { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedOn { get; set; }
    public int RetryCount { get; set; }
    public string? Error { get; set; }
}

public class ProjectionCheckpoint
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime LastProcessedAtUtc { get; set; }
    public string? LastMessageId { get; set; }
}
