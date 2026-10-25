using System.Text.Json;
using EnterpriseDirectoryService.Application.Contracts;
using EnterpriseDirectoryService.Application.Directory.Indexing;
using EnterpriseDirectoryService.Application.Directory.Platform;
using EnterpriseDirectoryService.Domain.ReadModels;
using EnterpriseDirectoryService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseDirectoryService.Infrastructure.Services;

public sealed class DirectoryProjectionRegistry(
    IEnumerable<IDirectoryProjectionHandler> handlers,
    IDirectoryIndexProjector projector) : IDirectoryProjectionRegistry
{
    private readonly Dictionary<string, IDirectoryProjectionHandler> _handlers =
        handlers.GroupBy(h => h.EventType).ToDictionary(g => g.Key, g => g.First());

    public async Task ProcessAsync(DirectoryEventEnvelope envelope, CancellationToken ct = default)
    {
        if (_handlers.TryGetValue(envelope.Type, out var handler))
        {
            await handler.HandleAsync(envelope, ct);
            return;
        }

        switch (envelope.Type)
        {
            case "PersonUpdated":
                await projector.HandlePersonUpdatedAsync(envelope.AggregateId, envelope.Payload, ct);
                break;
            case "EmploymentCreated":
            case "EmploymentTransferred":
            case "EmploymentTerminated":
                await projector.HandleEmploymentEventAsync(envelope.Type, envelope.AggregateId, envelope.Payload, ct);
                break;
            case "ProfessionalProfileUpdated":
            case "QualificationAdded":
            case "TrainingCompleted":
            case "CertificationRenewed":
            case "LicenseRenewed":
            case "SkillUpdated":
                await projector.HandleProfessionalEventAsync(envelope.AggregateId, envelope.Payload, ct);
                break;
        }
    }
}

public sealed class DirectoryOutboxWriter(EnterpriseDirectoryDbContext db) : IDirectoryOutboxWriter
{
    public async Task EnqueueAsync(string type, Guid aggregateId, object payload, CancellationToken ct = default)
    {
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            AggregateId = aggregateId,
            OccurredOn = DateTime.UtcNow,
            Type = type,
            Payload = JsonSerializer.Serialize(payload)
        });
        await db.SaveChangesAsync(ct);
    }
}

public sealed class PersonUpdatedProjection(IDirectoryIndexProjector projector) : IDirectoryProjectionHandler
{
    public string EventType => "PersonUpdated";
    public Task HandleAsync(DirectoryEventEnvelope envelope, CancellationToken ct = default)
        => projector.HandlePersonUpdatedAsync(envelope.AggregateId, envelope.Payload, ct);
}

public sealed class EmploymentProjection(IDirectoryIndexProjector projector, string eventType) : IDirectoryProjectionHandler
{
    public string EventType => eventType;
    public Task HandleAsync(DirectoryEventEnvelope envelope, CancellationToken ct = default)
        => projector.HandleEmploymentEventAsync(eventType, envelope.AggregateId, envelope.Payload, ct);
}

public sealed class ProfessionalProjection(IDirectoryIndexProjector projector, string eventType) : IDirectoryProjectionHandler
{
    public string EventType => eventType;
    public Task HandleAsync(DirectoryEventEnvelope envelope, CancellationToken ct = default)
        => projector.HandleProfessionalEventAsync(envelope.AggregateId, envelope.Payload, ct);
}
