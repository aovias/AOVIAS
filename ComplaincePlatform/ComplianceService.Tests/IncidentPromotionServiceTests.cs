using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Infrastructure.Domain.Enums;
using Xunit;

namespace ComplianceService.Tests;

public sealed class IncidentPromotionServiceTests : IDisposable
{
    private readonly ComplianceDbContext _db;

    public IncidentPromotionServiceTests()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ComplianceDbContext(options, userContext: null);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task PromoteToFindingAsync_links_incident_to_new_finding_without_capa_table()
    {
        var orgId = Guid.NewGuid();
        var incident = SeedIncident(orgId, IncidentStatus.Reported);
        await _db.SaveChangesAsync();

        var sut = new IncidentPromotionService(
            _db,
            new FakeFindingPromotion(),
            NullLogger<IncidentPromotionService>.Instance);

        var result = await sut.PromoteToFindingAsync(
            orgId, null, Guid.NewGuid(),
            new PromoteIncidentFindingRequestDto { IncidentId = incident.Id });

        Assert.NotNull(result.FindingId);
        Assert.Null(result.DeficiencyId);

        var updated = await _db.Incidents.AsNoTracking().SingleAsync(i => i.Id == incident.Id);
        Assert.Equal(result.FindingId, updated.FindingId);
        Assert.Null(updated.CapaId);
        Assert.Null(updated.DeficiencyId);
        Assert.Equal(0, await _db.Capas.CountAsync());
    }

    [Fact]
    public async Task PromoteToDeficiencyAsync_applies_rca_corrective_preventive_to_deficiency_capa_ssot()
    {
        var orgId = Guid.NewGuid();
        var hospitalId = Guid.NewGuid();
        var incident = SeedIncident(orgId, IncidentStatus.RcaInProgress);
        var findingId = Guid.NewGuid();
        incident.FindingId = findingId;

        incident.RootCauseAnalysis = new IncidentRca
        {
            Id = Guid.NewGuid(),
            IncidentId = incident.Id,
            Method = RcaMethod.FiveWhys,
            FiveWhysJson = """{"why1":"Missed check","why2":"Interrupt","why3":"No barrier","why4":"Design gap","why5":"No review","finalRootCause":"Process design"}""",
            RootCauseSummary = "Process design gap",
            CorrectiveActionSummary = "Add dual verification",
            PreventiveActionSummary = "Revise SOP and train staff",
            RootCauseCategory = "ProcessFailure",
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };
        _db.IncidentRcas.Add(incident.RootCauseAnalysis);

        var deficiency = ComplianceDeficiency.Create(
            orgId, orgId, hospitalId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "DEF-INC-001", "From incident", "desc",
            DeficiencyType.Assessment, DeficiencySeverity.Major,
            ObjectiveElementClassification.Core, null, "AuditFinding", "tester");
        deficiency.AssignCapaNumber("CAPA-2026-00001");
        _db.ComplianceDeficiencies.Add(deficiency);
        await _db.SaveChangesAsync();

        var fake = new FakeFindingPromotion
        {
            DeficiencyId = deficiency.Id,
            DeficiencyCode = deficiency.DeficiencyCode,
            FindingId = findingId,
        };

        var sut = new IncidentPromotionService(
            _db,
            fake,
            NullLogger<IncidentPromotionService>.Instance);

        var result = await sut.PromoteToDeficiencyAsync(
            orgId, hospitalId, Guid.NewGuid(),
            new PromoteIncidentDeficiencyRequestDto { IncidentId = incident.Id, FindingId = findingId });

        Assert.Equal(deficiency.Id, result.DeficiencyId);

        var updatedIncident = await _db.Incidents.AsNoTracking().SingleAsync(i => i.Id == incident.Id);
        Assert.Equal(deficiency.Id, updatedIncident.DeficiencyId);
        Assert.Null(updatedIncident.CapaId);
        Assert.Equal(0, await _db.Capas.CountAsync());

        var updatedDef = await _db.ComplianceDeficiencies.SingleAsync(d => d.Id == deficiency.Id);
        Assert.Equal("Add dual verification", updatedDef.CorrectiveActionSummary);
        Assert.Equal("Revise SOP and train staff", updatedDef.PreventiveActionSummary);
        Assert.Equal(RootCauseCategory.ProcessFailure, updatedDef.RootCauseCategory);
        Assert.Equal("Process design gap", updatedDef.RootCauseSummary);
        Assert.Equal("Missed check", updatedDef.Why1);
        Assert.Equal(nameof(DeficiencyOriginType.Incident), updatedDef.LinkedSourceType);
        Assert.Equal(CapaActionStatus.InProgress, updatedDef.CorrectiveActionStatus);
        Assert.Equal(CapaActionStatus.InProgress, updatedDef.PreventiveActionStatus);
    }

    [Fact]
    public async Task PromoteToFindingAsync_rejects_closed_incident()
    {
        var orgId = Guid.NewGuid();
        var incident = SeedIncident(orgId, IncidentStatus.Closed);
        await _db.SaveChangesAsync();

        var sut = new IncidentPromotionService(
            _db,
            new FakeFindingPromotion(),
            NullLogger<IncidentPromotionService>.Instance);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.PromoteToFindingAsync(
                orgId, null, Guid.NewGuid(),
                new PromoteIncidentFindingRequestDto { IncidentId = incident.Id }));

        Assert.Contains("Closed or cancelled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private Incident SeedIncident(Guid orgId, IncidentStatus status)
    {
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            TenantId = orgId,
            IncidentNumber = "INC-2026-00001",
            Title = "Medication near miss",
            Description = "Wrong dose almost administered",
            Severity = IncidentSeverity.Major,
            Status = status,
            Department = FindingDepartment.Clinical,
            IncidentDateTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            ClosedAt = status == IncidentStatus.Closed ? DateTime.UtcNow : null,
            RowVersion = new byte[8],
        };
        _db.Incidents.Add(incident);
        return incident;
    }

    private sealed class FakeFindingPromotion : IFindingPromotionService
    {
        public Guid FindingId { get; set; } = Guid.NewGuid();
        public Guid DeficiencyId { get; set; } = Guid.NewGuid();
        public string DeficiencyCode { get; set; } = "DEF-FAKE";

        public Task<FindingPromotionEligibilityDto> GetEligibilityAsync(Guid organizationId, Guid findingId, CancellationToken ct = default)
            => Task.FromResult(new FindingPromotionEligibilityDto());

        public Task<FindingPromotionStatusDto> GetPromotionStatusAsync(Guid organizationId, Guid findingId, Guid hospitalId, CancellationToken ct = default)
            => Task.FromResult(new FindingPromotionStatusDto());

        public Task<PromoteFindingResultDto> PromoteAsync(
            Guid organizationId, Guid findingId, Guid hospitalId, Guid? userId,
            CancellationToken ct = default, bool allowAdditional = true)
            => Task.FromResult(new PromoteFindingResultDto
            {
                FindingId = findingId == Guid.Empty ? FindingId : findingId,
                DeficiencyId = DeficiencyId,
                DeficiencyCode = DeficiencyCode,
                PromotedAt = DateTime.UtcNow,
            });
    }
}
