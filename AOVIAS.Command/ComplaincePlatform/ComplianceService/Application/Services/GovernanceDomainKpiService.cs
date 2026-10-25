using ComplianceService.Application.DTOs;
using ComplianceService.Domain.Documentation;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

public interface IGovernanceDomainKpiService
{
    Task<GovernanceDomainKpisDto> GetAsync(Guid organizationId, Guid hospitalId, CancellationToken ct = default);
}

public sealed class GovernanceDomainKpiService(ComplianceDbContext db) : IGovernanceDomainKpiService
{
    public async Task<GovernanceDomainKpisDto> GetAsync(Guid organizationId, Guid hospitalId, CancellationToken ct = default)
    {
        var docsEffective = await db.ControlledDocuments.AsNoTracking()
            .CountAsync(d => d.TenantId == organizationId && d.Status == ControlledDocumentStatus.Effective && !d.IsArchived, ct);

        var openIncidents = await db.Incidents.AsNoTracking()
            .CountAsync(i => i.TenantId == organizationId
                && i.Status != IncidentStatus.Closed
                && i.Status != IncidentStatus.Cancelled, ct);

        var openRisks = await db.Risks.AsNoTracking()
            .CountAsync(r => r.TenantId == organizationId && r.Status != RiskStatus.Closed, ct);

        // Training/LMS not yet a Compliance SSOT — report 0 with note.
        _ = hospitalId;
        return new GovernanceDomainKpisDto
        {
            DocumentsEffective = docsEffective,
            TrainingOverdue = 0,
            OpenIncidents = openIncidents,
            OpenRisks = openRisks,
            Notes = "Training overdue is 0 until LMS module lands; other counts from ComplianceService."
        };
    }
}
