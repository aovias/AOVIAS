using System.Text.Json;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Documentation;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain;
using ComplianceService.Domain.Documentation;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

/// <summary>
/// Read-model projection for Documentation Intelligence &amp; Compliance Graph (Sprint 15C).
/// No separate graph DB — queries existing QDMS and operational bounded contexts.
/// </summary>
public sealed class DocumentationIntelligenceService : IDocumentationIntelligenceService
{
    private readonly ComplianceDbContext _db;
    private readonly IDocumentationAuthoringService _authoring;

    public DocumentationIntelligenceService(ComplianceDbContext db, IDocumentationAuthoringService authoring)
    {
        _db = db;
        _authoring = authoring;
    }

    public async Task<ComplianceGraphDto> GetGraphAsync(Guid organizationId, CancellationToken ct = default)
    {
        var docs = await _db.ControlledDocuments.AsNoTracking()
            .Where(d => d.TenantId == organizationId && !d.IsArchived)
            .Take(100)
            .ToListAsync(ct);

        var xrefs = await _db.DocumentCrossReferences.AsNoTracking()
            .Where(x => docs.Select(d => d.Id).Contains(x.ControlledDocumentId))
            .ToListAsync(ct);

        var nodes = new List<ComplianceGraphNodeDto>();
        var edges = new List<ComplianceGraphEdgeDto>();

        foreach (var doc in docs)
        {
            var docNodeId = DocNodeId(doc.Id);
            nodes.Add(new ComplianceGraphNodeDto
            {
                Id = docNodeId,
                Label = doc.Title,
                NodeType = "document",
                Route = $"/app/documentation/authoring/{doc.Id}",
                ConnectionCount = xrefs.Count(x => x.ControlledDocumentId == doc.Id),
            });

            AddFrameworkChainNodes(doc, docNodeId, nodes, edges);

            if (doc.MappedObjectiveElementId.HasValue)
            {
                var oeId = OeNodeId(doc.MappedObjectiveElementId.Value);
                if (!nodes.Any(n => n.Id == oeId))
                {
                    nodes.Add(new ComplianceGraphNodeDto
                    {
                        Id = oeId,
                        Label = doc.MappedObjectiveElement ?? "Objective Element",
                        NodeType = "objectiveElement",
                        Route = "/app/journey/execution",
                    });
                }

                edges.Add(new ComplianceGraphEdgeDto
                {
                    SourceId = docNodeId,
                    TargetId = oeId,
                    RelationshipType = "mapsTo",
                    Bidirectional = true,
                });
            }
        }

        foreach (var xref in xrefs)
        {
            var targetNodeId = XrefNodeId(xref);
            if (!nodes.Any(n => n.Id == targetNodeId))
            {
                nodes.Add(new ComplianceGraphNodeDto
                {
                    Id = targetNodeId,
                    Label = xref.TargetLabel,
                    NodeType = xref.ReferenceType.ToString(),
                    Route = xref.TargetRoute,
                });
            }

            edges.Add(new ComplianceGraphEdgeDto
            {
                SourceId = DocNodeId(xref.ControlledDocumentId),
                TargetId = targetNodeId,
                RelationshipType = "references",
                Bidirectional = true,
            });
        }

        return new ComplianceGraphDto
        {
            Nodes = nodes,
            Edges = edges,
            TotalDocuments = docs.Count,
            TotalRelationships = edges.Count,
        };
    }

    public async Task<DocumentIntelligenceDto?> GetIntelligenceAsync(
        Guid organizationId, Guid documentId, CancellationToken ct = default)
    {
        var session = await _authoring.GetAuthoringAsync(organizationId, documentId, ct);
        if (session == null) return null;

        var impact = await BuildImpactAsync(organizationId, documentId, ct);
        var acks = await _db.DocumentAcknowledgements.AsNoTracking()
            .Where(a => a.ControlledDocumentId == documentId)
            .ToListAsync(ct);
        var ackPct = acks.Count > 0
            ? (double)acks.Count(a => a.AcknowledgedAt != null) / acks.Count * 100
            : 0;

        var mappingCoverage = session.MappedObjectiveElementId.HasValue ? 100
            : session.MappingSuggestions.Count > 0 ? 50 : 0;

        var linkedAssets = await BuildLinkedAssetsAsync(organizationId, documentId, ct);
        var deps = await GetDependenciesAsync(organizationId, documentId, ct);

        var aiConfidence = Math.Clamp(
            (session.Health.OverallHealthScore + mappingCoverage + (int)ackPct) / 3, 0, 100);

        return new DocumentIntelligenceDto
        {
            DocumentId = documentId,
            DocumentNumber = session.DocumentNumber,
            Title = session.Title,
            Health = session.Health,
            ComplianceMappingCoveragePercent = mappingCoverage,
            AcknowledgementPercent = Math.Round(ackPct, 1),
            LinkedAssetsCount = linkedAssets.Count,
            DependenciesCount = deps?.UsedBy.Sum(u => u.Count) ?? 0,
            AiConfidencePercent = aiConfidence,
            ReviewStatus = session.Status.ToString(),
            Impact = impact.Summary,
            LinkedAssets = linkedAssets,
            Strengths = BuildStrengths(session),
            Risks = BuildRisks(session, impact.Summary),
        };
    }

    public async Task<DocumentImpactDto?> GetImpactAsync(
        Guid organizationId, Guid documentId, CancellationToken ct = default) =>
        await BuildImpactAsync(organizationId, documentId, ct);

    public async Task<DocumentDependenciesDto?> GetDependenciesAsync(
        Guid organizationId, Guid documentId, CancellationToken ct = default)
    {
        var doc = await _db.ControlledDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == documentId, ct);
        if (doc == null) return null;

        var inboundXrefs = await _db.DocumentCrossReferences.AsNoTracking()
            .Where(x => x.TargetId == documentId && x.ReferenceType == DocumentCrossReferenceType.Document)
            .ToListAsync(ct);

        var policyRefs = await _db.DocumentCrossReferences.AsNoTracking()
            .CountAsync(x => x.TargetId == documentId, ct);

        var impact = await BuildImpactAsync(organizationId, documentId, ct);
        var activeCapas = impact?.Summary.AffectedCapas ?? 0;
        var oeCount = impact?.Summary.AffectedObjectiveElements ?? 0;

        var usedBy = new List<DependencyItemDto>();
        if (inboundXrefs.Count > 0)
            usedBy.Add(new DependencyItemDto
            {
                Category = "Documents referencing this",
                Count = inboundXrefs.Count,
                Examples = inboundXrefs.Take(3).Select(x => x.TargetLabel).ToList(),
            });
        if (oeCount > 0)
            usedBy.Add(new DependencyItemDto { Category = "Objective Elements", Count = oeCount });
        if (activeCapas > 0)
            usedBy.Add(new DependencyItemDto { Category = "Active CAPAs", Count = activeCapas });

        var hasBlockers = inboundXrefs.Count > 0 || activeCapas > 0 || policyRefs > 5;
        return new DocumentDependenciesDto
        {
            DocumentId = documentId,
            Title = doc.Title,
            CanArchive = !hasBlockers,
            CanSupersede = !hasBlockers,
            CanDelete = false,
            BlockReason = hasBlockers
                ? "Active dependencies exist — archive or supersede blocked until resolved."
                : string.Empty,
            UsedBy = usedBy,
        };
    }

    public async Task<DocumentRelationshipsDto?> GetRelationshipsAsync(
        Guid organizationId, Guid documentId, CancellationToken ct = default)
    {
        var doc = await _db.ControlledDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == documentId, ct);
        if (doc == null) return null;

        var graph = await BuildDocumentGraphAsync(organizationId, doc, ct);
        return new DocumentRelationshipsDto
        {
            DocumentId = documentId,
            Title = doc.Title,
            Graph = graph,
        };
    }

    public async Task<AiComplianceReviewDto?> ReviewAiAsync(
        Guid organizationId, AiComplianceReviewRequest request, CancellationToken ct = default)
    {
        var session = await _authoring.GetAuthoringAsync(organizationId, request.DocumentId, ct);
        if (session == null) return null;

        var suggestions = new List<AiReviewSuggestionDto>();
        var strengths = new List<string>();
        var risks = new List<string>();
        var score = session.Health.OverallHealthScore;

        if (session.Health.MissingSections.Count > 0)
        {
            foreach (var ms in session.Health.MissingSections)
                suggestions.Add(new AiReviewSuggestionDto
                {
                    Category = "Missing Section",
                    Message = $"Required section '{ms}' is incomplete.",
                    Severity = "high",
                });
            score -= 10;
            risks.Add($"{session.Health.MissingSections.Count} mandatory sections incomplete.");
        }
        else
        {
            strengths.Add("All mandatory sections present.");
        }

        if (!session.MappedObjectiveElementId.HasValue)
        {
            suggestions.Add(new AiReviewSuggestionDto
            {
                Category = "Compliance Mapping",
                Message = "No Objective Element mapping — link to NABH framework before publish.",
                Severity = "high",
            });
            score -= 15;
            risks.Add("Weak compliance mapping coverage.");
        }
        else
        {
            strengths.Add($"Mapped to {session.MappedObjectiveElement}.");
        }

        var proc = session.Sections.FirstOrDefault(s => s.SectionKey == "procedure");
        if (proc != null && proc.Blocks.Count < 2)
        {
            suggestions.Add(new AiReviewSuggestionDto
            {
                Category = "Weak Procedure",
                Message = "Procedure section lacks sufficient step-by-step detail.",
                Severity = "medium",
            });
            score -= 8;
        }

        if (session.Health.ReadabilityScore < 70)
        {
            suggestions.Add(new AiReviewSuggestionDto
            {
                Category = "Readability",
                Message = "Consider simplifying language for frontline staff readability.",
                Severity = "low",
            });
        }
        else
        {
            strengths.Add("Readability score acceptable for clinical use.");
        }

        if (session.DocumentType == ControlledDocumentType.Sop && !session.Sections.Any(s => s.SectionKey == "definitions"))
        {
            suggestions.Add(new AiReviewSuggestionDto
            {
                Category = "Definitions",
                Message = "SOP missing definitions section — add key terms.",
                Severity = "medium",
            });
        }

        var docMeta = await _db.ControlledDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == request.DocumentId, ct);
        if (docMeta != null && docMeta.ReviewFrequencyMonths <= 0)
        {
            suggestions.Add(new AiReviewSuggestionDto
            {
                Category = "Review Frequency",
                Message = "Set review frequency per accreditation policy.",
                Severity = "medium",
            });
        }

        score = Math.Clamp(score, 0, 100);
        return new AiComplianceReviewDto
        {
            OverallComplianceReviewScore = score,
            Strengths = strengths,
            Risks = risks,
            Suggestions = suggestions,
            IsAdvisoryOnly = true,
        };
    }

    public async Task<DocumentExplorerDto> GetExplorerAsync(Guid organizationId, CancellationToken ct = default)
    {
        var docs = await _db.ControlledDocuments.AsNoTracking()
            .Where(d => d.TenantId == organizationId && !d.IsArchived)
            .OrderBy(d => d.DocumentType)
            .ThenBy(d => d.Department)
            .ThenBy(d => d.Title)
            .ToListAsync(ct);

        var typeGroups = docs.GroupBy(d => d.DocumentType);
        var tree = typeGroups.Select(g => new DocumentExplorerNodeDto
        {
            Id = $"type-{g.Key}",
            Label = g.Key.ToString(),
            NodeType = "category",
            Children = g.GroupBy(d => d.Department ?? "General").Select(dept => new DocumentExplorerNodeDto
            {
                Id = $"dept-{g.Key}-{dept.Key}",
                Label = dept.Key,
                NodeType = "department",
                Children = dept.Select(d => new DocumentExplorerNodeDto
                {
                    Id = d.Id.ToString(),
                    Label = d.Title,
                    NodeType = "document",
                    DocumentId = d.Id,
                    Route = $"/app/documentation/authoring/{d.Id}",
                }).ToList(),
            }).ToList(),
        }).ToList();

        var recent = docs.OrderByDescending(d => d.LastUpdatedAt).Take(8)
            .Select(MapListItem).ToList();
        var pinned = docs.Where(d => d.Status is ControlledDocumentStatus.Effective or ControlledDocumentStatus.Published)
            .OrderByDescending(d => d.LastUpdatedAt).Take(5).Select(MapListItem).ToList();

        return new DocumentExplorerDto { Tree = tree, Recent = recent, Pinned = pinned };
    }

    public async Task<IReadOnlyList<DocumentSearchResultDto>> SearchAsync(
        Guid organizationId, string query, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var q = query.Trim().ToLowerInvariant();
        var docs = await _db.ControlledDocuments.AsNoTracking()
            .Where(d => d.TenantId == organizationId && !d.IsArchived)
            .ToListAsync(ct);

        var versionIds = docs.Where(d => d.CurrentVersionId.HasValue).Select(d => d.CurrentVersionId!.Value).ToList();
        var sections = await _db.DocumentSections.AsNoTracking()
            .Where(s => versionIds.Contains(s.DocumentVersionId))
            .ToListAsync(ct);

        var results = new List<DocumentSearchResultDto>();

        foreach (var doc in docs)
        {
            if (doc.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                || doc.DocumentNumber.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (doc.MappedObjectiveElement?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            {
                results.Add(new DocumentSearchResultDto
                {
                    DocumentId = doc.Id,
                    DocumentNumber = doc.DocumentNumber,
                    Title = doc.Title,
                    MatchContext = doc.Title,
                    MatchLocation = "Title",
                    Score = 1.0,
                    Route = $"/app/documentation/authoring/{doc.Id}",
                });
            }

            var docSections = sections.Where(s => s.DocumentVersionId == doc.CurrentVersionId);
            foreach (var sec in docSections)
            {
                if (sec.Title.Contains(q, StringComparison.OrdinalIgnoreCase)
                    || sec.BlocksJson.Contains(q, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(new DocumentSearchResultDto
                    {
                        DocumentId = doc.Id,
                        DocumentNumber = doc.DocumentNumber,
                        Title = doc.Title,
                        MatchContext = sec.Title,
                        MatchLocation = $"Section: {sec.Title}",
                        Score = 0.8,
                        Route = $"/app/documentation/authoring/{doc.Id}",
                    });
                }
            }
        }

        return results.OrderByDescending(r => r.Score).Take(50).ToList();
    }

    public async Task<DocumentHealthDashboardDto> GetHealthDashboardAsync(
        Guid organizationId, CancellationToken ct = default)
    {
        var docs = await _db.ControlledDocuments.AsNoTracking()
            .Where(d => d.TenantId == organizationId && !d.IsArchived)
            .ToListAsync(ct);

        var xrefCounts = await _db.DocumentCrossReferences.AsNoTracking()
            .GroupBy(x => x.ControlledDocumentId)
            .Select(g => new { DocumentId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var needsReview = docs.Count(d => d.NextReviewDate.HasValue && d.NextReviewDate <= now.AddDays(90));
        var weakMapping = docs.Count(d => !d.MappedObjectiveElementId.HasValue);
        var affectingAccreditation = docs.Count(d => d.MappedObjectiveElementId.HasValue
            && d.Status is ControlledDocumentStatus.Effective or ControlledDocumentStatus.Published);

        var topReferenced = xrefCounts
            .OrderByDescending(x => x.Count)
            .Take(5)
            .Join(docs, x => x.DocumentId, d => d.Id, (_, d) => MapListItem(d))
            .ToList();

        var insights = new List<DocumentInsightDto>
        {
            new()
            {
                InsightType = "coverage",
                Title = "Mapping coverage",
                Description = $"{docs.Count - weakMapping} of {docs.Count} documents mapped to framework elements.",
            },
            new()
            {
                InsightType = "review",
                Title = "Review pipeline",
                Description = $"{needsReview} documents due for review within 90 days.",
            },
        };

        if (topReferenced.Count > 0)
        {
            insights.Add(new DocumentInsightDto
            {
                InsightType = "reference",
                Title = "Most referenced",
                Description = topReferenced[0].Title,
                DocumentId = topReferenced[0].Id,
            });
        }

        return new DocumentHealthDashboardDto
        {
            HealthyDocuments = docs.Count(d => d.Status is ControlledDocumentStatus.Effective or ControlledDocumentStatus.Published),
            NeedsReview = needsReview,
            WeakMapping = weakMapping,
            LowAcknowledgement = 0,
            MissingSections = docs.Count(d => d.Status == ControlledDocumentStatus.Draft),
            AiReviewPending = docs.Count(d => d.Status == ControlledDocumentStatus.UnderReview),
            HighImpactDocuments = affectingAccreditation,
            RecentlyModified = docs.Count(d => d.LastUpdatedAt >= now.AddDays(-7)),
            TopReferencedDocuments = xrefCounts.Count,
            AffectingAccreditation = affectingAccreditation,
            Insights = insights,
            TopReferenced = topReferenced,
        };
    }

    public async Task<IReadOnlyList<CrossReferenceSuggestionDto>> GetCrossReferenceSuggestionsAsync(
        Guid organizationId, Guid documentId, string query, CancellationToken ct = default)
    {
        var q = (query ?? string.Empty).Trim().ToLowerInvariant();
        if (q.Length < 1) return [];

        var suggestions = new List<CrossReferenceSuggestionDto>();

        var docs = await _db.ControlledDocuments.AsNoTracking()
            .Where(d => d.TenantId == organizationId && d.Id != documentId && !d.IsArchived
                        && (d.Title.Contains(q) || d.DocumentNumber.Contains(q)))
            .Take(5)
            .ToListAsync(ct);
        suggestions.AddRange(docs.Select(d => new CrossReferenceSuggestionDto
        {
            ReferenceType = "policy",
            TargetId = d.Id,
            Label = d.Title,
            InsertText = $"@{d.DocumentNumber}",
            Route = $"/app/documentation/authoring/{d.Id}",
        }));

        var oes = await _db.ObjectiveElements.AsNoTracking()
            .Where(oe => oe.Code.Contains(q) || oe.Description.Contains(q))
            .Take(5)
            .ToListAsync(ct);
        suggestions.AddRange(oes.Select(oe => new CrossReferenceSuggestionDto
        {
            ReferenceType = "objectiveElement",
            TargetId = oe.Id,
            Label = $"{oe.Code} — {oe.Description}",
            InsertText = $"@{oe.Code}",
            Route = $"/app/journey/execution",
        }));

        var findings = await _db.Findings.AsNoTracking()
            .Where(f => f.TenantId == organizationId && (f.Title.Contains(q) || f.Description.Contains(q)))
            .Take(3)
            .ToListAsync(ct);
        suggestions.AddRange(findings.Select(f => new CrossReferenceSuggestionDto
        {
            ReferenceType = "finding",
            TargetId = f.Id,
            Label = f.Title,
            InsertText = $"@finding-{f.Id.ToString()[..8]}",
            Route = $"/app/audit/findings/{f.Id}",
        }));

        return suggestions;
    }

    private async Task<DocumentImpactDto?> BuildImpactAsync(
        Guid organizationId, Guid documentId, CancellationToken ct)
    {
        var doc = await _db.ControlledDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == documentId, ct);
        if (doc == null) return null;

        var oeIds = new HashSet<Guid>();
        if (doc.MappedObjectiveElementId.HasValue)
            oeIds.Add(doc.MappedObjectiveElementId.Value);

        var xrefs = await _db.DocumentCrossReferences.AsNoTracking()
            .Where(x => x.ControlledDocumentId == documentId)
            .ToListAsync(ct);

        foreach (var xref in xrefs.Where(x => x.ReferenceType == DocumentCrossReferenceType.ObjectiveElement))
            oeIds.Add(xref.TargetId);

        var items = new List<ImpactItemDto>();
        var evidenceCount = oeIds.Count > 0
            ? await _db.ObjectiveElementEvidences.AsNoTracking().CountAsync(e => oeIds.Contains(e.ObjectiveElementId), ct)
            : 0;
        var findingsCount = oeIds.Count > 0
            ? await _db.Findings.AsNoTracking().CountAsync(f => f.TenantId == organizationId
                && f.ObjectiveElementId.HasValue && oeIds.Contains(f.ObjectiveElementId.Value), ct)
            : 0;
        var defCount = oeIds.Count > 0
            ? await _db.ComplianceDeficiencies.AsNoTracking().CountAsync(d => d.TenantId == organizationId
                && oeIds.Contains(d.ObjectiveElementId), ct)
            : 0;
        var capaCount = oeIds.Count > 0
            ? await _db.Capas.AsNoTracking().CountAsync(c => c.TenantId == organizationId
                && c.ObjectiveElementId.HasValue && oeIds.Contains(c.ObjectiveElementId.Value)
                && c.Status != CapaStatus.Completed && c.Status != CapaStatus.Cancelled, ct)
            : 0;
        var taskCount = oeIds.Count > 0
            ? await _db.ComplianceTasks.AsNoTracking().CountAsync(t => oeIds.Contains(t.ObjectiveElementId), ct)
            : 0;
        var auditCount = await _db.AuditSessions.AsNoTracking()
            .CountAsync(s => s.TenantId == organizationId, ct);
        var deptCount = (await _db.DocumentAcknowledgements.AsNoTracking()
            .Where(a => a.ControlledDocumentId == documentId)
            .Select(a => a.AssigneeName)
            .Distinct()
            .CountAsync(ct));

        if (doc.MappedChapter != null) items.Add(new ImpactItemDto { Category = "Chapter", Id = documentId, Label = doc.MappedChapter });
        if (doc.MappedStandard != null) items.Add(new ImpactItemDto { Category = "Standard", Id = documentId, Label = doc.MappedStandard });
        if (doc.MappedObjectiveElement != null)
            items.Add(new ImpactItemDto { Category = "OE", Id = doc.MappedObjectiveElementId ?? documentId, Label = doc.MappedObjectiveElement, Route = "/app/journey/execution" });

        var oeCount = oeIds.Count;
        var publishingRisk = oeCount > 20 || capaCount > 3 ? "High" : oeCount > 5 ? "Medium" : "Low";
        var summary = new DocumentImpactSummaryDto
        {
            AffectedChapters = string.IsNullOrEmpty(doc.MappedChapter) ? 0 : 1,
            AffectedStandards = string.IsNullOrEmpty(doc.MappedStandard) ? 0 : 1,
            AffectedObjectiveElements = oeCount,
            AffectedEvidence = evidenceCount,
            AffectedFindings = findingsCount,
            AffectedDeficiencies = defCount,
            AffectedCapas = capaCount,
            AffectedTasks = taskCount,
            AffectedAuditSessions = auditCount,
            AffectedDepartments = deptCount,
            PublishingRisk = publishingRisk,
            ImpactSummary = oeCount > 0
                ? $"This revision impacts {oeCount} Objective Element(s) and {evidenceCount} active evidence mapping(s)."
                : "No framework mapping — limited operational impact detected.",
        };

        return new DocumentImpactDto { DocumentId = documentId, Title = doc.Title, Summary = summary, Items = items };
    }

    private async Task<ComplianceGraphDto> BuildDocumentGraphAsync(
        Guid organizationId, ControlledDocument doc, CancellationToken ct)
    {
        var nodes = new List<ComplianceGraphNodeDto>
        {
            new()
            {
                Id = DocNodeId(doc.Id),
                Label = doc.Title,
                NodeType = "document",
                Route = $"/app/documentation/authoring/{doc.Id}",
            },
        };
        var edges = new List<ComplianceGraphEdgeDto>();

        if (doc.MappedObjectiveElementId.HasValue)
        {
            var oeId = OeNodeId(doc.MappedObjectiveElementId.Value);
            nodes.Add(new ComplianceGraphNodeDto { Id = oeId, Label = doc.MappedObjectiveElement ?? "OE", NodeType = "objectiveElement" });
            edges.Add(new ComplianceGraphEdgeDto { SourceId = DocNodeId(doc.Id), TargetId = oeId, RelationshipType = "mapsTo", Bidirectional = true });
        }

        AddFrameworkChainNodes(doc, DocNodeId(doc.Id), nodes, edges);

        var xrefs = await _db.DocumentCrossReferences.AsNoTracking()
            .Where(x => x.ControlledDocumentId == doc.Id)
            .ToListAsync(ct);

        foreach (var xref in xrefs)
        {
            var nid = XrefNodeId(xref);
            nodes.Add(new ComplianceGraphNodeDto { Id = nid, Label = xref.TargetLabel, NodeType = xref.ReferenceType.ToString(), Route = xref.TargetRoute });
            edges.Add(new ComplianceGraphEdgeDto { SourceId = DocNodeId(doc.Id), TargetId = nid, RelationshipType = "references", Bidirectional = true });
        }

        var impact = await BuildImpactAsync(organizationId, doc.Id, ct);
        if (impact != null && impact.Summary.AffectedCapas > 0)
        {
            var capaNode = "capa-cluster";
            nodes.Add(new ComplianceGraphNodeDto { Id = capaNode, Label = $"{impact.Summary.AffectedCapas} CAPAs", NodeType = "capa" });
            edges.Add(new ComplianceGraphEdgeDto { SourceId = DocNodeId(doc.Id), TargetId = capaNode, RelationshipType = "influences" });
        }

        return new ComplianceGraphDto { Nodes = nodes, Edges = edges, TotalDocuments = 1, TotalRelationships = edges.Count };
    }

    private async Task<IReadOnlyList<LinkedAssetDto>> BuildLinkedAssetsAsync(
        Guid organizationId, Guid documentId, CancellationToken ct)
    {
        var assets = new List<LinkedAssetDto>();
        var xrefs = await _db.DocumentCrossReferences.AsNoTracking()
            .Where(x => x.ControlledDocumentId == documentId)
            .ToListAsync(ct);

        assets.AddRange(xrefs.Select(x => new LinkedAssetDto
        {
            AssetType = x.ReferenceType.ToString(),
            Id = x.TargetId,
            Label = x.TargetLabel,
            Route = x.TargetRoute,
        }));

        var doc = await _db.ControlledDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == documentId, ct);
        if (doc?.MappedObjectiveElementId != null)
        {
            assets.Add(new LinkedAssetDto
            {
                AssetType = "objectiveElement",
                Id = doc.MappedObjectiveElementId.Value,
                Label = doc.MappedObjectiveElement ?? "OE",
                Route = "/app/journey/execution",
            });
        }

        return assets;
    }

    private static List<string> BuildStrengths(AuthoringSessionDto session)
    {
        var list = new List<string>();
        if (session.Health.CompletenessPercent >= 80) list.Add("High section completeness.");
        if (session.MappedObjectiveElementId.HasValue) list.Add("Framework mapping established.");
        if (session.Health.ReadabilityScore >= 75) list.Add("Good readability for clinical staff.");
        return list;
    }

    private static List<string> BuildRisks(AuthoringSessionDto session, DocumentImpactSummaryDto impact)
    {
        var list = new List<string>();
        if (session.Health.MissingSections.Count > 0) list.Add("Incomplete mandatory sections.");
        if (!session.MappedObjectiveElementId.HasValue) list.Add("No OE mapping.");
        if (impact.PublishingRisk == "High") list.Add($"High publishing risk — {impact.ImpactSummary}");
        return list;
    }

    private static ControlledDocumentListItemDto MapListItem(ControlledDocument d) => new()
    {
        Id = d.Id,
        DocumentNumber = d.DocumentNumber,
        Title = d.Title,
        DocumentType = d.DocumentType,
        Department = d.Department,
        Status = d.Status,
        CurrentVersion = d.CurrentVersionLabel,
        CreatedAt = d.CreatedAt,
        LastUpdatedAt = d.LastUpdatedAt,
    };

    private static string DocNodeId(Guid id) => $"doc-{id:N}";
    private static string OeNodeId(Guid id) => $"oe-{id:N}";
    private static string XrefNodeId(DocumentCrossReference x) => $"{x.ReferenceType}-{x.TargetId:N}";

    private static void AddFrameworkChainNodes(
        ControlledDocument doc,
        string docNodeId,
        List<ComplianceGraphNodeDto> nodes,
        List<ComplianceGraphEdgeDto> edges)
    {
        string? prevId = null;

        if (!string.IsNullOrWhiteSpace(doc.MappedFramework))
        {
            var fwId = $"fw-{doc.MappedFramework.Trim()}";
            if (!nodes.Any(n => n.Id == fwId))
            {
                nodes.Add(new ComplianceGraphNodeDto
                {
                    Id = fwId,
                    Label = doc.MappedFramework,
                    NodeType = "framework",
                    Route = "/app/journey/execution",
                });
            }
            prevId = fwId;
        }

        if (!string.IsNullOrWhiteSpace(doc.MappedChapter))
        {
            var chId = $"ch-{doc.MappedChapter.Trim()}";
            if (!nodes.Any(n => n.Id == chId))
            {
                nodes.Add(new ComplianceGraphNodeDto
                {
                    Id = chId,
                    Label = doc.MappedChapter,
                    NodeType = "chapter",
                    Route = "/app/journey/execution",
                });
            }
            if (prevId != null)
            {
                edges.Add(new ComplianceGraphEdgeDto
                {
                    SourceId = prevId,
                    TargetId = chId,
                    RelationshipType = "contains",
                });
            }
            prevId = chId;
        }

        if (!string.IsNullOrWhiteSpace(doc.MappedStandard))
        {
            var stId = $"st-{doc.MappedStandard.Trim()}";
            if (!nodes.Any(n => n.Id == stId))
            {
                nodes.Add(new ComplianceGraphNodeDto
                {
                    Id = stId,
                    Label = doc.MappedStandard,
                    NodeType = "standard",
                    Route = "/app/journey/execution",
                });
            }
            if (prevId != null)
            {
                edges.Add(new ComplianceGraphEdgeDto
                {
                    SourceId = prevId,
                    TargetId = stId,
                    RelationshipType = "contains",
                });
            }
            prevId = stId;
        }

        if (prevId != null)
        {
            edges.Add(new ComplianceGraphEdgeDto
            {
                SourceId = docNodeId,
                TargetId = prevId,
                RelationshipType = "supports",
                Bidirectional = true,
            });
        }
    }
}
