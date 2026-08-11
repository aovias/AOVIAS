using AuditService.Application.Contracts;
using AuditService.Domain;
using Shared.Infrastructure.DTO;
using Shared.Infrastructure.Services;

namespace AuditService.Application.Services;

public class AuditAppService
{
    private readonly IAuditRepository _repo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPlatformIdGenerator _idGen;
    private readonly IReasoningCapture _reasoningCapture;
    private readonly ILogger<AuditAppService> _logger;

    public AuditAppService(
        IAuditRepository repo,
        IUnitOfWork unitOfWork,
        IPlatformIdGenerator idGen,
        IReasoningCapture reasoningCapture,
        ILogger<AuditAppService> logger)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
        _idGen = idGen;
        _reasoningCapture = reasoningCapture;
        _logger = logger;
    }

    public async Task<AuditSession> GenerateAuditAsync(Guid organizationId, Guid complianceProgramId, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting audit session generation for OrganizationId={OrganizationId}, ComplianceProgramId={ComplianceProgramId}",
            organizationId, complianceProgramId);

        var sessionId = Guid.NewGuid();
        _logger.LogDebug("Generated session ID: {SessionId}", sessionId);

        var platformId = await _idGen.GenerateAsync("AUD", ct);
        _logger.LogInformation("Generated PlatformId={PlatformId} for audit session {SessionId}", platformId, sessionId);

        var session = new AuditSession
        {
            Id = sessionId,
            PlatformId = platformId,
            OrganizationId = organizationId,
            ComplianceProgramId = complianceProgramId,
            StartedAt = DateTime.UtcNow,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };

        // Create Explainability metadata describing why this audit was initiated and what compliance scope it covers
        // This explains the existing behavior: audit session is generated as part of compliance workflow
        var explainability = new ExplainabilityDto
        {
            Reasoning = $"This audit session was initiated as part of the compliance workflow to assist in evaluating adherence to regulatory requirements for organization {organizationId}. The audit scope covers the compliance program identified by ComplianceProgramId {complianceProgramId}, which includes the controls, tasks, and compliance requirements associated with that program. This audit session was automatically generated to facilitate compliance review and assessment activities. All audit findings, observations, and conclusions generated during this audit session require human review and validation to ensure accuracy, appropriateness, and compliance with applicable standards and regulations.",
            RulesReferenced = new List<string> { $"ComplianceProgram:{complianceProgramId}", $"Organization:{organizationId}" },
            ConfidenceScore = 0.3, // Conservative confidence score - audit requires human review and validation
            HumanReviewRequired = true,
            GeneratedBy = "AuditService"
        };

        // Attach explainability metadata using ReasoningCapture service
        _reasoningCapture.Attach(session, explainability);

        await _repo.AddAsync(session);
        _logger.LogDebug("Added audit session {SessionId} to repository (tracked, not yet persisted)", sessionId);

        var savedCount = await _unitOfWork.SaveChangesAsync(ct);
        _logger.LogInformation("Persisted audit session {SessionId} (PlatformId={PlatformId}) to database. Saved {SavedCount} entity/entities",
            sessionId, platformId, savedCount);

        return session;
    }

    public async Task<AuditSession?> GetAuditSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        _logger.LogDebug("Retrieving audit session: SessionId={SessionId}", sessionId);
        return await _repo.GetAsync(sessionId);
    }

    public async Task<List<AuditSession>> GetAuditSessionsByOrganizationAsync(Guid organizationId, CancellationToken ct = default)
    {
        _logger.LogDebug("Retrieving audit sessions for OrganizationId={OrganizationId}", organizationId);
        return await _repo.GetByOrganizationAsync(organizationId);
    }
}

