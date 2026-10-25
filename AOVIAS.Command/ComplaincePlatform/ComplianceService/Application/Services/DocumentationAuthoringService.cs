using System.Text;
using System.Text.Json;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.Documentation;
using ComplianceService.Application.DTOs;
using ComplianceService.Domain.Documentation;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ComplianceService.Application.Services;

public sealed class DocumentationAuthoringService : IDocumentationAuthoringService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    private readonly ComplianceDbContext _db;

    static DocumentationAuthoringService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public DocumentationAuthoringService(ComplianceDbContext db) => _db = db;

    public async Task<AuthoringSessionDto> CreateAuthoringAsync(
        Guid organizationId, Guid hospitalId, Guid userId, string? userName,
        CreateAuthoringRequest request, CancellationToken ct = default)
    {
        var schemaKey = request.AuthoringSchemaKey
                        ?? (request.TemplateId.HasValue
                            ? await ResolveTemplateSchemaAsync(request.TemplateId.Value, ct)
                            : AuthoringSchemaRegistry.ResolveSchemaKey(request.DocumentType));

        var year = DateTime.UtcNow.Year;
        var count = await _db.ControlledDocuments.CountAsync(d => d.TenantId == organizationId, ct);
        var doc = new ControlledDocument
        {
            Id = Guid.NewGuid(),
            TenantId = organizationId,
            HospitalId = hospitalId,
            DocumentNumber = $"DOC-{year}-{(count + 1):D4}",
            Title = request.Title.Trim(),
            DocumentType = request.DocumentType,
            Department = request.Department,
            OwnerUserId = userId,
            OwnerName = userName,
            Status = ControlledDocumentStatus.Draft,
            CreatedByUserId = userId,
            CreatedByName = userName,
            CreatedAt = DateTime.UtcNow,
            LastUpdatedAt = DateTime.UtcNow,
        };

        var version = new DocumentVersion
        {
            Id = Guid.NewGuid(),
            ControlledDocumentId = doc.Id,
            VersionNumber = 1,
            VersionLabel = "1.0",
            AuthoringSchemaKey = schemaKey,
            IsCurrent = true,
            CreatedByUserId = userId,
            CreatedByName = userName,
            CreatedAt = DateTime.UtcNow,
        };

        var sections = BuildInitialSections(version.Id, schemaKey, request.Title);
        version.Content = ComposeMarkdown(doc.Title, sections);
        doc.CurrentVersionId = version.Id;
        doc.CurrentVersionLabel = version.VersionLabel;

        _db.ControlledDocuments.Add(doc);
        _db.DocumentVersions.Add(version);
        _db.DocumentSections.AddRange(sections);
        _db.DocumentLifecycleAuditEntries.Add(new DocumentLifecycleAuditEntry
        {
            Id = Guid.NewGuid(),
            ControlledDocumentId = doc.Id,
            DocumentVersionId = version.Id,
            EventType = "AuthoringCreated",
            ToStatus = ControlledDocumentStatus.Draft,
            PerformedByUserId = userId,
            PerformedByName = userName,
            Details = "Authoring Studio session created",
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);
        return (await BuildSessionAsync(organizationId, doc.Id, ct))!;
    }

    public async Task<AuthoringSessionDto?> GetAuthoringAsync(
        Guid organizationId, Guid documentId, CancellationToken ct = default) =>
        await BuildSessionAsync(organizationId, documentId, ct);

    public async Task<AuthoringSessionDto?> PatchAuthoringAsync(
        Guid organizationId, Guid documentId, Guid userId, string? userName,
        PatchAuthoringRequest request, CancellationToken ct = default)
    {
        var doc = await _db.ControlledDocuments
            .Include(d => d.Versions)
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == documentId && !d.IsArchived, ct);
        if (doc?.CurrentVersionId == null) return null;

        if (request.Title != null) doc.Title = request.Title.Trim();
        if (request.Department != null) doc.Department = request.Department.Trim();

        if (request.ManualMapping != null)
        {
            doc.MappedFramework = request.ManualMapping.Framework;
            doc.MappedChapter = request.ManualMapping.Chapter;
            doc.MappedStandard = request.ManualMapping.Standard;
            doc.MappedObjectiveElement = request.ManualMapping.ObjectiveElement;
            doc.MappedObjectiveElementId = request.ManualMapping.ObjectiveElementId;
        }
        else if (request.AcceptMappingObjectiveElementId.HasValue)
        {
            doc.MappedObjectiveElementId = request.AcceptMappingObjectiveElementId;
        }

        var version = doc.Versions.First(v => v.Id == doc.CurrentVersionId);
        var sections = await _db.DocumentSections
            .Where(s => s.DocumentVersionId == version.Id)
            .ToListAsync(ct);

        if (request.Sections?.Count > 0)
        {
            foreach (var patch in request.Sections)
            {
                var section = sections.FirstOrDefault(s => s.Id == patch.Id);
                if (section == null) continue;
                if (patch.IsCollapsed.HasValue) section.IsCollapsed = patch.IsCollapsed.Value;
                if (patch.Blocks != null)
                {
                    section.BlocksJson = JsonSerializer.Serialize(patch.Blocks, JsonOpts);
                    section.LastEditedAt = DateTime.UtcNow;
                }
            }

            version.Content = ComposeMarkdown(doc.Title, sections);
        }

        doc.LastUpdatedAt = DateTime.UtcNow;
        doc.LastUpdatedByUserId = userId;

        if (request.CreateCheckpoint)
        {
            var snapshot = JsonSerializer.Serialize(new
            {
                doc.Title,
                doc.Department,
                Sections = sections.Select(s => new { s.Id, s.SectionKey, s.Title, s.BlocksJson, s.IsCollapsed }),
            }, JsonOpts);

            _db.DocumentAuthoringCheckpoints.Add(new DocumentAuthoringCheckpoint
            {
                Id = Guid.NewGuid(),
                ControlledDocumentId = doc.Id,
                DocumentVersionId = version.Id,
                SnapshotJson = snapshot,
                Label = request.CheckpointLabel ?? "Autosave",
                CreatedByUserId = userId,
                CreatedByName = userName,
                CreatedAt = DateTime.UtcNow,
            });
        }

        _db.DocumentLifecycleAuditEntries.Add(new DocumentLifecycleAuditEntry
        {
            Id = Guid.NewGuid(),
            ControlledDocumentId = doc.Id,
            DocumentVersionId = version.Id,
            EventType = "AuthoringSaved",
            PerformedByUserId = userId,
            PerformedByName = userName,
            Details = request.CreateCheckpoint ? "Checkpoint saved" : "Autosave",
            CreatedAt = DateTime.UtcNow,
        });

        await _db.SaveChangesAsync(ct);
        return await BuildSessionAsync(organizationId, documentId, ct);
    }

    public async Task<StructuredCompareResultDto?> CompareStructuredAsync(
        Guid organizationId, StructuredCompareRequest request, CancellationToken ct = default)
    {
        var versions = await _db.DocumentVersions.AsNoTracking()
            .Include(v => v.Sections)
            .Where(v => v.ControlledDocumentId == request.DocumentId
                        && v.ControlledDocument.TenantId == organizationId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);

        if (versions.Count == 0) return null;

        var current = request.VersionId.HasValue
            ? versions.FirstOrDefault(v => v.Id == request.VersionId.Value) ?? versions[0]
            : versions.FirstOrDefault(v => v.IsCurrent) ?? versions[0];

        var previous = versions.FirstOrDefault(v => v.VersionNumber < current.VersionNumber);
        var diffs = DiffSections(previous?.Sections.OrderBy(s => s.SortOrder).ToList() ?? [],
            current.Sections.OrderBy(s => s.SortOrder).ToList());

        var summary = BuildCompareSummary(diffs);
        return new StructuredCompareResultDto
        {
            CurrentVersionId = current.Id,
            PreviousVersionId = previous?.Id,
            SectionDiffs = diffs,
            AiSummary = summary,
        };
    }

    public Task<AuthoringAiDraftResponse> DraftAiAsync(
        Guid organizationId, Guid userId, AuthoringAiDraftRequest request, CancellationToken ct = default) =>
        GenerateAiBlocksAsync(organizationId, request, ct);

    public async Task<AuthoringCommentDto?> AddCommentAsync(
        Guid organizationId, Guid userId, string? userName,
        CreateAuthoringCommentRequest request, CancellationToken ct = default)
    {
        var doc = await _db.ControlledDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == request.DocumentId, ct);
        if (doc?.CurrentVersionId == null) return null;

        var comment = new DocumentAuthoringComment
        {
            Id = Guid.NewGuid(),
            ControlledDocumentId = doc.Id,
            DocumentVersionId = doc.CurrentVersionId.Value,
            SectionId = request.SectionId,
            BlockId = request.BlockId,
            CommentType = request.CommentType,
            Content = request.Content.Trim(),
            AuthorUserId = userId,
            AuthorName = userName,
            MentionedUserName = request.MentionedUserName,
            CreatedAt = DateTime.UtcNow,
        };

        _db.DocumentAuthoringComments.Add(comment);
        await _db.SaveChangesAsync(ct);

        return new AuthoringCommentDto
        {
            Id = comment.Id,
            SectionId = comment.SectionId,
            BlockId = comment.BlockId,
            CommentType = comment.CommentType,
            Content = comment.Content,
            AuthorName = comment.AuthorName,
            MentionedUserName = comment.MentionedUserName,
            IsResolved = false,
            CreatedAt = comment.CreatedAt,
        };
    }

    public async Task<CrossReferenceDto?> AddCrossReferenceAsync(
        Guid organizationId, AddCrossReferenceRequest request, CancellationToken ct = default)
    {
        var doc = await _db.ControlledDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == request.DocumentId, ct);
        if (doc == null) return null;

        var xref = new DocumentCrossReference
        {
            Id = Guid.NewGuid(),
            ControlledDocumentId = doc.Id,
            SectionId = request.SectionId,
            BlockId = request.BlockId,
            ReferenceType = request.ReferenceType,
            TargetId = request.TargetId,
            TargetLabel = request.TargetLabel,
            TargetRoute = request.TargetRoute,
            CreatedAt = DateTime.UtcNow,
        };

        _db.DocumentCrossReferences.Add(xref);
        await _db.SaveChangesAsync(ct);

        return new CrossReferenceDto
        {
            Id = xref.Id,
            SectionId = xref.SectionId,
            BlockId = xref.BlockId,
            ReferenceType = xref.ReferenceType,
            TargetId = xref.TargetId,
            TargetLabel = xref.TargetLabel,
            TargetRoute = xref.TargetRoute,
        };
    }

    public async Task<DocumentExportResultDto?> ExportAsync(
        Guid organizationId, DocumentExportRequest request, CancellationToken ct = default)
    {
        var session = await BuildSessionAsync(organizationId, request.DocumentId, ct);
        if (session == null) return null;

        var safeName = SanitizeFileName(session.DocumentNumber + "-" + session.Title);
        return request.Format switch
        {
            DocumentExportFormat.Pdf => ExportPdf(session, safeName),
            DocumentExportFormat.Docx => ExportDocx(session, safeName),
            DocumentExportFormat.Html => ExportHtml(session, safeName),
            DocumentExportFormat.Markdown => ExportMarkdown(session, safeName),
            _ => ExportPdf(session, safeName),
        };
    }

    private async Task<AuthoringSessionDto?> BuildSessionAsync(
        Guid organizationId, Guid documentId, CancellationToken ct)
    {
        var doc = await _db.ControlledDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == documentId, ct);
        if (doc?.CurrentVersionId == null) return null;

        var version = await _db.DocumentVersions.AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == doc.CurrentVersionId, ct);
        if (version == null) return null;

        var sections = await _db.DocumentSections.AsNoTracking()
            .Where(s => s.DocumentVersionId == version.Id)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

        if (sections.Count == 0)
        {
            var schemaKey = version.AuthoringSchemaKey ?? AuthoringSchemaRegistry.ResolveSchemaKey(doc.DocumentType);
            var seeded = BuildInitialSections(version.Id, schemaKey, doc.Title);
            foreach (var s in seeded) _db.DocumentSections.Add(s);
            await _db.SaveChangesAsync(ct);
            sections = seeded;
        }

        var comments = await _db.DocumentAuthoringComments.AsNoTracking()
            .Where(c => c.ControlledDocumentId == doc.Id && c.DocumentVersionId == version.Id)
            .OrderByDescending(c => c.CreatedAt)
            .Take(100)
            .ToListAsync(ct);

        var xrefs = await _db.DocumentCrossReferences.AsNoTracking()
            .Where(x => x.ControlledDocumentId == doc.Id)
            .ToListAsync(ct);

        var checkpoints = await _db.DocumentAuthoringCheckpoints.AsNoTracking()
            .Where(c => c.ControlledDocumentId == doc.Id)
            .OrderByDescending(c => c.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        var sectionDtos = sections.Select(MapSection).ToList();
        var health = ComputeHealth(doc, sectionDtos, comments, xrefs);
        var suggestions = SuggestMappings(doc, sectionDtos);

        return new AuthoringSessionDto
        {
            DocumentId = doc.Id,
            VersionId = version.Id,
            DocumentNumber = doc.DocumentNumber,
            Title = doc.Title,
            DocumentType = doc.DocumentType,
            Status = doc.Status,
            AuthoringSchemaKey = version.AuthoringSchemaKey ?? AuthoringSchemaRegistry.ResolveSchemaKey(doc.DocumentType),
            Department = doc.Department,
            MappedFramework = doc.MappedFramework,
            MappedChapter = doc.MappedChapter,
            MappedStandard = doc.MappedStandard,
            MappedObjectiveElement = doc.MappedObjectiveElement,
            MappedObjectiveElementId = doc.MappedObjectiveElementId,
            Sections = sectionDtos,
            Health = health,
            MappingSuggestions = suggestions,
            Comments = comments.Select(c => new AuthoringCommentDto
            {
                Id = c.Id,
                SectionId = c.SectionId,
                BlockId = c.BlockId,
                CommentType = c.CommentType,
                Content = c.Content,
                AuthorName = c.AuthorName,
                MentionedUserName = c.MentionedUserName,
                IsResolved = c.IsResolved,
                CreatedAt = c.CreatedAt,
            }).ToList(),
            CrossReferences = xrefs.Select(x => new CrossReferenceDto
            {
                Id = x.Id,
                SectionId = x.SectionId,
                BlockId = x.BlockId,
                ReferenceType = x.ReferenceType,
                TargetId = x.TargetId,
                TargetLabel = x.TargetLabel,
                TargetRoute = x.TargetRoute,
            }).ToList(),
            Checkpoints = checkpoints.Select(c => new AuthoringCheckpointDto
            {
                Id = c.Id,
                Label = c.Label,
                CreatedByName = c.CreatedByName,
                CreatedAt = c.CreatedAt,
            }).ToList(),
            LastSavedAt = doc.LastUpdatedAt,
        };
    }

    private static List<DocumentSection> BuildInitialSections(Guid versionId, string schemaKey, string title)
    {
        var defs = AuthoringSchemaRegistry.GetSections(schemaKey);
        return defs.Select(d => new DocumentSection
        {
            Id = Guid.NewGuid(),
            DocumentVersionId = versionId,
            SectionKey = d.Key,
            Title = d.Title,
            SortOrder = d.SortOrder,
            IsRequired = d.IsRequired,
            BlocksJson = JsonSerializer.Serialize(CreateDefaultBlocks(d.Key, title), JsonOpts),
            LastEditedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        }).ToList();
    }

    private static List<AuthoringBlockDto> CreateDefaultBlocks(string sectionKey, string title)
    {
        var intro = sectionKey switch
        {
            "purpose" => $"This document defines the purpose of {title}.",
            "scope" => $"This document applies to all relevant personnel and processes for {title}.",
            "procedure" => "Describe the step-by-step procedure below.",
            _ => string.Empty,
        };

        var blocks = new List<AuthoringBlockDto>();
        if (!string.IsNullOrEmpty(intro))
        {
            blocks.Add(new AuthoringBlockDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Type = "paragraph",
                Data = JsonSerializer.SerializeToElement(new { text = intro }, JsonOpts),
            });
        }
        else
        {
            blocks.Add(new AuthoringBlockDto
            {
                Id = Guid.NewGuid().ToString("N"),
                Type = "paragraph",
                Data = JsonSerializer.SerializeToElement(new { text = string.Empty }, JsonOpts),
            });
        }

        return blocks;
    }

    private static AuthoringSectionDto MapSection(DocumentSection s)
    {
        IReadOnlyList<AuthoringBlockDto> blocks;
        try
        {
            blocks = JsonSerializer.Deserialize<List<AuthoringBlockDto>>(s.BlocksJson, JsonOpts) ?? [];
        }
        catch
        {
            blocks = [];
        }

        return new AuthoringSectionDto
        {
            Id = s.Id,
            SectionKey = s.SectionKey,
            Title = s.Title,
            SortOrder = s.SortOrder,
            IsRequired = s.IsRequired,
            IsCollapsed = s.IsCollapsed,
            Blocks = blocks,
            LastEditedAt = s.LastEditedAt,
        };
    }

    private static string ComposeMarkdown(string title, IEnumerable<DocumentSection> sections)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# ").AppendLine(title);
        foreach (var section in sections.OrderBy(s => s.SortOrder))
        {
            sb.AppendLine().AppendLine("## ").AppendLine(section.Title);
            sb.AppendLine(ExtractPlainText(section.BlocksJson));
        }

        return sb.ToString();
    }

    private static string ExtractPlainText(string blocksJson)
    {
        try
        {
            var blocks = JsonSerializer.Deserialize<List<AuthoringBlockDto>>(blocksJson, JsonOpts) ?? [];
            return string.Join("\n", blocks.Select(b => BlockToPlainText(b)));
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string BlockToPlainText(AuthoringBlockDto block)
    {
        if (block.Data.TryGetProperty("text", out var text)) return text.GetString() ?? string.Empty;
        if (block.Data.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
            return string.Join("\n", items.EnumerateArray().Select(i => "• " + (i.GetString() ?? string.Empty)));
        return block.Type;
    }

    private static DocumentHealthScoreDto ComputeHealth(
        ControlledDocument doc,
        IReadOnlyList<AuthoringSectionDto> sections,
        IReadOnlyList<DocumentAuthoringComment> comments,
        IReadOnlyList<DocumentCrossReference> xrefs)
    {
        var required = sections.Where(s => s.IsRequired).ToList();
        var filled = required.Count(s => s.Blocks.Any(b => !string.IsNullOrWhiteSpace(BlockToPlainText(b))));
        var completeness = required.Count > 0 ? (int)Math.Round((double)filled / required.Count * 100) : 100;

        var wordCounts = sections.SelectMany(s => s.Blocks)
            .Select(BlockToPlainText)
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length)
            .ToList();
        var avgWords = wordCounts.Count > 0 ? wordCounts.Average() : 0;
        var readability = avgWords switch
        {
            < 8 => 95,
            < 15 => 85,
            < 25 => 75,
            _ => 65,
        };

        var unresolved = comments.Count(c => !c.IsResolved);
        var missing = required.Where(s => !s.Blocks.Any(b => !string.IsNullOrWhiteSpace(BlockToPlainText(b))))
            .Select(s => s.Title).ToList();
        var approvalReady = missing.Count == 0 && unresolved == 0 ? 100 : Math.Max(0, 100 - missing.Count * 15 - unresolved * 5);

        var oeMapped = doc.MappedObjectiveElementId.HasValue ? 1 : 0;
        var overall = (completeness + readability + approvalReady) / 3;

        return new DocumentHealthScoreDto
        {
            CompletenessPercent = completeness,
            ReadabilityScore = readability,
            ApprovalReadinessPercent = approvalReady,
            OverallHealthScore = overall,
            MissingSections = missing,
            MappedObjectiveElements = oeMapped,
            UnresolvedComments = unresolved,
            LinkedEvidenceCount = xrefs.Count(x => x.ReferenceType == DocumentCrossReferenceType.Evidence),
            LinkedCapaCount = xrefs.Count(x => x.ReferenceType == DocumentCrossReferenceType.Capa),
            LinkedFindingCount = xrefs.Count(x => x.ReferenceType == DocumentCrossReferenceType.Finding),
        };
    }

    private static IReadOnlyList<ComplianceMappingSuggestionDto> SuggestMappings(
        ControlledDocument doc, IReadOnlyList<AuthoringSectionDto> sections)
    {
        if (doc.MappedObjectiveElementId.HasValue) return [];

        var text = (doc.Title + " " + string.Join(' ', sections.SelectMany(s => s.Blocks.Select(BlockToPlainText))))
            .ToLowerInvariant();

        var suggestions = new List<ComplianceMappingSuggestionDto>();
        if (text.Contains("hand hygiene") || text.Contains("infection"))
        {
            suggestions.Add(new ComplianceMappingSuggestionDto
            {
                Framework = "NABH",
                Chapter = "COP",
                Standard = "COP.13",
                ObjectiveElement = "COP.13.d",
                Confidence = 0.88,
                Reason = "Infection control / hand hygiene keywords detected",
            });
        }

        if (text.Contains("medication") || text.Contains("drug"))
        {
            suggestions.Add(new ComplianceMappingSuggestionDto
            {
                Framework = "NABH",
                Chapter = "MOM",
                Standard = "MOM.6",
                ObjectiveElement = "MOM.6.e",
                Confidence = 0.82,
                Reason = "Medication management keywords detected",
            });
        }

        if (text.Contains("fire") || text.Contains("emergency"))
        {
            suggestions.Add(new ComplianceMappingSuggestionDto
            {
                Framework = "NABH",
                Chapter = "FMS",
                Standard = "FMS.3",
                ObjectiveElement = "FMS.3.c",
                Confidence = 0.79,
                Reason = "Safety / emergency preparedness keywords detected",
            });
        }

        return suggestions;
    }

    private async Task<AuthoringAiDraftResponse> GenerateAiBlocksAsync(
        Guid organizationId, AuthoringAiDraftRequest request, CancellationToken ct)
    {
        var doc = await _db.ControlledDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.TenantId == organizationId && d.Id == request.DocumentId, ct);
        var section = await _db.DocumentSections.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SectionId, ct);

        if (doc == null || section == null)
            return new AuthoringAiDraftResponse { Explanation = "Document or section not found." };

        var action = request.Action.ToLowerInvariant();
        var blocks = new List<AuthoringBlockDto>();
        var explanation = "AI draft generated (advisory only — human approval required).";

        switch (action)
        {
            case "draft-purpose":
                blocks.Add(MakeParagraph($"The purpose of {doc.Title} is to establish standardized practices that support patient safety, regulatory compliance, and consistent quality of care."));
                break;
            case "draft-scope":
                blocks.Add(MakeParagraph($"This document applies to all clinical and support departments involved in {doc.Title.ToLowerInvariant()} at this hospital."));
                blocks.Add(MakeCallout("nabhNote", "Ensure scope aligns with applicable NABH objective elements."));
                break;
            case "rewrite":
            case "improve-readability":
                var existing = ExtractPlainText(section.BlocksJson);
                blocks.Add(MakeParagraph(string.IsNullOrWhiteSpace(existing)
                    ? $"Clear, professional language for {section.Title}."
                    : existing.Trim() + " (Revised for clarity and professional tone.)"));
                break;
            case "expand-procedure":
                blocks.Add(MakeParagraph("1. Verify prerequisites and patient identification."));
                blocks.Add(MakeParagraph("2. Perform the procedure following standard precautions."));
                blocks.Add(MakeParagraph("3. Document completion and report deviations immediately."));
                blocks.Add(MakeHealthcareBlock("patientSafetyAlert", "Stop and escalate if patient safety is at risk."));
                break;
            case "suggest-responsibilities":
                blocks.Add(MakeBulletList(["Department Head: oversight and approval", "Clinical Staff: compliance with procedure", "Quality Team: audit and monitoring"]));
                break;
            case "generate-checklist":
                blocks.Add(MakeChecklist(["Step completed", "Documentation complete", "Deviations reported"]));
                break;
            case "suggest-references":
                blocks.Add(MakeParagraph("Refer to applicable hospital policies, NABH standards, and statutory regulations."));
                blocks.Add(MakeHealthcareBlock("legalRequirement", "Maintain records per regulatory retention requirements."));
                break;
            case "summarize":
                blocks.Add(MakeParagraph($"Summary: {doc.Title} covers {section.Title} and related controlled documentation requirements for accreditation readiness."));
                break;
            default:
                blocks.Add(MakeParagraph(request.Instructions ?? $"Draft content for {section.Title}."));
                explanation = "Section draft generated from instructions.";
                break;
        }

        if (!string.IsNullOrWhiteSpace(request.Instructions))
            explanation += " " + request.Instructions;

        return new AuthoringAiDraftResponse
        {
            SuggestedBlocks = blocks,
            Explanation = explanation,
            IsAdvisoryOnly = true,
        };
    }

    private static AuthoringBlockDto MakeParagraph(string text) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Type = "paragraph",
        Data = JsonSerializer.SerializeToElement(new { text }, JsonOpts),
    };

    private static AuthoringBlockDto MakeCallout(string variant, string text) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Type = "callout",
        Data = JsonSerializer.SerializeToElement(new { variant, text }, JsonOpts),
    };

    private static AuthoringBlockDto MakeHealthcareBlock(string variant, string text) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Type = variant,
        Data = JsonSerializer.SerializeToElement(new { text }, JsonOpts),
    };

    private static AuthoringBlockDto MakeBulletList(string[] items) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Type = "bulletList",
        Data = JsonSerializer.SerializeToElement(new { items }, JsonOpts),
    };

    private static AuthoringBlockDto MakeChecklist(string[] items) => new()
    {
        Id = Guid.NewGuid().ToString("N"),
        Type = "checklist",
        Data = JsonSerializer.SerializeToElement(new { items, checkedItems = Array.Empty<string>() }, JsonOpts),
    };

    private static List<SectionDiffDto> DiffSections(
        IReadOnlyList<DocumentSection>? before,
        IReadOnlyList<DocumentSection> after)
    {
        before ??= [];
        var diffs = new List<SectionDiffDto>();
        var beforeMap = before.ToDictionary(s => s.SectionKey);

        foreach (var section in after)
        {
            beforeMap.TryGetValue(section.SectionKey, out var prev);
            var beforeText = prev == null ? string.Empty : ExtractPlainText(prev.BlocksJson);
            var afterText = ExtractPlainText(section.BlocksJson);

            if (prev == null)
            {
                diffs.Add(new SectionDiffDto
                {
                    SectionKey = section.SectionKey,
                    Title = section.Title,
                    ChangeType = "Added",
                    BlockDiffs = [new BlockDiffDto { BlockId = section.Id.ToString(), ChangeType = "Added", AfterText = afterText }],
                });
            }
            else if (beforeText != afterText)
            {
                diffs.Add(new SectionDiffDto
                {
                    SectionKey = section.SectionKey,
                    Title = section.Title,
                    ChangeType = "Modified",
                    BlockDiffs = [new BlockDiffDto
                    {
                        BlockId = section.Id.ToString(),
                        ChangeType = "Modified",
                        BeforeText = beforeText,
                        AfterText = afterText,
                    }],
                });
            }
        }

        foreach (var removed in before.Where(b => after.All(a => a.SectionKey != b.SectionKey)))
        {
            diffs.Add(new SectionDiffDto
            {
                SectionKey = removed.SectionKey,
                Title = removed.Title,
                ChangeType = "Removed",
                BlockDiffs = [new BlockDiffDto { BlockId = removed.Id.ToString(), ChangeType = "Removed", BeforeText = ExtractPlainText(removed.BlocksJson) }],
            });
        }

        return diffs;
    }

    private static string BuildCompareSummary(IReadOnlyList<SectionDiffDto> diffs)
    {
        if (diffs.Count == 0) return "No structural changes detected between versions.";
        var added = diffs.Count(d => d.ChangeType == "Added");
        var modified = diffs.Count(d => d.ChangeType == "Modified");
        var removed = diffs.Count(d => d.ChangeType == "Removed");
        return $"What changed: {modified} section(s) modified, {added} added, {removed} removed. Review approval impact before publishing.";
    }

    private async Task<string?> ResolveTemplateSchemaAsync(Guid templateId, CancellationToken ct)
    {
        var t = await _db.DocumentTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == templateId, ct);
        return t?.AuthoringSchemaKey ?? (t != null ? AuthoringSchemaRegistry.ResolveSchemaKey(t.DocumentType) : null);
    }

    private static DocumentExportResultDto ExportPdf(AuthoringSessionDto session, string safeName)
    {
        var bytes = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text(session.Title).SemiBold().FontSize(16);
                page.Content().Column(col =>
                {
                    col.Item().Text($"{session.DocumentNumber} · {session.Status}").FontSize(10).FontColor(Colors.Grey.Darken1);
                    foreach (var section in session.Sections)
                    {
                        col.Item().PaddingTop(10).Text(section.Title).SemiBold().FontSize(12);
                        foreach (var block in section.Blocks)
                        {
                            var text = BlockToPlainText(block);
                            if (!string.IsNullOrWhiteSpace(text))
                                col.Item().Text(text).FontSize(10);
                        }
                    }
                });
                page.Footer().AlignCenter().Text(x => x.CurrentPageNumber());
            });
        }).GeneratePdf();

        return new DocumentExportResultDto
        {
            FileName = $"{safeName}.pdf",
            ContentType = "application/pdf",
            Base64Content = Convert.ToBase64String(bytes),
        };
    }

    private static DocumentExportResultDto ExportDocx(AuthoringSessionDto session, string safeName)
    {
        var html = RenderHtml(session);
        var bytes = Encoding.UTF8.GetBytes(html);
        return new DocumentExportResultDto
        {
            FileName = $"{safeName}.doc",
            ContentType = "application/msword",
            Base64Content = Convert.ToBase64String(bytes),
        };
    }

    private static DocumentExportResultDto ExportHtml(AuthoringSessionDto session, string safeName)
    {
        var html = RenderHtml(session);
        return new DocumentExportResultDto
        {
            FileName = $"{safeName}.html",
            ContentType = "text/html",
            Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(html)),
        };
    }

    private static DocumentExportResultDto ExportMarkdown(AuthoringSessionDto session, string safeName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# ").AppendLine(session.Title);
        foreach (var section in session.Sections)
        {
            sb.AppendLine().AppendLine("## ").AppendLine(section.Title);
            foreach (var block in section.Blocks)
            {
                var text = BlockToPlainText(block);
                if (!string.IsNullOrWhiteSpace(text)) sb.AppendLine(text);
            }
        }

        var md = sb.ToString();
        return new DocumentExportResultDto
        {
            FileName = $"{safeName}.md",
            ContentType = "text/markdown",
            Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(md)),
        };
    }

    private static string RenderHtml(AuthoringSessionDto session)
    {
        var sb = new StringBuilder();
        sb.Append("<html><head><meta charset='utf-8'><title>").Append(session.Title).Append("</title></head><body>");
        sb.Append("<h1>").Append(session.Title).Append("</h1>");
        sb.Append("<p><em>").Append(session.DocumentNumber).Append("</em></p>");
        foreach (var section in session.Sections)
        {
            sb.Append("<h2>").Append(section.Title).Append("</h2>");
            foreach (var block in section.Blocks)
            {
                var text = System.Net.WebUtility.HtmlEncode(BlockToPlainText(block));
                if (!string.IsNullOrWhiteSpace(text))
                    sb.Append("<p>").Append(text).Append("</p>");
            }
        }

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '-');
        return name.Length > 80 ? name[..80] : name;
    }
}
