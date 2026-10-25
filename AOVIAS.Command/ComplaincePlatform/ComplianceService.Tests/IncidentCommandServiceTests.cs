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

public sealed class IncidentCommandServiceTests : IDisposable
{
    private readonly ComplianceDbContext _db;
    private readonly RecordingNotificationPublisher _notifications;
    private readonly IncidentCommandService _sut;

    public IncidentCommandServiceTests()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ComplianceDbContext(options, userContext: null);
        _notifications = new RecordingNotificationPublisher();
        _sut = new IncidentCommandService(_db, _notifications, NullLogger<IncidentCommandService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task PatchAsync_rejects_closed_incident()
    {
        var orgId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            TenantId = orgId,
            IncidentNumber = "INC-2026-00099",
            Title = "Closed event",
            Description = "done",
            Severity = IncidentSeverity.Moderate,
            Status = IncidentStatus.Closed,
            Department = FindingDepartment.Clinical,
            IncidentDateTime = DateTime.UtcNow,
            ClosedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };
        _db.Incidents.Add(incident);
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.PatchAsync(orgId, incident.Id, Guid.NewGuid(), new PatchIncidentRequestDto { Title = "Mutate" }));

        Assert.Contains("Closed or cancelled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveRcaAsync_rejects_closed_incident()
    {
        var orgId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            TenantId = orgId,
            IncidentNumber = "INC-2026-00100",
            Title = "Closed RCA",
            Description = "done",
            Severity = IncidentSeverity.Major,
            Status = IncidentStatus.Closed,
            Department = FindingDepartment.Clinical,
            IncidentDateTime = DateTime.UtcNow,
            ClosedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };
        _db.Incidents.Add(incident);
        await _db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.SaveRcaAsync(orgId, Guid.NewGuid(), new SaveRcaRequestDto
            {
                IncidentId = incident.Id,
                Method = "FiveWhys",
                FiveWhys = new FiveWhysDto { Why1 = "x" },
            }));

        Assert.Contains("Closed or cancelled", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveRcaAsync_persists_typed_five_whys_and_actions()
    {
        var orgId = Guid.NewGuid();
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            TenantId = orgId,
            IncidentNumber = "INC-2026-00101",
            Title = "Open RCA",
            Description = "desc",
            Severity = IncidentSeverity.Major,
            Status = IncidentStatus.UnderInvestigation,
            Department = FindingDepartment.Clinical,
            IncidentDateTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };
        _db.Incidents.Add(incident);
        await _db.SaveChangesAsync();

        var dto = await _sut.SaveRcaAsync(orgId, Guid.NewGuid(), new SaveRcaRequestDto
        {
            IncidentId = incident.Id,
            Method = "FiveWhys",
            FiveWhys = new FiveWhysDto
            {
                Why1 = "Missed check",
                Why2 = "Interrupt",
                FinalRootCause = "No barrier",
            },
            CauseCategories = new CauseCategoriesDto
            {
                Human = new List<string> { "Fatigue" },
                Process = new List<string> { "No double check" },
            },
            RootCauseSummary = "Barrier missing",
            CorrectiveActionSummary = "Add barrier",
            PreventiveActionSummary = "Train team",
            RootCauseCategory = "ProcessFailure",
        });

        Assert.Equal("Missed check", dto.FiveWhys?.Why1);
        Assert.Equal("Barrier missing", dto.RootCauseSummary);
        Assert.Equal("Add barrier", dto.CorrectiveActionSummary);
        Assert.Contains("Fatigue", dto.CauseCategories!.Human);
        Assert.False(string.IsNullOrWhiteSpace(dto.FiveWhysJson));
    }

    [Fact]
    public async Task CreateAsync_high_severity_publishes_notification()
    {
        var orgId = Guid.NewGuid();
        await _sut.CreateAsync(orgId, Guid.NewGuid(), Guid.NewGuid(), new CreateIncidentRequestDto
        {
            Title = "Critical fall",
            Description = "Patient fell",
            Severity = "Critical",
            Category = "PatientFall",
            Department = "Clinical",
        });

        Assert.Contains(_notifications.Messages, m => m.EventType == "HighSeverityIncident");
    }

    private sealed class RecordingNotificationPublisher : IComplianceNotificationPublisher
    {
        public List<ComplianceNotificationMessage> Messages { get; } = new();

        public Task PublishAsync(ComplianceNotificationMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
