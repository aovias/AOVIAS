using ComplianceService.Application.BackgroundJobs;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
using ComplianceService.Domain.Documentation;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ComplianceService.Tests;

public sealed class DocumentControlLifecycleTests : IDisposable
{
    private readonly ComplianceDbContext _db;
    private readonly RecordingNotificationPublisher _notifications;
    private readonly DocumentationQueryService _queries;
    private readonly DocumentationCommandService _commands;

    public DocumentControlLifecycleTests()
    {
        var options = new DbContextOptionsBuilder<ComplianceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ComplianceDbContext(options, userContext: null);
        _notifications = new RecordingNotificationPublisher();
        _queries = new DocumentationQueryService(_db);
        _commands = new DocumentationCommandService(_db, _queries, NullLogger<DocumentationCommandService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task ReviewDueJob_TransitionsEffectivePastNextReview_AndCreatesDocumentReview()
    {
        var orgId = Guid.NewGuid();
        var doc = SeedEffectiveDocument(orgId, nextReviewDate: DateTime.UtcNow.AddDays(-1));
        await _db.SaveChangesAsync();

        var job = new DocumentReviewDueJob(
            _db,
            _notifications,
            NullLogger<DocumentReviewDueJob>.Instance);

        await job.ProcessReviewDueAsync();

        var updated = await _db.ControlledDocuments.AsNoTracking().SingleAsync(d => d.Id == doc.Id);
        Assert.Equal(ControlledDocumentStatus.ReviewDue, updated.Status);

        var review = await _db.DocumentReviews.AsNoTracking()
            .SingleOrDefaultAsync(r => r.ControlledDocumentId == doc.Id && r.CompletedDate == null);
        Assert.NotNull(review);
        Assert.Equal(doc.CurrentVersionId, review!.DocumentVersionId);

        Assert.Contains(_notifications.Messages, m => m.EventType == "DocumentReviewDue");
    }

    [Fact]
    public async Task Publish_SupersedesPriorVersions()
    {
        var orgId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var (doc, v1, v2) = SeedApprovedDocumentWithTwoVersions(orgId);
        await _db.SaveChangesAsync();

        var result = await _commands.PublishAsync(
            orgId,
            doc.Id,
            userId,
            "tester",
            new PublishDocumentRequest
            {
                EffectiveDate = DateTime.UtcNow.Date,
                RequireReAcknowledge = false,
            });

        Assert.NotNull(result);
        Assert.Equal(ControlledDocumentStatus.Effective, result!.Status);

        var versions = await _db.DocumentVersions.AsNoTracking()
            .Where(v => v.ControlledDocumentId == doc.Id)
            .ToListAsync();

        var prior = versions.Single(v => v.Id == v1.Id);
        var current = versions.Single(v => v.Id == v2.Id);

        Assert.True(prior.IsSuperseded);
        Assert.False(prior.IsCurrent);
        Assert.False(current.IsSuperseded);
        Assert.True(current.IsCurrent);
    }

    [Fact]
    public void SupersedePriorVersions_MarksNonCurrentAsSuperseded()
    {
        var currentId = Guid.NewGuid();
        var versions = new List<DocumentVersion>
        {
            new() { Id = Guid.NewGuid(), VersionNumber = 1, IsCurrent = false, IsSuperseded = false },
            new() { Id = currentId, VersionNumber = 2, IsCurrent = true, IsSuperseded = false },
        };

        DocumentationCommandService.SupersedePriorVersions(versions, currentVersionId: currentId);

        Assert.True(versions[0].IsSuperseded);
        Assert.False(versions[0].IsCurrent);
        Assert.False(versions[1].IsSuperseded);
        Assert.True(versions[1].IsCurrent);
    }

    [Fact]
    public void ExpiredDocument_CannotTransitionToEffective()
    {
        var doc = new ControlledDocument
        {
            Id = Guid.NewGuid(),
            Status = ControlledDocumentStatus.Approved,
            ExpiryDate = DateTime.UtcNow.AddDays(-1),
        };

        Assert.Throws<InvalidOperationException>(() =>
            doc.TransitionTo(ControlledDocumentStatus.Published));

        doc.Status = ControlledDocumentStatus.Published;
        Assert.Throws<InvalidOperationException>(() =>
            doc.TransitionTo(ControlledDocumentStatus.Effective));
    }

    [Fact]
    public void ArchivedDocument_IsImmutable()
    {
        var doc = new ControlledDocument
        {
            Id = Guid.NewGuid(),
            Status = ControlledDocumentStatus.Archived,
            IsArchived = true,
        };

        Assert.True(doc.IsImmutable());
        Assert.Throws<InvalidOperationException>(() => doc.EnsureMutable());
    }

    private ControlledDocument SeedEffectiveDocument(Guid orgId, DateTime nextReviewDate)
    {
        var versionId = Guid.NewGuid();
        var doc = new ControlledDocument
        {
            Id = Guid.NewGuid(),
            TenantId = orgId,
            HospitalId = Guid.NewGuid(),
            DocumentNumber = $"DOC-{Guid.NewGuid():N}"[..16],
            Title = "Infection Control Policy",
            DocumentType = ControlledDocumentType.Policy,
            Status = ControlledDocumentStatus.Effective,
            CurrentVersionId = versionId,
            CurrentVersionLabel = "1.0",
            EffectiveDate = DateTime.UtcNow.AddMonths(-12),
            NextReviewDate = nextReviewDate,
            ReviewFrequencyMonths = 12,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };

        _db.ControlledDocuments.Add(doc);
        _db.DocumentVersions.Add(new DocumentVersion
        {
            Id = versionId,
            ControlledDocumentId = doc.Id,
            VersionNumber = 1,
            VersionLabel = "1.0",
            Content = "v1",
            IsCurrent = true,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        });

        return doc;
    }

    private (ControlledDocument Doc, DocumentVersion V1, DocumentVersion V2) SeedApprovedDocumentWithTwoVersions(Guid orgId)
    {
        var docId = Guid.NewGuid();
        var v1 = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            ControlledDocumentId = docId,
            VersionNumber = 1,
            VersionLabel = "1.0",
            Content = "old",
            IsCurrent = false,
            IsSuperseded = false,
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            RowVersion = new byte[8],
        };
        var v2 = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            ControlledDocumentId = docId,
            VersionNumber = 2,
            VersionLabel = "2.0",
            Content = "new",
            IsCurrent = true,
            IsSuperseded = false,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };

        var doc = new ControlledDocument
        {
            Id = docId,
            TenantId = orgId,
            HospitalId = Guid.NewGuid(),
            DocumentNumber = $"DOC-{Guid.NewGuid():N}"[..16],
            Title = "SOP Hand Hygiene",
            DocumentType = ControlledDocumentType.Sop,
            Status = ControlledDocumentStatus.Approved,
            CurrentVersionId = v2.Id,
            CurrentVersionLabel = v2.VersionLabel,
            ReviewFrequencyMonths = 12,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };

        _db.ControlledDocuments.Add(doc);
        _db.DocumentVersions.AddRange(v1, v2);
        return (doc, v1, v2);
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
