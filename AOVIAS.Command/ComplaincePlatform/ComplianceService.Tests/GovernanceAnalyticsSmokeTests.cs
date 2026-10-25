using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Documentation;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ComplianceService.Tests;

public class GovernanceAnalyticsSmokeTests
{
    private static ComplianceDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ComplianceDbContext(options);
    }

    [Fact]
    public async Task Governance_domain_kpis_count_docs_incidents_risks()
    {
        await using var db = CreateDb();
        var orgId = Guid.NewGuid();
        var hospitalId = Guid.NewGuid();
        var rowVersion = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 };

        db.ControlledDocuments.Add(new ControlledDocument
        {
            Id = Guid.NewGuid(),
            TenantId = orgId,
            Title = "Policy A",
            DocumentNumber = "POL-1",
            Status = ControlledDocumentStatus.Effective,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            RowVersion = rowVersion,
        });
        db.Incidents.Add(new Incident
        {
            Id = Guid.NewGuid(),
            TenantId = orgId,
            IncidentNumber = "INC-1",
            Title = "Slip",
            Description = "Test",
            Status = IncidentStatus.Reported,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            RowVersion = rowVersion,
        });
        db.Risks.Add(new Risk
        {
            Id = Guid.NewGuid(),
            TenantId = orgId,
            HospitalId = hospitalId,
            Title = "Fire risk",
            Likelihood = 2,
            Impact = 3,
            Status = RiskStatus.Identified,
            IdentifiedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            Severity = ComplianceSeverity.Major,
            RowVersion = rowVersion,
        });
        db.GovernanceTraceLinks.Add(new GovernanceTraceLink
        {
            Id = Guid.NewGuid(),
            OrganizationId = orgId,
            SourceEntityType = "Meeting",
            SourceEntityId = Guid.NewGuid(),
            SourceLabel = "MR Q1",
            TargetEntityType = "Risk",
            TargetEntityId = Guid.NewGuid(),
            TargetLabel = "Fire risk",
            RelationshipType = "LinkedTo",
            CreatedAt = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var svc = new GovernanceDomainKpiService(db);
        var kpis = await svc.GetAsync(orgId, hospitalId);
        Assert.Equal(1, kpis.DocumentsEffective);
        Assert.Equal(1, kpis.OpenIncidents);
        Assert.Equal(1, kpis.OpenRisks);
        Assert.Equal(0, kpis.TrainingOverdue);
        Assert.True(await db.GovernanceTraceLinks.AnyAsync());
    }
}
