using ComplianceService.Application.Contracts;
using ComplianceService.Application.Journey;
using ComplianceService.Infrastructure.Persistence;

namespace ComplianceService.Application.Services;

public sealed class EvidenceBinderService : IEvidenceBinderService
{
    private readonly ComplianceDbContext _db;
    private readonly IEvidenceBinderQueryService _binderQuery;
    private readonly IEvidenceRegistrationService _evidenceRegistration;

    public EvidenceBinderService(
        ComplianceDbContext db,
        IEvidenceBinderQueryService binderQuery,
        IEvidenceRegistrationService evidenceRegistration)
    {
        _db = db;
        _binderQuery = binderQuery;
        _evidenceRegistration = evidenceRegistration;
    }

    public async Task<Guid> RegisterEvidenceFromUploadAsync(
        Guid hospitalId,
        Guid frameworkId,
        Guid objectiveElementId,
        Guid tenantId,
        Guid userId,
        Stream fileStream,
        string fileName,
        string? contentType,
        string evidenceType,
        string? displayName,
        string? title,
        string? metadata,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fileStream);

        var fwId = await ComplianceJourneySnapshotResolver.ResolveFrameworkOrJourneyIdAsync(
            _db,
            hospitalId,
            frameworkId,
            cancellationToken);

        if (!await _binderQuery.IsObjectiveInBinderScopeAsync(fwId, objectiveElementId, cancellationToken))
        {
            throw new InvalidOperationException("Objective element is not in scope for this framework.");
        }

        var journey = await ComplianceJourneySnapshotResolver.GetActiveJourneyForSnapshotAsync(
            _db,
            hospitalId,
            fwId,
            cancellationToken) ?? throw new InvalidOperationException("No active compliance journey for this hospital and framework.");

        var result = await _evidenceRegistration.RegisterFromUploadAsync(
            new EvidenceRegistrationUploadRequest
            {
                Context = EvidenceRegistrationContext.ForJourneyObjectiveElement(
                    journey.Id,
                    objectiveElementId,
                    tenantId,
                    userId),
                FileStream = fileStream,
                FileName = fileName,
                ContentType = contentType,
                EvidenceType = evidenceType,
                DisplayName = displayName,
                Title = title,
                Metadata = metadata
            },
            cancellationToken);

        return result.EvidenceId;
    }
}
