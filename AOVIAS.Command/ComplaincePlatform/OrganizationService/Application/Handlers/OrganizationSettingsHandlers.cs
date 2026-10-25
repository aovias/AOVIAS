using MediatR;
using Microsoft.EntityFrameworkCore;
using OrganizationService.Application.Commands;
using OrganizationService.Application.Contracts;
using OrganizationService.Application.DTOs;
using OrganizationService.Application.Queries;
using OrganizationService.Infrastructure.Persistence;
namespace OrganizationService.Application.Handlers;

public class GetOrganizationSettingsHandler(IOrganizationSettingsAppService svc)
    : IRequestHandler<GetOrganizationSettingsQuery, OrganizationSettingsDto?>
{
    public Task<OrganizationSettingsDto?> Handle(GetOrganizationSettingsQuery request, CancellationToken ct)
        => svc.GetAsync(request.OrganizationId, request.ActorUserId, request.CanEdit, ct);
}

public class UpdateOrganizationSettingsHandler(IOrganizationSettingsAppService svc)
    : IRequestHandler<UpdateOrganizationSettingsCommand, OrganizationSettingsDto>
{
    public Task<OrganizationSettingsDto> Handle(UpdateOrganizationSettingsCommand request, CancellationToken ct)
        => svc.UpdateAsync(request.OrganizationId, request.Request, request.ActorUserId, ct);
}

public class UploadOrganizationLogoHandler(IOrganizationSettingsAppService svc)
    : IRequestHandler<UploadOrganizationLogoCommand, OrganizationSettingsDto>
{
    public Task<OrganizationSettingsDto> Handle(UploadOrganizationLogoCommand request, CancellationToken ct)
        => svc.UploadLogoAsync(request.OrganizationId, request.FileStream, request.FileName, request.ContentType, request.Length, request.ActorUserId, ct);
}

public class RemoveOrganizationLogoHandler(IOrganizationSettingsAppService svc)
    : IRequestHandler<RemoveOrganizationLogoCommand, OrganizationSettingsDto>
{
    public Task<OrganizationSettingsDto> Handle(RemoveOrganizationLogoCommand request, CancellationToken ct)
        => svc.RemoveLogoAsync(request.OrganizationId, request.ActorUserId, ct);
}

public class ListOrganizationAuditLogsHandler(OrganizationDbContext db)
    : IRequestHandler<ListOrganizationAuditLogsQuery, IReadOnlyList<OrganizationAuditLogDto>>
{
    public async Task<IReadOnlyList<OrganizationAuditLogDto>> Handle(ListOrganizationAuditLogsQuery request, CancellationToken ct)
    {
        var query = db.OrganizationAuditLogs.AsNoTracking()
            .Where(a => a.EntityId == request.OrganizationId || a.EntityType == "OrganizationSettings");

        if (request.From.HasValue)
            query = query.Where(a => a.OccurredAt >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(a => a.OccurredAt <= request.To.Value);

        return await query
            .OrderByDescending(a => a.OccurredAt)
            .Take(request.Take)
            .Select(a => new OrganizationAuditLogDto
            {
                Id = a.Id,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                Action = a.Action,
                ActorUserId = a.ActorUserId,
                OccurredAt = a.OccurredAt,
            })
            .ToListAsync(ct);
    }
}
