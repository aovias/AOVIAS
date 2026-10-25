using System.Text.Json;
using ComplianceService.Application.Contracts;
using ComplianceService.Domain;

namespace ComplianceService.Application.Services;

public class AuditLogService
{
    private readonly IAuditLogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(
        IAuditLogRepository repository,
        IUnitOfWork unitOfWork,
        ILogger<AuditLogService> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task LogEntityCreatedAsync(
        string entityType,
        Guid entityId,
        Guid performedBy,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        await LogAsync(entityType, entityId, "Created", performedBy, metadata, cancellationToken);
    }

    public async Task LogEntityUpdatedAsync(
        string entityType,
        Guid entityId,
        Guid performedBy,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        await LogAsync(entityType, entityId, "Updated", performedBy, metadata, cancellationToken);
    }

    public async Task LogEntityDeletedAsync(
        string entityType,
        Guid entityId,
        Guid performedBy,
        object? metadata = null,
        CancellationToken cancellationToken = default)
    {
        await LogAsync(entityType, entityId, "Deleted", performedBy, metadata, cancellationToken);
    }

    private async Task LogAsync(
        string entityType,
        Guid entityId,
        string action,
        Guid performedBy,
        object? metadata,
        CancellationToken cancellationToken)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            PerformedBy = performedBy,
            Timestamp = DateTime.UtcNow,
            MetadataJson = metadata == null
                ? "{}"
                : JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(log, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Audit log created: {Action} {EntityType} {EntityId} by {UserId}",
            action, entityType, entityId, performedBy);
    }
}

