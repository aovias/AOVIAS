using ComplianceService.Application.Analytics;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Documentation;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

/// <summary>
/// Sprint 17A — Enterprise Compliance Graph read-model projection.
/// Consumes existing SSOTs; no graph DB, no duplicate workflows.
/// </summary>
public sealed class EnterpriseComplianceGraphService : IEnterpriseComplianceGraphService
{
    private readonly ComplianceDbContext _db;
    private readonly IAuditorTraceabilityService _auditorTraceability;
    private readonly IAnalyticsAggregationService _analytics;

    public EnterpriseComplianceGraphService(
        ComplianceDbContext db,
        IAuditorTraceabilityService auditorTraceability,
        IAnalyticsAggregationService analytics)
    {
        _db = db;
        _auditorTraceability = auditorTraceability;
        _analytics = analytics;
    }

    public async Task<GraphEntitySummaryDto?> GetEntityAsync(Guid organizationId, Guid entityId, CancellationToken ct = default)
    {
        var resolved = await ResolveEntityAsync(organizationId, entityId, ct);
        return resolved?.Summary;
    }

    public async Task<GraphRelationshipsDto?> GetRelationshipsAsync(Guid organizationId, Guid entityId, CancellationToken ct = default)
    {
        var resolved = await ResolveEntityAsync(organizationId, entityId, ct);
        if (resolved == null) return null;

        var incoming = new List<GraphRelationshipDto>();
        var outgoing = new List<GraphRelationshipDto>();
        await AppendRelationshipsAsync(organizationId, resolved, incoming, outgoing, ct);

        var visual = BuildVisualGraph(resolved.Summary, incoming, outgoing);
        return new GraphRelationshipsDto
        {
            Entity = resolved.Summary,
            Incoming = incoming,
            Outgoing = outgoing,
            VisualGraph = visual,
            ImpactSummary = $"{incoming.Count} incoming · {outgoing.Count} outgoing relationships",
        };
    }

    public async Task<GraphTimelineDto?> GetTimelineAsync(Guid organizationId, Guid entityId, CancellationToken ct = default)
    {
        var resolved = await ResolveEntityAsync(organizationId, entityId, ct);
        if (resolved == null) return null;

        var entries = await BuildTimelineAsync(organizationId, resolved, ct);
        return new GraphTimelineDto { Entity = resolved.Summary, Entries = entries };
    }

    public async Task<GraphImpactDto?> GetImpactAsync(Guid organizationId, Guid entityId, CancellationToken ct = default)
    {
        var resolved = await ResolveEntityAsync(organizationId, entityId, ct);
        if (resolved == null) return null;

        var rels = await GetRelationshipsAsync(organizationId, entityId, ct);
        var affected = GroupImpact(rels?.Incoming ?? Array.Empty<GraphRelationshipDto>(),
            rels?.Outgoing ?? Array.Empty<GraphRelationshipDto>());

        return new GraphImpactDto
        {
            Entity = resolved.Summary,
            Summary = $"Changes to {resolved.Summary.Title} may affect {affected.Sum(a => a.Count)} linked assets across {affected.Count} categories.",
            AffectedAreas = affected,
        };
    }

    public async Task<GraphDependenciesDto?> GetDependenciesAsync(Guid organizationId, Guid entityId, CancellationToken ct = default)
    {
        var resolved = await ResolveEntityAsync(organizationId, entityId, ct);
        if (resolved == null) return null;

        var rels = await GetRelationshipsAsync(organizationId, entityId, ct);
        var incoming = rels?.Incoming ?? Array.Empty<GraphRelationshipDto>();
        var outgoing = rels?.Outgoing ?? Array.Empty<GraphRelationshipDto>();

        var blocking = incoming
            .Where(r => r.IsBlocking || r.RelationshipType is "DependsOn" or "Blocks" or "References")
            .GroupBy(r => r.RelationshipType)
            .Select(g => new GraphDependencyItemDto
            {
                Category = g.First().SourceEntityType,
                RelationshipType = g.Key,
                Count = g.Count(),
                IsBlocking = true,
                Examples = g.Take(3).Select(x => x.SourceLabel).ToList(),
            }).ToList();

        var downstream = outgoing
            .GroupBy(r => r.TargetEntityType)
            .Select(g => new GraphDependencyItemDto
            {
                Category = g.Key,
                RelationshipType = g.First().RelationshipType,
                Count = g.Count(),
                Examples = g.Take(3).Select(x => x.TargetLabel).ToList(),
            }).ToList();

        var upstream = incoming
            .GroupBy(r => r.SourceEntityType)
            .Select(g => new GraphDependencyItemDto
            {
                Category = g.Key,
                RelationshipType = g.First().RelationshipType,
                Count = g.Count(),
                Examples = g.Take(3).Select(x => x.SourceLabel).ToList(),
            }).ToList();

        var hasBlockers = blocking.Count > 0 || downstream.Any(d => d.IsBlocking);
        return new GraphDependenciesDto
        {
            Entity = resolved.Summary,
            CanArchive = !hasBlockers,
            CanSupersede = !hasBlockers,
            CanDelete = false,
            CanClose = downstream.All(d => !d.IsBlocking),
            WarningMessage = hasBlockers
                ? "Soft warning: referenced assets exist — review dependencies before archive, supersede, or close."
                : string.Empty,
            Blocking = blocking,
            Downstream = downstream,
            Upstream = upstream,
        };
    }

    public async Task<GraphSearchDto> SearchAsync(
        Guid organizationId, string? query, string? entityType, CancellationToken ct = default)
    {
        var q = (query ?? string.Empty).Trim();
        if (q.Length < 2)
            return new GraphSearchDto { Query = q, TotalResults = 0 };

        var groups = new List<GraphSearchGroupDto>();
        var total = 0;

        if (entityType == null || entityType.Equals("document", StringComparison.OrdinalIgnoreCase))
        {
            var docs = await _db.ControlledDocuments.AsNoTracking()
                .Where(d => d.TenantId == organizationId && !d.IsArchived
                    && (d.Title.Contains(q) || d.DocumentNumber.Contains(q)))
                .Take(8)
                .Select(d => new GraphSearchResultItemDto
                {
                    EntityId = d.Id, EntityType = "ControlledDocument", Title = d.Title,
                    Subtitle = d.DocumentNumber, Status = d.Status.ToString(),
                    Route = $"/app/documentation/authoring/{d.Id}", Score = 100,
                }).ToListAsync(ct);
            if (docs.Count > 0) { groups.Add(new() { EntityType = "ControlledDocument", Label = "Documents", Items = docs }); total += docs.Count; }
        }

        if (entityType == null || entityType.Equals("finding", StringComparison.OrdinalIgnoreCase))
        {
            var findings = await _db.Findings.AsNoTracking()
                .Where(f => f.TenantId == organizationId
                    && (f.Title.Contains(q) || f.FindingNumber.Contains(q) || f.ClauseReference.Contains(q)))
                .Take(8)
                .Select(f => new GraphSearchResultItemDto
                {
                    EntityId = f.Id, EntityType = "Finding",
                    Title = (string.IsNullOrEmpty(f.FindingNumber) ? f.Title : f.FindingNumber + " — " + f.Title),
                    Status = f.Status.ToString(), Route = $"/app/audit/findings?selected={f.Id}", Score = 90,
                }).ToListAsync(ct);
            if (findings.Count > 0) { groups.Add(new() { EntityType = "Finding", Label = "Findings", Items = findings }); total += findings.Count; }
        }

        if (entityType == null || entityType.Equals("deficiency", StringComparison.OrdinalIgnoreCase))
        {
            var defs = await _db.ComplianceDeficiencies.AsNoTracking()
                .Where(d => d.TenantId == organizationId
                    && (d.Title.Contains(q) || d.DeficiencyCode.Contains(q)
                        || (d.CapaNumber != null && d.CapaNumber.Contains(q))))
                .Take(8)
                .Select(d => new GraphSearchResultItemDto
                {
                    EntityId = d.Id, EntityType = "Deficiency", Title = d.DeficiencyCode + " — " + d.Title,
                    Status = d.Status.ToString(), Route = $"/app/audit/corrective-actions?selected={d.Id}", Score = 90,
                }).ToListAsync(ct);
            if (defs.Count > 0) { groups.Add(new() { EntityType = "Deficiency", Label = "Deficiencies", Items = defs }); total += defs.Count; }
        }

        if (entityType == null || entityType.Equals("capa", StringComparison.OrdinalIgnoreCase))
        {
            var capas = await _db.Capas.AsNoTracking()
                .Where(c => c.TenantId == organizationId && c.Title.Contains(q))
                .Take(8)
                .Select(c => new GraphSearchResultItemDto
                {
                    EntityId = c.Id, EntityType = "CAPA", Title = c.Title,
                    Status = c.Status.ToString(), Route = $"/app/audit/capa/{c.Id}", Score = 85,
                }).ToListAsync(ct);

            // Phase 2 SSOT: deficiency CAPAs (CapaNumber) take precedence for ops search.
            var capaDefs = await _db.ComplianceDeficiencies.AsNoTracking()
                .Where(d => d.OrganizationId == organizationId
                    && d.CapaNumber != null
                    && (d.CapaNumber.Contains(q) || d.Title.Contains(q) || d.DeficiencyCode.Contains(q)))
                .Take(8)
                .Select(d => new GraphSearchResultItemDto
                {
                    EntityId = d.Id,
                    EntityType = "CAPA",
                    Title = d.CapaNumber + " — " + d.Title,
                    Status = d.Status.ToString(),
                    Route = $"/app/audit/corrective-actions?selected={d.Id}",
                    Score = 90,
                }).ToListAsync(ct);

            var capaItems = capaDefs
                .Concat(capas)
                .GroupBy(x => x.EntityId)
                .Select(g => g.OrderByDescending(x => x.Score).First())
                .Take(8)
                .ToList();
            if (capaItems.Count > 0) { groups.Add(new() { EntityType = "CAPA", Label = "CAPA", Items = capaItems }); total += capaItems.Count; }
        }

        if (entityType == null || entityType.Equals("incident", StringComparison.OrdinalIgnoreCase))
        {
            var incidents = await _db.Incidents.AsNoTracking()
                .Where(i => i.TenantId == organizationId
                    && (i.Title.Contains(q) || i.IncidentNumber.Contains(q)))
                .Take(8)
                .Select(i => new GraphSearchResultItemDto
                {
                    EntityId = i.Id, EntityType = "Incident",
                    Title = i.IncidentNumber + " — " + i.Title,
                    Status = i.Status.ToString(),
                    Route = $"/app/patient-safety/command-center?selected={i.Id}", Score = 85,
                }).ToListAsync(ct);
            if (incidents.Count > 0) { groups.Add(new() { EntityType = "Incident", Label = "Incidents", Items = incidents }); total += incidents.Count; }
        }

        if (entityType == null || entityType.Equals("risk", StringComparison.OrdinalIgnoreCase))
        {
            var risks = await _db.Risks.AsNoTracking()
                .Where(r => r.TenantId == organizationId
                    && (r.Title.Contains(q)
                        || (r.Description != null && r.Description.Contains(q))
                        || (r.Category != null && r.Category.Contains(q))))
                .Take(8)
                .Select(r => new GraphSearchResultItemDto
                {
                    EntityId = r.Id, EntityType = "Risk",
                    Title = r.Title, Subtitle = r.Category,
                    Status = r.Status.ToString(),
                    Route = $"/app/risks/register?selected={r.Id}", Score = 88,
                }).ToListAsync(ct);
            if (risks.Count > 0) { groups.Add(new() { EntityType = "Risk", Label = "Risks", Items = risks }); total += risks.Count; }
        }

        if (entityType == null || entityType.Equals("clinicalIndicator", StringComparison.OrdinalIgnoreCase))
        {
            var indicators = await _db.ClinicalIndicators.AsNoTracking()
                .Where(i => i.TenantId == organizationId
                    && (i.Name.Contains(q) || i.IndicatorCode.Contains(q)))
                .Take(8)
                .Select(i => new GraphSearchResultItemDto
                {
                    EntityId = i.Id, EntityType = "ClinicalIndicator",
                    Title = i.IndicatorCode + " — " + i.Name,
                    Status = i.Status.ToString(),
                    Route = $"/app/quality-intelligence/indicators?selected={i.Id}", Score = 80,
                }).ToListAsync(ct);
            if (indicators.Count > 0) { groups.Add(new() { EntityType = "ClinicalIndicator", Label = "Clinical Indicators", Items = indicators }); total += indicators.Count; }
        }

        if (entityType == null || entityType.Equals("objectiveElement", StringComparison.OrdinalIgnoreCase))
        {
            var oes = await _db.ObjectiveElements.AsNoTracking()
                .Where(o => o.Code.Contains(q) || o.Title.Contains(q))
                .Take(8)
                .Select(o => new GraphSearchResultItemDto
                {
                    EntityId = o.Id, EntityType = "ObjectiveElement",
                    Title = o.Code + " — " + o.Title, Status = "Active",
                    Route = "/app/journey/execution", Score = 75,
                }).ToListAsync(ct);
            if (oes.Count > 0) { groups.Add(new() { EntityType = "ObjectiveElement", Label = "Objective Elements", Items = oes }); total += oes.Count; }
        }

        if (entityType == null || entityType.Equals("task", StringComparison.OrdinalIgnoreCase))
        {
            var hospitalIds = await _db.ComplianceJourneys.AsNoTracking()
                .Where(j => j.OrganizationId == organizationId)
                .Select(j => j.HospitalId)
                .Distinct()
                .ToListAsync(ct);

            if (hospitalIds.Count > 0)
            {
                var tasks = await _db.ComplianceTasks.AsNoTracking()
                    .Where(t => hospitalIds.Contains(t.HospitalId)
                        && ((t.Title != null && t.Title.Contains(q))
                            || (t.Description != null && t.Description.Contains(q))
                            || (t.DepartmentName != null && t.DepartmentName.Contains(q))))
                    .Take(8)
                    .Select(t => new GraphSearchResultItemDto
                    {
                        EntityId = t.Id,
                        EntityType = "Task",
                        Title = t.Title ?? "Compliance Task",
                        Subtitle = t.DepartmentName,
                        Status = t.Status.ToString(),
                        Route = $"/app/tasks?selected={t.Id}",
                        Score = 88,
                    }).ToListAsync(ct);
                if (tasks.Count > 0) { groups.Add(new() { EntityType = "Task", Label = "Tasks", Items = tasks }); total += tasks.Count; }
            }
        }

        if (entityType == null
            || entityType.Equals("audit", StringComparison.OrdinalIgnoreCase)
            || entityType.Equals("auditSession", StringComparison.OrdinalIgnoreCase))
        {
            var sessions = await _db.AuditSessions.AsNoTracking()
                .Where(s => s.TenantId == organizationId
                    && (s.SessionNumber.Contains(q)
                        || s.Scope.Contains(q)
                        || (s.LeadAuditorName != null && s.LeadAuditorName.Contains(q))
                        || (s.Summary != null && s.Summary.Contains(q))))
                .Take(8)
                .Select(s => new GraphSearchResultItemDto
                {
                    EntityId = s.Id,
                    EntityType = "AuditSession",
                    Title = s.SessionNumber + " — " + s.AuditType.ToString(),
                    Subtitle = s.Scope,
                    Status = s.Status.ToString(),
                    Route = $"/app/auditor/sessions/{s.Id}",
                    Score = 87,
                }).ToListAsync(ct);
            if (sessions.Count > 0) { groups.Add(new() { EntityType = "AuditSession", Label = "Audits", Items = sessions }); total += sessions.Count; }
        }

        if (entityType == null
            || entityType.Equals("committee", StringComparison.OrdinalIgnoreCase)
            || entityType.Equals("meeting", StringComparison.OrdinalIgnoreCase))
        {
            var linkHits = await _db.GovernanceTraceLinks.AsNoTracking()
                .Where(l => l.OrganizationId == organizationId
                    && ((l.SourceLabel.Contains(q) && (l.SourceEntityType == "Committee" || l.SourceEntityType == "Meeting"))
                        || (l.TargetLabel.Contains(q) && (l.TargetEntityType == "Committee" || l.TargetEntityType == "Meeting"))))
                .Take(16)
                .ToListAsync(ct);

            var meetingItems = linkHits
                .SelectMany(l => new[]
                {
                    new { Type = l.SourceEntityType, Id = l.SourceEntityId, Label = l.SourceLabel },
                    new { Type = l.TargetEntityType, Id = l.TargetEntityId, Label = l.TargetLabel },
                })
                .Where(x => x.Type is "Committee" or "Meeting")
                .GroupBy(x => x.Id)
                .Take(8)
                .Select(g =>
                {
                    var first = g.First();
                    return new GraphSearchResultItemDto
                    {
                        EntityId = first.Id,
                        EntityType = first.Type,
                        Title = first.Label,
                        Status = "Linked",
                        Route = first.Type == "Committee"
                            ? $"/app/governance/committees/{first.Id}/meetings"
                            : "/app/governance/committees",
                        Score = 70,
                    };
                })
                .ToList();

            if (meetingItems.Count > 0)
            {
                groups.Add(new() { EntityType = "Meeting", Label = "Committees / Meetings", Items = meetingItems });
                total += meetingItems.Count;
            }
        }

        return new GraphSearchDto { Query = q, TotalResults = total, Groups = groups };
    }

    public async Task<GraphNavigationDto?> GetNavigationAsync(Guid organizationId, Guid entityId, CancellationToken ct = default)
    {
        var resolved = await ResolveEntityAsync(organizationId, entityId, ct);
        if (resolved == null) return null;

        var rels = await GetRelationshipsAsync(organizationId, entityId, ct);
        var forward = (rels?.Outgoing ?? Array.Empty<GraphRelationshipDto>())
            .Take(8)
            .Select((r, i) => new GraphNavigationStepDto
            {
                Order = i + 1,
                Label = r.TargetLabel,
                EntityType = r.TargetEntityType,
                EntityId = r.TargetEntityId,
                Route = r.TargetRoute ?? string.Empty,
                RelationshipType = r.RelationshipType,
            }).ToList();

        var backward = (rels?.Incoming ?? Array.Empty<GraphRelationshipDto>())
            .Take(8)
            .Select((r, i) => new GraphNavigationStepDto
            {
                Order = i + 1,
                Label = r.SourceLabel,
                EntityType = r.SourceEntityType,
                EntityId = r.SourceEntityId,
                Route = r.TargetRoute ?? string.Empty,
                RelationshipType = r.RelationshipType,
            }).ToList();

        var shortcuts = (rels?.Outgoing ?? Array.Empty<GraphRelationshipDto>())
            .Concat(rels?.Incoming ?? Array.Empty<GraphRelationshipDto>())
            .GroupBy(r => r.TargetEntityId)
            .Take(6)
            .Select(g =>
            {
                var r = g.First();
                return new GraphEntitySummaryDto
                {
                    EntityId = r.TargetEntityId,
                    EntityType = r.TargetEntityType,
                    NodeId = r.TargetNodeId,
                    Title = r.TargetLabel,
                    Route = r.TargetRoute,
                };
            }).ToList();

        return new GraphNavigationDto
        {
            Entity = resolved.Summary,
            ForwardPath = forward,
            BackwardPath = backward,
            RelatedShortcuts = shortcuts,
        };
    }

    public async Task<GraphTraceabilityDto?> GetTraceabilityAsync(
        Guid organizationId, Guid? hospitalId, Guid entityId, CancellationToken ct = default)
    {
        var resolved = await ResolveEntityAsync(organizationId, entityId, ct);
        if (resolved == null) return null;

        var chain = new List<GraphTraceabilityStepDto> { new()
        {
            Order = 1, Layer = resolved.Summary.EntityType, Label = resolved.Summary.Title,
            Status = resolved.Summary.Status, EntityId = entityId, EntityType = resolved.Summary.EntityType,
            Route = resolved.Summary.Route,
        }};

        decimal? readiness = null;

        if (resolved.ObjectiveElementId.HasValue && hospitalId.HasValue && hospitalId != Guid.Empty)
        {
            var oeTrace = await _auditorTraceability.GetTraceabilityAsync(
                organizationId, hospitalId.Value, resolved.ObjectiveElementId.Value, ct);
            if (oeTrace != null)
            {
                readiness = oeTrace.CurrentReadinessScore;
                var order = 2;
                foreach (var t in oeTrace.Timeline.Where(t => t.Layer != "ObjectiveElement"))
                {
                    chain.Add(new GraphTraceabilityStepDto
                    {
                        Order = order++,
                        Layer = t.Layer,
                        Label = t.Label,
                        Status = t.Status,
                        EntityId = t.EntityId,
                        Detail = t.Detail,
                    });
                }
            }
        }
        else
        {
            var rels = await GetRelationshipsAsync(organizationId, entityId, ct);
            var order = 2;
            foreach (var r in (rels?.Outgoing ?? Array.Empty<GraphRelationshipDto>()).Take(12))
            {
                chain.Add(new GraphTraceabilityStepDto
                {
                    Order = order++,
                    Layer = r.TargetEntityType,
                    Label = r.TargetLabel,
                    Status = "Linked",
                    EntityId = r.TargetEntityId,
                    EntityType = r.TargetEntityType,
                    Route = r.TargetRoute,
                });
            }
        }

        return new GraphTraceabilityDto
        {
            Root = resolved.Summary,
            Chain = chain,
            Summary = $"Traceability chain with {chain.Count} layers from {resolved.Summary.EntityType}.",
            ReadinessScore = readiness,
        };
    }

    public async Task<GraphExecutiveImpactDto> GetExecutiveImpactAsync(Guid organizationId, CancellationToken ct = default)
    {
        var criticalIndicators = await _db.ClinicalIndicators.AsNoTracking()
            .Where(i => i.TenantId == organizationId && i.ThresholdStatus == ThresholdBand.Critical)
            .Take(5)
            .Select(i => new GraphEntitySummaryDto
            {
                EntityId = i.Id, EntityType = "ClinicalIndicator", NodeId = NodeId("ClinicalIndicator", i.Id),
                Title = i.Name, Status = i.ThresholdStatus.ToString(), Risk = i.RiskLevel.ToString(),
                Route = $"/app/quality-intelligence/indicators?selected={i.Id}",
            }).ToListAsync(ct);

        var topDocs = await _db.DocumentCrossReferences.AsNoTracking()
            .GroupBy(x => x.ControlledDocumentId)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .Join(_db.ControlledDocuments.AsNoTracking(),
                g => g.Key, d => d.Id,
                (g, d) => new GraphEntitySummaryDto
                {
                    EntityId = d.Id, EntityType = "ControlledDocument", NodeId = NodeId("ControlledDocument", d.Id),
                    Title = d.Title, Status = d.Status.ToString(),
                    RelatedObjectCount = g.Count(),
                    Route = $"/app/documentation/authoring/{d.Id}",
                }).ToListAsync(ct);

        var capas = await _db.Capas.AsNoTracking()
            .Where(c => c.TenantId == organizationId && c.Status != CapaStatus.Completed)
            .OrderByDescending(c => c.CreatedAt)
            .Take(5)
            .Select(c => new GraphEntitySummaryDto
            {
                EntityId = c.Id, EntityType = "CAPA", NodeId = NodeId("CAPA", c.Id),
                Title = c.Title, Status = c.Status.ToString(), Route = $"/app/audit/capa/{c.Id}",
            }).ToListAsync(ct);

        var openDefs = await _db.ComplianceDeficiencies.AsNoTracking()
            .CountAsync(d => d.TenantId == organizationId && d.Status != DeficiencyStatus.Closed, ct);

        var openIncidents = await _db.Incidents.AsNoTracking()
            .CountAsync(i => i.TenantId == organizationId && i.Status != IncidentStatus.Closed, ct);

        return new GraphExecutiveImpactDto
        {
            TopConnectedRisks = criticalIndicators,
            MostReferencedDocuments = topDocs,
            MostInfluentialCapas = capas,
            HighDependencyAreas = new[] { "Documentation cross-references", "Objective Element mappings", "CAPA remediation chains" },
            DepartmentsWithHighestImpact = await _db.Incidents.AsNoTracking()
                .Where(i => i.TenantId == organizationId)
                .GroupBy(i => i.Department)
                .OrderByDescending(g => g.Count())
                .Take(4)
                .Select(g => g.Key.ToString())
                .ToListAsync(ct),
            ComplianceBottlenecks = new List<string>
            {
                openDefs > 0 ? $"{openDefs} open deficiencies" : "No open deficiencies",
                openIncidents > 0 ? $"{openIncidents} open incidents" : "No open incidents",
            },
            CrossDomainHeatMap = new List<GraphImpactItemDto>
            {
                new() { Category = "Patient Safety → Quality", Count = openIncidents },
                new() { Category = "Findings → Deficiencies", Count = openDefs },
                new() { Category = "Documentation → Compliance", Count = topDocs.Count },
            },
        };
    }

    public async Task<GraphAnalyticsDto> GetAnalyticsAsync(Guid organizationId, CancellationToken ct = default)
    {
        var docCount = await _db.ControlledDocuments.AsNoTracking().CountAsync(d => d.TenantId == organizationId && !d.IsArchived, ct);
        var xrefCount = await _db.DocumentCrossReferences.AsNoTracking()
            .CountAsync(x => _db.ControlledDocuments.Any(d => d.Id == x.ControlledDocumentId && d.TenantId == organizationId), ct);
        var findingCount = await _db.Findings.AsNoTracking().CountAsync(f => f.TenantId == organizationId, ct);
        var defCount = await _db.ComplianceDeficiencies.AsNoTracking().CountAsync(d => d.TenantId == organizationId, ct);
        var incidentCount = await _db.Incidents.AsNoTracking().CountAsync(i => i.TenantId == organizationId, ct);
        var riskCount = await _db.Risks.AsNoTracking().CountAsync(r => r.TenantId == organizationId, ct);
        var indicatorCount = await _db.ClinicalIndicators.AsNoTracking().CountAsync(i => i.TenantId == organizationId, ct);

        var totalNodes = docCount + findingCount + defCount + incidentCount + riskCount + indicatorCount;
        var totalRels = xrefCount + findingCount + defCount + riskCount;
        var density = totalNodes > 0 ? Math.Round((decimal)totalRels / totalNodes, 2) : 0;

        var mappedDocs = await _db.ControlledDocuments.AsNoTracking()
            .CountAsync(d => d.TenantId == organizationId && d.MappedObjectiveElementId != null, ct);
        var traceability = docCount > 0 ? Math.Round(mappedDocs * 100m / docCount, 1) : 0;

        return new GraphAnalyticsDto
        {
            TotalNodes = totalNodes,
            TotalRelationships = totalRels,
            RelationshipDensity = density,
            MostConnectedAssets = xrefCount > 0 ? Math.Min(xrefCount, 50) : 0,
            CrossDomainCoverage = traceability,
            DependencyRisk = defCount > 0 ? Math.Min(100, defCount * 5m) : 0,
            TraceabilityCompleteness = traceability,
            DomainCoverage = new List<GraphImpactItemDto>
            {
                new() { Category = "Documentation", Count = docCount },
                new() { Category = "Findings", Count = findingCount },
                new() { Category = "Deficiencies", Count = defCount },
                new() { Category = "Incidents", Count = incidentCount },
                new() { Category = "Risks", Count = riskCount },
                new() { Category = "Clinical Indicators", Count = indicatorCount },
            },
        };
    }

    public async Task<ComplianceGraphDto> GetVisualGraphAsync(
        Guid organizationId, Guid? centerEntityId, CancellationToken ct = default)
    {
        if (centerEntityId.HasValue)
        {
            var rels = await GetRelationshipsAsync(organizationId, centerEntityId.Value, ct);
            return rels?.VisualGraph ?? new ComplianceGraphDto();
        }

        var docs = await _db.ControlledDocuments.AsNoTracking()
            .Where(d => d.TenantId == organizationId && !d.IsArchived)
            .Take(40)
            .ToListAsync(ct);

        var nodes = new List<ComplianceGraphNodeDto>();
        var edges = new List<ComplianceGraphEdgeDto>();

        foreach (var doc in docs)
        {
            var nid = NodeId("ControlledDocument", doc.Id);
            nodes.Add(new ComplianceGraphNodeDto
            {
                Id = nid, Label = doc.Title, NodeType = "document",
                Route = $"/app/documentation/authoring/{doc.Id}",
            });

            if (doc.MappedObjectiveElementId.HasValue)
            {
                var oeNid = NodeId("ObjectiveElement", doc.MappedObjectiveElementId.Value);
                if (!nodes.Any(n => n.Id == oeNid))
                    nodes.Add(new ComplianceGraphNodeDto { Id = oeNid, Label = doc.MappedObjectiveElement ?? "OE", NodeType = "objectiveElement", Route = "/app/journey/execution" });
                edges.Add(new ComplianceGraphEdgeDto { SourceId = nid, TargetId = oeNid, RelationshipType = "MappedTo", Bidirectional = true });
            }
        }

        var xrefs = await _db.DocumentCrossReferences.AsNoTracking()
            .Where(x => docs.Select(d => d.Id).Contains(x.ControlledDocumentId))
            .Take(80)
            .ToListAsync(ct);

        foreach (var xref in xrefs)
        {
            var src = NodeId("ControlledDocument", xref.ControlledDocumentId);
            var tgt = xref.TargetId != Guid.Empty ? NodeId(xref.ReferenceType.ToString(), xref.TargetId) : XrefSyntheticId(xref);
            if (!nodes.Any(n => n.Id == tgt))
                nodes.Add(new ComplianceGraphNodeDto { Id = tgt, Label = xref.TargetLabel, NodeType = xref.ReferenceType.ToString(), Route = xref.TargetRoute });
            edges.Add(new ComplianceGraphEdgeDto { SourceId = src, TargetId = tgt, RelationshipType = "References", Bidirectional = true });
        }

        return new ComplianceGraphDto
        {
            Nodes = nodes,
            Edges = edges,
            TotalDocuments = docs.Count,
            TotalRelationships = edges.Count,
        };
    }

    // ── Entity resolution & relationship projection ───────────────────────────

    private sealed record ResolvedEntity(GraphEntitySummaryDto Summary, Guid? ObjectiveElementId);

    private async Task<ResolvedEntity?> ResolveEntityAsync(Guid orgId, Guid entityId, CancellationToken ct)
    {
        var doc = await _db.ControlledDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.TenantId == orgId && d.Id == entityId, ct);
        if (doc != null)
            return new ResolvedEntity(new GraphEntitySummaryDto
            {
                EntityId = doc.Id, EntityType = "ControlledDocument", NodeId = NodeId("ControlledDocument", doc.Id),
                Title = doc.Title, Subtitle = doc.DocumentNumber, Status = doc.Status.ToString(),
                Route = $"/app/documentation/authoring/{doc.Id}", LastUpdated = doc.LastUpdatedAt,
            }, doc.MappedObjectiveElementId);

        var finding = await _db.Findings.AsNoTracking()
            .FirstOrDefaultAsync(f => f.TenantId == orgId && f.Id == entityId, ct);
        if (finding != null)
            return new ResolvedEntity(new GraphEntitySummaryDto
            {
                EntityId = finding.Id, EntityType = "Finding", NodeId = NodeId("Finding", finding.Id),
                Title = string.IsNullOrEmpty(finding.FindingNumber) ? finding.Title : finding.FindingNumber + " — " + finding.Title,
                Status = finding.Status.ToString(), Department = finding.Department.ToString(),
                Route = $"/app/audit/findings?selected={finding.Id}", Risk = finding.Severity.ToString(),
                LastUpdated = finding.LastUpdatedAt,
            }, finding.ObjectiveElementId);

        var def = await _db.ComplianceDeficiencies.AsNoTracking()
            .FirstOrDefaultAsync(d => d.TenantId == orgId && d.Id == entityId, ct);
        if (def != null)
            return new ResolvedEntity(new GraphEntitySummaryDto
            {
                EntityId = def.Id, EntityType = "Deficiency", NodeId = NodeId("Deficiency", def.Id),
                Title = def.DeficiencyCode + " — " + def.Title, Status = def.Status.ToString(),
                Department = def.Department, Route = $"/app/audit/deficiency-command-center?selected={def.Id}",
                Risk = def.Severity.ToString(), LastUpdated = def.UpdatedAt,
            }, def.ObjectiveElementId);

        var capa = await _db.Capas.AsNoTracking()
            .FirstOrDefaultAsync(c => c.TenantId == orgId && c.Id == entityId, ct);
        if (capa != null)
            return new ResolvedEntity(new GraphEntitySummaryDto
            {
                EntityId = capa.Id, EntityType = "CAPA", NodeId = NodeId("CAPA", capa.Id),
                Title = capa.Title, Status = capa.Status.ToString(), Owner = capa.OwnerName,
                Route = $"/app/audit/capa/{capa.Id}", LastUpdated = capa.CreatedAt,
            }, capa.ObjectiveElementId);

        var incident = await _db.Incidents.AsNoTracking()
            .FirstOrDefaultAsync(i => i.TenantId == orgId && i.Id == entityId, ct);
        if (incident != null)
            return new ResolvedEntity(new GraphEntitySummaryDto
            {
                EntityId = incident.Id, EntityType = "Incident", NodeId = NodeId("Incident", incident.Id),
                Title = incident.IncidentNumber + " — " + incident.Title, Status = incident.Status.ToString(),
                Department = incident.Department.ToString(), Owner = incident.OwnerName,
                Route = $"/app/patient-safety/command-center?selected={incident.Id}",
                Risk = incident.Severity.ToString(), LastUpdated = incident.LastUpdatedAt,
            }, null);

        var risk = await _db.Risks.AsNoTracking()
            .FirstOrDefaultAsync(r => r.TenantId == orgId && r.Id == entityId, ct);
        if (risk != null)
            return new ResolvedEntity(new GraphEntitySummaryDto
            {
                EntityId = risk.Id, EntityType = "Risk", NodeId = NodeId("Risk", risk.Id),
                Title = risk.Title, Subtitle = risk.Category, Status = risk.Status.ToString(),
                Owner = risk.OwnerName, Route = $"/app/risks/register?selected={risk.Id}",
                Risk = $"Score {risk.RiskScore}", LastUpdated = risk.LastUpdatedAt,
            }, risk.ObjectiveElementId);

        var indicator = await _db.ClinicalIndicators.AsNoTracking()
            .FirstOrDefaultAsync(i => i.TenantId == orgId && i.Id == entityId, ct);
        if (indicator != null)
            return new ResolvedEntity(new GraphEntitySummaryDto
            {
                EntityId = indicator.Id, EntityType = "ClinicalIndicator", NodeId = NodeId("ClinicalIndicator", indicator.Id),
                Title = indicator.IndicatorCode + " — " + indicator.Name, Status = indicator.Status.ToString(),
                Department = indicator.Department.ToString(), Owner = indicator.OwnerName,
                Route = $"/app/quality-intelligence/indicators?selected={indicator.Id}",
                Health = indicator.ThresholdStatus.ToString(), Risk = indicator.RiskLevel.ToString(),
                LastUpdated = indicator.LastUpdatedAt,
            }, indicator.ObjectiveElementId);

        var oe = await _db.ObjectiveElements.AsNoTracking().FirstOrDefaultAsync(o => o.Id == entityId, ct);
        if (oe != null)
            return new ResolvedEntity(new GraphEntitySummaryDto
            {
                EntityId = oe.Id, EntityType = "ObjectiveElement", NodeId = NodeId("ObjectiveElement", oe.Id),
                Title = oe.Code + " — " + oe.Title, Status = "Active", Route = "/app/journey/execution",
            }, oe.Id);

        var evidence = await _db.Evidences.AsNoTracking().FirstOrDefaultAsync(e => e.Id == entityId, ct);
        if (evidence != null)
            return new ResolvedEntity(new GraphEntitySummaryDto
            {
                EntityId = evidence.Id, EntityType = "Evidence", NodeId = NodeId("Evidence", evidence.Id),
                Title = evidence.Title ?? "Evidence", Status = evidence.LifecycleStatus.ToString(),
                Route = $"/app/evidence/{evidence.Id}",
            }, null);

        // Synthetic Committee/Meeting nodes via governance trace links (no Governance DB dependency).
        var govLink = await _db.GovernanceTraceLinks.AsNoTracking()
            .Where(l => l.OrganizationId == orgId
                && (l.SourceEntityId == entityId || l.TargetEntityId == entityId)
                && (l.SourceEntityType == "Committee" || l.SourceEntityType == "Meeting"
                    || l.TargetEntityType == "Committee" || l.TargetEntityType == "Meeting"))
            .FirstOrDefaultAsync(ct);
        if (govLink != null)
        {
            var isSource = govLink.SourceEntityId == entityId;
            var type = isSource ? govLink.SourceEntityType : govLink.TargetEntityType;
            var label = isSource ? govLink.SourceLabel : govLink.TargetLabel;
            if (type is "Committee" or "Meeting")
            {
                return new ResolvedEntity(new GraphEntitySummaryDto
                {
                    EntityId = entityId,
                    EntityType = type,
                    NodeId = NodeId(type, entityId),
                    Title = label,
                    Status = "Active",
                    Route = type == "Committee"
                        ? $"/app/governance/committees/{entityId}/meetings"
                        : "/app/governance/committees",
                }, null);
            }
        }

        return null;
    }

    private async Task AppendRelationshipsAsync(
        Guid orgId, ResolvedEntity entity,
        List<GraphRelationshipDto> incoming, List<GraphRelationshipDto> outgoing,
        CancellationToken ct)
    {
        switch (entity.Summary.EntityType)
        {
            case "Incident":
                await AppendIncidentRelationshipsAsync(orgId, entity.Summary.EntityId, incoming, outgoing, ct);
                break;
            case "Risk":
                await AppendRiskRelationshipsAsync(orgId, entity.Summary.EntityId, incoming, outgoing, ct);
                break;
            case "Finding":
                await AppendFindingRelationshipsAsync(orgId, entity.Summary.EntityId, incoming, outgoing, ct);
                break;
            case "Deficiency":
                await AppendDeficiencyRelationshipsAsync(orgId, entity.Summary.EntityId, entity.ObjectiveElementId, incoming, outgoing, ct);
                break;
            case "CAPA":
                await AppendCapaRelationshipsAsync(orgId, entity.Summary.EntityId, incoming, outgoing, ct);
                break;
            case "ControlledDocument":
                await AppendDocumentRelationshipsAsync(orgId, entity.Summary.EntityId, incoming, outgoing, ct);
                break;
            case "ClinicalIndicator":
                await AppendIndicatorRelationshipsAsync(orgId, entity.Summary.EntityId, entity.ObjectiveElementId, incoming, outgoing, ct);
                break;
            case "ObjectiveElement":
                await AppendObjectiveElementRelationshipsAsync(orgId, entity.Summary.EntityId, incoming, outgoing, ct);
                break;
        }

        // Committee/Meeting/Document edges projected from GovernanceTraceLinks (Gateway dual-write).
        await AppendGovernanceTraceRelationshipsAsync(orgId, entity.Summary.EntityId, incoming, outgoing, ct);
    }

    private async Task AppendRiskRelationshipsAsync(
        Guid orgId, Guid id, List<GraphRelationshipDto> incoming, List<GraphRelationshipDto> outgoing, CancellationToken ct)
    {
        var risk = await _db.Risks.AsNoTracking().FirstAsync(r => r.Id == id, ct);
        if (risk.ObjectiveElementId.HasValue)
        {
            var oe = await _db.ObjectiveElements.AsNoTracking().FirstOrDefaultAsync(o => o.Id == risk.ObjectiveElementId, ct);
            outgoing.Add(Rel(id, "Risk", risk.Title, risk.ObjectiveElementId.Value, "ObjectiveElement", oe?.Code ?? "OE", "MappedTo", "/app/journey/execution"));
        }
        if (risk.TaskId.HasValue)
            outgoing.Add(Rel(id, "Risk", risk.Title, risk.TaskId.Value, "Task", "Task", "AssignedTo", $"/app/tasks?selected={risk.TaskId}"));
        if (risk.FindingId.HasValue)
            outgoing.Add(Rel(id, "Risk", risk.Title, risk.FindingId.Value, "Finding", "Finding", "RelatedTo", $"/app/audit/findings?selected={risk.FindingId}"));
        if (risk.DeficiencyId.HasValue)
            outgoing.Add(Rel(id, "Risk", risk.Title, risk.DeficiencyId.Value, "Deficiency", "Deficiency", "LinkedTo", $"/app/audit/deficiency-command-center?selected={risk.DeficiencyId}"));
        if (risk.IncidentId.HasValue)
            outgoing.Add(Rel(id, "Risk", risk.Title, risk.IncidentId.Value, "Incident", "Incident", "RelatedTo", $"/app/patient-safety/command-center?selected={risk.IncidentId}"));
    }

    private async Task AppendGovernanceTraceRelationshipsAsync(
        Guid orgId, Guid entityId, List<GraphRelationshipDto> incoming, List<GraphRelationshipDto> outgoing, CancellationToken ct)
    {
        var links = await _db.GovernanceTraceLinks.AsNoTracking()
            .Where(l => l.OrganizationId == orgId && (l.SourceEntityId == entityId || l.TargetEntityId == entityId))
            .Take(40)
            .ToListAsync(ct);

        foreach (var link in links)
        {
            if (link.SourceEntityId == entityId)
            {
                outgoing.Add(Rel(
                    link.SourceEntityId, link.SourceEntityType, link.SourceLabel,
                    link.TargetEntityId, link.TargetEntityType, link.TargetLabel,
                    link.RelationshipType, RouteFor(link.TargetEntityType, link.TargetEntityId)));
            }
            else
            {
                incoming.Add(Rel(
                    link.SourceEntityId, link.SourceEntityType, link.SourceLabel,
                    link.TargetEntityId, link.TargetEntityType, link.TargetLabel,
                    link.RelationshipType, RouteFor(link.SourceEntityType, link.SourceEntityId)));
            }
        }
    }

    private static string RouteFor(string entityType, Guid id) => entityType switch
    {
        "ControlledDocument" or "Document" => $"/app/documentation/authoring/{id}",
        "Risk" => $"/app/risks/register?selected={id}",
        "Task" => $"/app/tasks?selected={id}",
        "Deficiency" => $"/app/audit/deficiency-command-center?selected={id}",
        "Incident" => $"/app/patient-safety/command-center?selected={id}",
        "Committee" => $"/app/governance/committees/{id}/meetings",
        "Meeting" => "/app/governance/committees",
        _ => "/app/compliance-graph",
    };

    private async Task AppendIncidentRelationshipsAsync(
        Guid orgId, Guid id, List<GraphRelationshipDto> incoming, List<GraphRelationshipDto> outgoing, CancellationToken ct)
    {
        var inc = await _db.Incidents.AsNoTracking().FirstAsync(i => i.Id == id, ct);
        if (inc.FindingId.HasValue)
            outgoing.Add(Rel(inc.Id, "Incident", inc.IncidentNumber, inc.FindingId.Value, "Finding", "Finding", "CreatedFrom", "/app/audit/findings?selected=" + inc.FindingId));
        // CAPA SSOT: Incident ↔ Deficiency (ComplianceDeficiency). Prefer DeficiencyId over legacy CapaId.
        if (inc.DeficiencyId.HasValue)
        {
            var defLabel = await _db.ComplianceDeficiencies.AsNoTracking()
                .Where(d => d.Id == inc.DeficiencyId.Value)
                .Select(d => !string.IsNullOrWhiteSpace(d.CapaNumber) ? d.CapaNumber! : d.DeficiencyCode)
                .FirstOrDefaultAsync(ct) ?? "Deficiency";
            outgoing.Add(Rel(inc.Id, "Incident", inc.IncidentNumber, inc.DeficiencyId.Value, "Deficiency", defLabel, "CreatedFrom", "/app/audit/deficiency-command-center?selected=" + inc.DeficiencyId));
            // Also surface CAPA node label for graph consumers expecting CAPA edges (same deficiency id).
            outgoing.Add(Rel(inc.Id, "Incident", inc.IncidentNumber, inc.DeficiencyId.Value, "CAPA", defLabel, "LinkedTo", "/app/audit/deficiency-command-center?selected=" + inc.DeficiencyId));
        }
        else if (inc.CapaId.HasValue)
        {
            // Legacy Capas-table link only when DeficiencyId is absent.
            outgoing.Add(Rel(inc.Id, "Incident", inc.IncidentNumber, inc.CapaId.Value, "CAPA", "CAPA", "LinkedTo", "/app/audit/capa/" + inc.CapaId));
        }

        var linkedRisks = await _db.Risks.AsNoTracking()
            .Where(r => r.TenantId == orgId && r.IncidentId == id).Take(5).ToListAsync(ct);
        foreach (var risk in linkedRisks)
            outgoing.Add(Rel(inc.Id, "Incident", inc.IncidentNumber, risk.Id, "Risk", risk.Title, "LinkedTo", $"/app/risks/register?selected={risk.Id}"));

        var relatedIndicators = await _db.ClinicalIndicators.AsNoTracking()
            .Where(i => i.TenantId == orgId && i.Department == inc.Department)
            .Take(5).ToListAsync(ct);
        foreach (var ind in relatedIndicators)
            outgoing.Add(Rel(inc.Id, "Incident", inc.IncidentNumber, ind.Id, "ClinicalIndicator", ind.Name, "MeasuredBy", $"/app/quality-intelligence/indicators?selected={ind.Id}"));
    }

    private async Task AppendFindingRelationshipsAsync(
        Guid orgId, Guid id, List<GraphRelationshipDto> incoming, List<GraphRelationshipDto> outgoing, CancellationToken ct)
    {
        var f = await _db.Findings.AsNoTracking().FirstAsync(x => x.Id == id, ct);

        var incidents = await _db.Incidents.AsNoTracking()
            .Where(i => i.TenantId == orgId && i.FindingId == id).Take(3).ToListAsync(ct);
        foreach (var inc in incidents)
            incoming.Add(Rel(inc.Id, "Incident", inc.IncidentNumber, f.Id, "Finding", f.FindingNumber, "CreatedFrom", $"/app/patient-safety/command-center?selected={inc.Id}"));

        // Phase 2 SSOT: Finding → CAPA edges prefer ComplianceDeficiencies by OriginId.
        var capaDefs = await _db.ComplianceDeficiencies.AsNoTracking()
            .Where(d => d.OrganizationId == orgId
                        && d.OriginId == id
                        && d.OriginType == DeficiencyOriginType.AuditFinding)
            .OrderByDescending(d => d.PromotedAt ?? d.CreatedAt)
            .Take(5)
            .ToListAsync(ct);

        foreach (var def in capaDefs)
        {
            var label = !string.IsNullOrWhiteSpace(def.CapaNumber) ? def.CapaNumber! : def.DeficiencyCode;
            outgoing.Add(Rel(
                f.Id, "Finding", f.FindingNumber,
                def.Id, "CAPA", label, "GeneratedBy",
                $"/app/audit/deficiency-command-center?selected={def.Id}"));
        }

        if (capaDefs.Count == 0 && f.PromotedDeficiencyId.HasValue)
        {
            outgoing.Add(Rel(
                f.Id, "Finding", f.FindingNumber,
                f.PromotedDeficiencyId.Value, "Deficiency", "Deficiency", "CreatedFrom",
                $"/app/audit/deficiency-command-center?selected={f.PromotedDeficiencyId}"));
        }

        if (f.ObjectiveElementId.HasValue)
        {
            var oe = await _db.ObjectiveElements.AsNoTracking().FirstOrDefaultAsync(o => o.Id == f.ObjectiveElementId, ct);
            outgoing.Add(Rel(f.Id, "Finding", f.FindingNumber, f.ObjectiveElementId.Value, "ObjectiveElement", oe?.Code ?? "OE", "MappedTo", "/app/journey/execution"));
        }

        // Legacy Capas table — secondary only when no deficiency CAPAs linked.
        if (capaDefs.Count == 0)
        {
            var capas = await _db.Capas.AsNoTracking().Where(c => c.SourceFindingId == id).Take(3).ToListAsync(ct);
            foreach (var c in capas)
                outgoing.Add(Rel(f.Id, "Finding", f.FindingNumber, c.Id, "CAPA", c.Title, "GeneratedBy", $"/app/audit/capa/{c.Id}"));
        }
    }

    private async Task AppendDeficiencyRelationshipsAsync(
        Guid orgId, Guid id, Guid? oeId, List<GraphRelationshipDto> incoming, List<GraphRelationshipDto> outgoing, CancellationToken ct)
    {
        var d = await _db.ComplianceDeficiencies.AsNoTracking().FirstAsync(x => x.Id == id, ct);

        var findings = await _db.Findings.AsNoTracking()
            .Where(f => f.TenantId == orgId && f.PromotedDeficiencyId == id).Take(3).ToListAsync(ct);
        foreach (var f in findings)
            incoming.Add(Rel(f.Id, "Finding", f.FindingNumber, d.Id, "Deficiency", d.DeficiencyCode, "CreatedFrom", $"/app/audit/findings?selected={f.Id}"));

        // Phase 4 Sprint 3 — Incident ↔ Deficiency graph edges (reverse of incident.DeficiencyId).
        var linkedIncidents = await _db.Incidents.AsNoTracking()
            .Where(i => i.TenantId == orgId && i.DeficiencyId == id).Take(5).ToListAsync(ct);
        foreach (var inc in linkedIncidents)
            incoming.Add(Rel(inc.Id, "Incident", inc.IncidentNumber, d.Id, "Deficiency", d.DeficiencyCode, "CreatedFrom", $"/app/patient-safety/command-center?selected={inc.Id}"));

        if (oeId.HasValue)
        {
            var oe = await _db.ObjectiveElements.AsNoTracking().FirstOrDefaultAsync(o => o.Id == oeId, ct);
            outgoing.Add(Rel(d.Id, "Deficiency", d.DeficiencyCode, oeId.Value, "ObjectiveElement", oe?.Code ?? "OE", "DependsOn", "/app/journey/execution"));
        }

        var tasks = await _db.ComplianceTasks.AsNoTracking()
            .Where(t => t.SourceDeficiencyId == id).Take(5).ToListAsync(ct);
        foreach (var t in tasks)
            outgoing.Add(Rel(d.Id, "Deficiency", d.DeficiencyCode, t.Id, "Task", t.Title ?? "Task", "AssignedTo", $"/app/tasks/{t.Id}"));
    }

    private async Task AppendCapaRelationshipsAsync(
        Guid orgId, Guid id, List<GraphRelationshipDto> incoming, List<GraphRelationshipDto> outgoing, CancellationToken ct)
    {
        var c = await _db.Capas.AsNoTracking().FirstAsync(x => x.Id == id, ct);
        if (c.SourceFindingId.HasValue)
        {
            var f = await _db.Findings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == c.SourceFindingId, ct);
            incoming.Add(Rel(f?.Id ?? c.SourceFindingId.Value, "Finding", f?.FindingNumber ?? "Finding", c.Id, "CAPA", c.Title, "GeneratedBy", $"/app/audit/findings?selected={c.SourceFindingId}"));
        }

        var incidents = await _db.Incidents.AsNoTracking()
            .Where(i => i.TenantId == orgId && i.CapaId == id).Take(3).ToListAsync(ct);
        foreach (var inc in incidents)
            incoming.Add(Rel(inc.Id, "Incident", inc.IncidentNumber, c.Id, "CAPA", c.Title, "LinkedTo", $"/app/patient-safety/command-center?selected={inc.Id}"));
    }

    private async Task AppendDocumentRelationshipsAsync(
        Guid orgId, Guid id, List<GraphRelationshipDto> incoming, List<GraphRelationshipDto> outgoing, CancellationToken ct)
    {
        var doc = await _db.ControlledDocuments.AsNoTracking().FirstAsync(d => d.Id == id, ct);
        if (doc.MappedObjectiveElementId.HasValue)
            outgoing.Add(Rel(doc.Id, "ControlledDocument", doc.Title, doc.MappedObjectiveElementId.Value, "ObjectiveElement", doc.MappedObjectiveElement ?? "OE", "MappedTo", "/app/journey/execution"));

        var xrefs = await _db.DocumentCrossReferences.AsNoTracking()
            .Where(x => x.ControlledDocumentId == id).ToListAsync(ct);
        foreach (var xref in xrefs)
        {
            var tgtId = xref.TargetId;
            outgoing.Add(new GraphRelationshipDto
            {
                Id = Guid.NewGuid().ToString(),
                SourceEntityId = id, SourceEntityType = "ControlledDocument", SourceNodeId = NodeId("ControlledDocument", id), SourceLabel = doc.Title,
                TargetEntityId = tgtId, TargetEntityType = xref.ReferenceType.ToString(), TargetNodeId = tgtId != Guid.Empty ? NodeId(xref.ReferenceType.ToString(), tgtId) : XrefSyntheticId(xref),
                TargetLabel = xref.TargetLabel, RelationshipType = "References", Direction = "Outgoing", TargetRoute = xref.TargetRoute,
            });
        }

        var inbound = await _db.DocumentCrossReferences.AsNoTracking()
            .Where(x => x.TargetId == id).Take(10).ToListAsync(ct);
        foreach (var xref in inbound)
        {
            var srcDoc = await _db.ControlledDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == xref.ControlledDocumentId, ct);
            incoming.Add(Rel(xref.ControlledDocumentId, "ControlledDocument", srcDoc?.Title ?? "Document", id, "ControlledDocument", doc.Title, "References", $"/app/documentation/authoring/{xref.ControlledDocumentId}", isBlocking: true));
        }
    }

    private async Task AppendIndicatorRelationshipsAsync(
        Guid orgId, Guid id, Guid? oeId, List<GraphRelationshipDto> incoming, List<GraphRelationshipDto> outgoing, CancellationToken ct)
    {
        var ind = await _db.ClinicalIndicators.AsNoTracking().FirstAsync(i => i.Id == id, ct);
        if (oeId.HasValue)
        {
            var oe = await _db.ObjectiveElements.AsNoTracking().FirstOrDefaultAsync(o => o.Id == oeId, ct);
            outgoing.Add(Rel(id, "ClinicalIndicator", ind.Name, oeId.Value, "ObjectiveElement", oe?.Code ?? "OE", "MeasuredBy", "/app/journey/execution"));
        }

        var incidents = await _db.Incidents.AsNoTracking()
            .Where(i => i.TenantId == orgId && i.Department == ind.Department)
            .OrderByDescending(i => i.IncidentDateTime).Take(5).ToListAsync(ct);
        foreach (var inc in incidents)
            incoming.Add(Rel(inc.Id, "Incident", inc.IncidentNumber, id, "ClinicalIndicator", ind.Name, "RelatedTo", $"/app/patient-safety/command-center?selected={inc.Id}"));
    }

    private async Task AppendObjectiveElementRelationshipsAsync(
        Guid orgId, Guid id, List<GraphRelationshipDto> incoming, List<GraphRelationshipDto> outgoing, CancellationToken ct)
    {
        var oe = await _db.ObjectiveElements.AsNoTracking().FirstAsync(o => o.Id == id, ct);

        var docs = await _db.ControlledDocuments.AsNoTracking()
            .Where(d => d.TenantId == orgId && d.MappedObjectiveElementId == id).Take(5).ToListAsync(ct);
        foreach (var d in docs)
            incoming.Add(Rel(d.Id, "ControlledDocument", d.Title, id, "ObjectiveElement", oe.Code, "MappedTo", $"/app/documentation/authoring/{d.Id}"));

        var findings = await _db.Findings.AsNoTracking()
            .Where(f => f.TenantId == orgId && f.ObjectiveElementId == id).Take(5).ToListAsync(ct);
        foreach (var f in findings)
            incoming.Add(Rel(f.Id, "Finding", f.FindingNumber, id, "ObjectiveElement", oe.Code, "MappedTo", $"/app/audit/findings?selected={f.Id}"));

        var defs = await _db.ComplianceDeficiencies.AsNoTracking()
            .Where(d => d.TenantId == orgId && d.ObjectiveElementId == id).Take(5).ToListAsync(ct);
        foreach (var d in defs)
            incoming.Add(Rel(d.Id, "Deficiency", d.DeficiencyCode, id, "ObjectiveElement", oe.Code, "DependsOn", $"/app/audit/deficiency-command-center?selected={d.Id}"));
    }

    private async Task<IReadOnlyList<GraphTimelineEntryDto>> BuildTimelineAsync(
        Guid orgId, ResolvedEntity entity, CancellationToken ct)
    {
        var entries = new List<GraphTimelineEntryDto>();

        switch (entity.Summary.EntityType)
        {
            case "Incident":
                var incTimeline = await _db.IncidentTimelineEntries.AsNoTracking()
                    .Where(t => t.IncidentId == entity.Summary.EntityId)
                    .OrderByDescending(t => t.OccurredAt).Take(30).ToListAsync(ct);
                entries.AddRange(incTimeline.Select(t => new GraphTimelineEntryDto
                {
                    OccurredAt = t.OccurredAt, Layer = "Incident", Action = t.EventType,
                    Label = t.EventType, Detail = t.Description, ActorName = t.ActorName,
                    EntityId = entity.Summary.EntityId, EntityType = "Incident",
                }));
                var incAudit = await _db.IncidentAuditTrails.AsNoTracking()
                    .Where(a => a.IncidentId == entity.Summary.EntityId).OrderByDescending(a => a.OccurredAt).Take(20).ToListAsync(ct);
                entries.AddRange(incAudit.Select(a => new GraphTimelineEntryDto
                {
                    OccurredAt = a.OccurredAt, Layer = "Incident", Action = a.Action, Label = a.Action, Detail = a.Detail,
                }));
                break;

            case "Finding":
                var fAudit = await _db.FindingAuditTrails.AsNoTracking()
                    .Where(a => a.FindingId == entity.Summary.EntityId).OrderByDescending(a => a.OccurredAt).Take(25).ToListAsync(ct);
                entries.AddRange(fAudit.Select(a => new GraphTimelineEntryDto
                {
                    OccurredAt = a.OccurredAt, Layer = "Finding", Action = a.Action, Label = a.Action,
                }));
                break;

            case "Deficiency":
                var dAudit = await _db.DeficiencyAuditTrails.AsNoTracking()
                    .Where(a => a.DeficiencyId == entity.Summary.EntityId).OrderByDescending(a => a.OccurredAt).Take(25).ToListAsync(ct);
                entries.AddRange(dAudit.Select(a => new GraphTimelineEntryDto
                {
                    OccurredAt = a.OccurredAt, Layer = "Deficiency", Action = a.Action, Label = a.Action, Detail = a.DetailJson,
                }));
                break;

            case "ClinicalIndicator":
                var iAudit = await _db.IndicatorAuditTrails.AsNoTracking()
                    .Where(a => a.IndicatorId == entity.Summary.EntityId).OrderByDescending(a => a.OccurredAt).Take(25).ToListAsync(ct);
                entries.AddRange(iAudit.Select(a => new GraphTimelineEntryDto
                {
                    OccurredAt = a.OccurredAt, Layer = "ClinicalIndicator", Action = a.Action, Label = a.Action, Detail = a.Detail, ActorName = a.ActorName,
                }));
                break;

            case "ControlledDocument":
                var docAudit = await _db.DocumentLifecycleAuditEntries.AsNoTracking()
                    .Where(a => a.ControlledDocumentId == entity.Summary.EntityId).OrderByDescending(a => a.CreatedAt).Take(25).ToListAsync(ct);
                entries.AddRange(docAudit.Select(a => new GraphTimelineEntryDto
                {
                    OccurredAt = a.CreatedAt, Layer = "Document", Action = a.EventType, Label = a.EventType,
                    Detail = a.Details, ActorName = a.PerformedByName,
                }));
                break;
        }

        var globalAudit = await _db.AuditLogs.AsNoTracking()
            .Where(a => a.EntityId == entity.Summary.EntityId)
            .OrderByDescending(a => a.Timestamp).Take(15).ToListAsync(ct);
        entries.AddRange(globalAudit.Select(a => new GraphTimelineEntryDto
        {
            OccurredAt = a.Timestamp, Layer = a.EntityType ?? "Platform", Action = a.Action, Label = a.Action, Detail = a.MetadataJson,
        }));

        return entries.OrderByDescending(e => e.OccurredAt).Take(50).ToList();
    }

    private static ComplianceGraphDto BuildVisualGraph(
        GraphEntitySummaryDto center,
        IReadOnlyList<GraphRelationshipDto> incoming,
        IReadOnlyList<GraphRelationshipDto> outgoing)
    {
        var nodes = new Dictionary<string, ComplianceGraphNodeDto>();
        var edges = new List<ComplianceGraphEdgeDto>();

        void AddNode(string nodeId, string label, string type, string? route)
        {
            if (!nodes.ContainsKey(nodeId))
                nodes[nodeId] = new ComplianceGraphNodeDto { Id = nodeId, Label = label, NodeType = type, Route = route };
        }

        AddNode(center.NodeId, center.Title, center.EntityType, center.Route);

        foreach (var r in incoming.Concat(outgoing))
        {
            AddNode(r.SourceNodeId, r.SourceLabel, r.SourceEntityType, null);
            AddNode(r.TargetNodeId, r.TargetLabel, r.TargetEntityType, r.TargetRoute);
            edges.Add(new ComplianceGraphEdgeDto
            {
                SourceId = r.SourceNodeId, TargetId = r.TargetNodeId,
                RelationshipType = r.RelationshipType, Bidirectional = false,
            });
        }

        return new ComplianceGraphDto
        {
            Nodes = nodes.Values.ToList(),
            Edges = edges,
            TotalDocuments = nodes.Values.Count(n => n.NodeType is "document" or "ControlledDocument"),
            TotalRelationships = edges.Count,
        };
    }

    private static IReadOnlyList<GraphImpactItemDto> GroupImpact(
        IReadOnlyList<GraphRelationshipDto> incoming,
        IReadOnlyList<GraphRelationshipDto> outgoing)
    {
        return incoming.Concat(outgoing)
            .GroupBy(r => r.TargetEntityType)
            .Select(g => new GraphImpactItemDto
            {
                Category = g.Key,
                Count = g.Count(),
                Examples = g.Take(3).Select(r => new GraphEntitySummaryDto
                {
                    EntityId = r.TargetEntityId, EntityType = r.TargetEntityType,
                    Title = r.TargetLabel, Route = r.TargetRoute,
                }).ToList(),
            }).ToList();
    }

    private static GraphRelationshipDto Rel(
        Guid srcId, string srcType, string srcLabel,
        Guid tgtId, string tgtType, string tgtLabel,
        string relType, string route, bool isBlocking = false) => new()
    {
        Id = $"{srcId}:{tgtId}:{relType}",
        SourceEntityId = srcId, SourceEntityType = srcType, SourceNodeId = NodeId(srcType, srcId), SourceLabel = srcLabel,
        TargetEntityId = tgtId, TargetEntityType = tgtType, TargetNodeId = NodeId(tgtType, tgtId), TargetLabel = tgtLabel,
        RelationshipType = relType, Direction = "Outgoing", TargetRoute = route, IsBlocking = isBlocking,
    };

    private static string NodeId(string type, Guid id) => $"{type}:{id}";
    private static string XrefSyntheticId(DocumentCrossReference xref) => $"xref:{xref.Id}";
}
