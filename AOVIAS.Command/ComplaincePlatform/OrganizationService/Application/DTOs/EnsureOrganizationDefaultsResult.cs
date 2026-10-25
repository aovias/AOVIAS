namespace OrganizationService.Application.DTOs;

public sealed record EnsureOrganizationDefaultsResult(
    Guid OrganizationId,
    bool SettingsCreated,
    bool AlreadyHadSettings);
