using System.Text.Json.Serialization;

namespace EnterpriseDirectoryService.Application.Contracts;

public sealed class DirectoryQuery
{
    public string Text { get; set; } = string.Empty;
    public DirectoryQueryFilters Filters { get; set; } = new();
    public bool Fuzzy { get; set; }
    public bool Phonetic { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Sort { get; set; }
}

public sealed class DirectoryQueryFilters
{
    public Guid? OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? EmploymentStatus { get; set; }
    public string? EmploymentType { get; set; }
    public string? ProfessionalCategory { get; set; }
    public string? Certification { get; set; }
    public string? Skill { get; set; }
    public int? ExpiryWithinDays { get; set; }
}

public sealed class DirectorySearchResultDto
{
    public IReadOnlyList<DirectorySearchHitDto> Items { get; init; } = Array.Empty<DirectorySearchHitDto>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public long ExecutionMs { get; init; }
    public IReadOnlyDictionary<string, IReadOnlyList<FacetValueDto>> Facets { get; init; } = new Dictionary<string, IReadOnlyList<FacetValueDto>>();
}

public sealed class DirectorySearchHitDto
{
    public Guid PersonId { get; init; }
    public Guid? EmploymentId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? PreferredName { get; init; }
    public string? PlatformId { get; init; }
    public string? EmployeeNumber { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? DepartmentName { get; init; }
    public string? HospitalName { get; init; }
    public string? Designation { get; init; }
    public string? EmploymentStatus { get; init; }
    public string? ProfessionalCategory { get; init; }
    public double Score { get; init; }
    public IReadOnlyList<string> MatchedFields { get; init; } = Array.Empty<string>();
}

public sealed class FacetValueDto
{
    public string Value { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed class EnterpriseDirectoryProfileDto
{
    public Guid PersonId { get; init; }
    public Guid? EmploymentId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? PreferredName { get; init; }
    public string? PlatformId { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public IReadOnlyList<EmploymentProfileSectionDto> Employments { get; init; } = Array.Empty<EmploymentProfileSectionDto>();
    public ProfessionalProfileSectionDto? Professional { get; init; }
    public IReadOnlyList<GovernanceAssignmentDto> Responsibilities { get; init; } = Array.Empty<GovernanceAssignmentDto>();
    public IReadOnlyList<CommitteeMembershipDto> Committees { get; init; } = Array.Empty<CommitteeMembershipDto>();
}

public sealed class EmploymentProfileSectionDto
{
    public Guid EmploymentId { get; init; }
    public string? EmployeeNumber { get; init; }
    public string? DepartmentName { get; init; }
    public string? HospitalName { get; init; }
    public string? Designation { get; init; }
    public string? PositionTitle { get; init; }
    public string? EmploymentStatus { get; init; }
    public string? ManagerName { get; init; }
}

public sealed class ProfessionalProfileSectionDto
{
    public Guid ProfileId { get; init; }
    public string? ProfessionalNumber { get; init; }
    public string? Category { get; init; }
    public IReadOnlyList<string> Skills { get; init; } = Array.Empty<string>();
    public IReadOnlyList<CredentialSummaryDto> Licenses { get; init; } = Array.Empty<CredentialSummaryDto>();
    public IReadOnlyList<CredentialSummaryDto> Certifications { get; init; } = Array.Empty<CredentialSummaryDto>();
}

public sealed class CredentialSummaryDto
{
    public string Name { get; init; } = string.Empty;
    public string? Number { get; init; }
    public string? Status { get; init; }
    public DateTime? ExpiryDate { get; init; }
}

public sealed class GovernanceAssignmentDto
{
    public string ResponsibilityName { get; init; } = string.Empty;
    public string? Role { get; init; }
}

public sealed class CommitteeMembershipDto
{
    public string CommitteeName { get; init; } = string.Empty;
    public string? Role { get; init; }
}

public sealed class DirectorySuggestionDto
{
    public string Text { get; init; } = string.Empty;
    public string? Subtitle { get; init; }
    public Guid? PersonId { get; init; }
    public Guid? EmploymentId { get; init; }
}

public sealed class SavedSearchDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DirectoryQuery Query { get; init; } = new();
    public string Visibility { get; init; } = "Private";
    public bool IsScheduled { get; init; }
    public bool IsAiGenerated { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class CreateSavedSearchRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public DirectoryQuery Query { get; set; } = new();
    public string Visibility { get; set; } = "Private";
}

public sealed class SearchHistoryDto
{
    public Guid Id { get; init; }
    public string SearchTerms { get; init; } = string.Empty;
    public string? FiltersJson { get; init; }
    public int ResultCount { get; init; }
    public long ExecutionMs { get; init; }
    public DateTime SearchedAtUtc { get; init; }
}

public sealed class SearchPreferenceDto
{
    public Guid? DefaultOrganizationId { get; init; }
    public bool NaturalLanguageEnabled { get; init; } = true;
    public IReadOnlyList<string> PinnedResults { get; init; } = Array.Empty<string>();
}

public sealed class UpdateSearchPreferenceRequest
{
    public Guid? DefaultOrganizationId { get; set; }
    public bool NaturalLanguageEnabled { get; set; } = true;
    public IReadOnlyList<string>? PinnedResults { get; set; }
}

public sealed class NaturalLanguageSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public Guid OrganizationId { get; set; }
}

public sealed class NaturalLanguageSearchResultDto
{
    public DirectorySearchResultDto Results { get; init; } = new();
    public DirectoryQuery TranslatedQuery { get; init; } = new();
}

public sealed class PopularSearchTermDto
{
    public string Term { get; init; } = string.Empty;
    public int Count { get; init; }
}

// Source hydration DTOs
public sealed class SourcePersonDto
{
    public Guid Id { get; set; }
    public string? LegalName { get; set; }
    public string? PreferredName { get; set; }
    public string? PlatformId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public Guid? OrganizationId { get; set; }
}

public sealed class SourceEmploymentDto
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? HospitalId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? EmployeeNumber { get; set; }
    public string? DepartmentName { get; set; }
    public string? HospitalName { get; set; }
    public string? Designation { get; set; }
    public string? PositionTitle { get; set; }
    public string? EmploymentStatus { get; set; }
    public string? EmploymentType { get; set; }
    public string? ManagerName { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class SourceProfessionalProfileDto
{
    public Guid Id { get; set; }
    public Guid PersonId { get; set; }
    public Guid OrganizationId { get; set; }
    public string? ProfessionalNumber { get; set; }
    public string? Category { get; set; }
    public List<SourceSkillDto> Skills { get; set; } = [];
    public List<SourceCredentialDto> Licenses { get; set; } = [];
    public List<SourceCredentialDto> Certifications { get; set; } = [];
}

public sealed class SourceSkillDto
{
    public string SkillName { get; set; } = string.Empty;
    public string? ProficiencyLevel { get; set; }
}

public sealed class SourceCredentialDto
{
    public string Name { get; set; } = string.Empty;
    public string? Number { get; set; }
    public string? Status { get; set; }
    public DateTime? ExpiryDate { get; set; }
}

public sealed class SourceGovernanceAssigneeDto
{
    public string ResponsibilityName { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public sealed class SourceCommitteeMemberDto
{
    public string CommitteeName { get; set; } = string.Empty;
    public string? Role { get; set; }
}

public sealed class DirectoryEventEnvelope
{
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public DateTime OccurredOn { get; set; }
}

public sealed class ApiEnvelope<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public sealed class PagedDto<T>
{
    public List<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
}

public sealed class GovernedDirectoryContext
{
    public bool SearchAllowed { get; init; }
    public bool ContactReadable { get; init; }
    public bool CredentialReadable { get; init; }
    public RowLevelFilterScope RowLevelFilter { get; init; } = new();
}

public sealed class RowLevelFilterScope
{
    public Guid? OrganizationId { get; init; }
    public Guid? HospitalId { get; init; }
    public Guid? DepartmentId { get; init; }
}
