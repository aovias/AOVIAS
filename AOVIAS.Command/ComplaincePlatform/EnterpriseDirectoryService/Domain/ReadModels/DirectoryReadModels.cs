namespace EnterpriseDirectoryService.Domain.ReadModels;

public class DirectoryIndex
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid? EmploymentId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? PreferredName { get; set; }
    public string? PlatformId { get; set; }
    public string? EmployeeNumber { get; set; }
    public string? ProfessionalNumber { get; set; }
    public string? EmailNormalized { get; set; }
    public string? PhoneNormalized { get; set; }
    public string? DepartmentName { get; set; }
    public string? HospitalName { get; set; }
    public string? Designation { get; set; }
    public string? PositionTitle { get; set; }
    public string? EmploymentStatus { get; set; }
    public string? EmploymentType { get; set; }
    public string? ProfessionalCategory { get; set; }
    public string? ManagerName { get; set; }
    public string SearchBlob { get; set; } = string.Empty;
    public string? PhoneticKey { get; set; }
    public string SkillsJson { get; set; } = "[]";
    public string CertificationsJson { get; set; } = "[]";
    public string CommitteesJson { get; set; } = "[]";
    public string ResponsibilitiesJson { get; set; } = "[]";
    public string TagsJson { get; set; } = "[]";
    public string? VisibilityFlags { get; set; }
    public DateTime LastIndexedUtc { get; set; }
    public bool IsActive { get; set; } = true;
}

public class DirectoryFacet
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string FacetType { get; set; } = string.Empty;
    public string FacetValue { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime AsOfUtc { get; set; }
}

public class DirectorySnapshot
{
    public Guid Id { get; set; }
    public int Version { get; set; }
    public int RowCount { get; set; }
    public DateTime AsOfUtc { get; set; }
    public string? CorrelationId { get; set; }
    public long DurationMs { get; set; }
}

public class DirectoryRanking
{
    public Guid Id { get; set; }
    public string RulesJson { get; set; } = "{}";
    public bool IsActive { get; set; } = true;
}

public class SavedSearch
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string QueryJson { get; set; } = "{}";
    public string Visibility { get; set; } = "Private";
    public bool IsScheduled { get; set; }
    public bool IsAiGenerated { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class SearchHistory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string SearchTerms { get; set; } = string.Empty;
    public string? FiltersJson { get; set; }
    public int ResultCount { get; set; }
    public long ExecutionMs { get; set; }
    public DateTime SearchedAtUtc { get; set; }
}

public class SearchPreference
{
    public Guid UserId { get; set; }
    public Guid? DefaultOrganizationId { get; set; }
    public string? PinnedResultsJson { get; set; }
    public bool NaturalLanguageEnabled { get; set; } = true;
    public string? PreferencesJson { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class DirectorySuggestion
{
    public Guid Id { get; set; }
    public string CacheKey { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "[]";
    public DateTime ExpiresAtUtc { get; set; }
}

public class DirectorySynonym
{
    public Guid Id { get; set; }
    public string Token { get; set; } = string.Empty;
    public string Synonym { get; set; } = string.Empty;
}

public class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid AggregateId { get; set; }
    public DateTime OccurredOn { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedOn { get; set; }
    public DateTime? RelayProcessedOn { get; set; }
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
