using ComplianceService.Application.Contracts;
using ComplianceService.Application.Events;
using ComplianceService.Domain;
using ComplianceService.Domain.Enums;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Services;

/// <summary>Sprint 9B — intelligent assignment routing (Rules A/B/C).</summary>
public sealed class AssignmentRoutingService : IAssignmentRoutingService
{
    private readonly ComplianceDbContext _db;
    private readonly IDeficiencyTaskOwnershipSyncService _ownershipSync;
    private readonly IComplianceEventEmitter _eventEmitter;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignmentRoutingService> _logger;

    public AssignmentRoutingService(
        ComplianceDbContext db,
        IDeficiencyTaskOwnershipSyncService ownershipSync,
        IComplianceEventEmitter eventEmitter,
        IUnitOfWork unitOfWork,
        ILogger<AssignmentRoutingService> logger)
    {
        _db = db;
        _ownershipSync = ownershipSync;
        _eventEmitter = eventEmitter;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<AssignmentRoutingResultDto> ResolveForDeficiencyAsync(
        Guid deficiencyId,
        CancellationToken ct = default)
    {
        var deficiency = await _db.ComplianceDeficiencies
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deficiencyId, ct);

        if (deficiency == null)
        {
            return Empty("Deficiency not found.");
        }

        var oeResult = await ResolveRuleAAsync(
            deficiency.OrganizationId,
            deficiency.HospitalId,
            deficiency.ObjectiveElementId,
            ct);

        if (oeResult.Confidence >= AssignmentConfidence.Medium)
        {
            return oeResult;
        }

        if (deficiency.RootCauseCategory.HasValue && deficiency.RootCauseCategory != RootCauseCategory.None)
        {
            var rcResult = await ResolveRuleBAsync(
                deficiency.OrganizationId,
                deficiency.HospitalId,
                deficiency.RootCauseCategory.Value,
                ct);

            if (rcResult.Confidence > AssignmentConfidence.None)
            {
                return rcResult;
            }
        }

        return oeResult.Confidence > AssignmentConfidence.None ? oeResult : Empty("No routing rule matched.");
    }

    public async Task<AssignmentRoutingResultDto> ResolveForObjectiveElementAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid objectiveElementId,
        CancellationToken ct = default) =>
        await ResolveRuleAAsync(organizationId, hospitalId, objectiveElementId, ct);

    public async Task<AssignmentRoutingResultDto> ResolveForFindingAsync(
        Guid findingId,
        Guid organizationId,
        Guid hospitalId,
        CancellationToken ct = default)
    {
        var finding = await _db.Findings
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == findingId && f.TenantId == organizationId, ct);

        if (finding == null)
        {
            return Empty("Finding not found.");
        }

        var ruleC = ResolveRuleC(finding);
        if (ruleC.Confidence > AssignmentConfidence.None && !string.IsNullOrWhiteSpace(ruleC.NormalizedDepartmentCode))
        {
            var dept = await ResolveDepartmentByNormalizedCodeAsync(
                organizationId,
                hospitalId,
                ruleC.NormalizedDepartmentCode!,
                ct);

            if (dept != null)
            {
                var owner = await ResolveDepartmentOwnerAsync(organizationId, hospitalId, dept.Id, ct);
                ruleC.RecommendedDepartmentId = dept.Id;
                ruleC.RecommendedDepartmentName = dept.Name;
                ruleC.RecommendedAssigneeUserId = owner?.PrimaryOwnerUserId ?? owner?.BackupOwnerUserId;
                ruleC.CanAutoAssign = ruleC.Confidence == AssignmentConfidence.High
                    && !string.IsNullOrWhiteSpace(ruleC.RecommendedAssigneeUserId);
            }
        }

        if (ruleC.Confidence >= AssignmentConfidence.Medium)
        {
            return ruleC;
        }

        if (finding.ObjectiveElementId.HasValue)
        {
            return await ResolveRuleAAsync(
                organizationId,
                hospitalId,
                finding.ObjectiveElementId.Value,
                ct);
        }

        return ruleC.Confidence > AssignmentConfidence.None ? ruleC : Empty("No routing rule matched for finding.");
    }

    public async Task<AssignmentRoutingApplyResultDto> TryAutoAssignDeficiencyAsync(
        Guid deficiencyId,
        bool forceAutoAssign = false,
        string? updatedBy = null,
        CancellationToken ct = default)
    {
        var suggestion = await ResolveForDeficiencyAsync(deficiencyId, ct);

        await EmitRecommendationEventAsync(deficiencyId, suggestion, ct);
        await LogRoutingDecisionAsync(deficiencyId, suggestion, applied: false, autoAssigned: false, ct);

        var result = new AssignmentRoutingApplyResultDto { Suggestion = suggestion };

        var shouldApply = (forceAutoAssign || suggestion.CanAutoAssign)
            && !string.IsNullOrWhiteSpace(suggestion.RecommendedAssigneeUserId);

        if (!shouldApply)
        {
            return result;
        }

        await _ownershipSync.SetPrimaryOwnerAsync(
            deficiencyId,
            suggestion.RecommendedAssigneeUserId!,
            null,
            suggestion.RecommendedDepartmentName,
            suggestion.RecommendedDepartmentId,
            updatedBy ?? "assignment-routing",
            ct);

        result.WasApplied = true;
        result.WasAutoAssigned = suggestion.CanAutoAssign || forceAutoAssign;
        result.PrimaryRemediationTaskId = (await _ownershipSync.GetPrimaryRemediationTaskAsync(deficiencyId, ct))?.Id;

        await LogRoutingDecisionAsync(
            deficiencyId,
            suggestion,
            applied: true,
            autoAssigned: result.WasAutoAssigned,
            ct,
            appliedUserId: suggestion.RecommendedAssigneeUserId);

        _logger.LogInformation(
            "AssignmentRouting: Auto-assigned deficiency {DeficiencyId} to {User} via {Rule}.",
            deficiencyId,
            suggestion.RecommendedAssigneeUserId,
            suggestion.RuleAppliedLabel);

        return result;
    }

    public async Task<AssignmentRoutingMetricsDto> GetMetricsAsync(
        Guid organizationId,
        CancellationToken ct = default)
    {
        var logs = await _db.AssignmentRoutingLogs
            .AsNoTracking()
            .Where(l => l.OrganizationId == organizationId)
            .ToListAsync(ct);

        var total = logs.Count;
        var auto = logs.Count(l => l.WasAutoAssigned);
        var overridden = logs.Count(l => l.WasOverridden);
        var high = logs.Count(l => l.Confidence == AssignmentConfidence.High);
        var applied = logs.Count(l => l.AppliedUserId != null);

        return new AssignmentRoutingMetricsDto
        {
            TotalRoutingDecisions = total,
            AutoAssignedCount = auto,
            ManualOverrideCount = overridden,
            HighConfidenceCount = high,
            AutoAssignmentRate = total > 0 ? (double)auto / total : 0,
            RoutingSuccessRate = total > 0 ? (double)applied / total : 0,
            ByRule = logs.GroupBy(l => l.RuleApplied.ToString())
                .Select(g => new AssignmentCountByLabelDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList(),
        };
    }

    private async Task<AssignmentRoutingResultDto> ResolveRuleAAsync(
        Guid organizationId,
        Guid hospitalId,
        Guid objectiveElementId,
        CancellationToken ct)
    {
        var oe = await _db.ObjectiveElements
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == objectiveElementId, ct);

        if (oe == null || string.IsNullOrWhiteSpace(oe.Department))
        {
            return Empty("Objective element has no department metadata.");
        }

        await EnsureLabelMappingsSeededAsync(ct);

        var mapping = await _db.DepartmentLabelMappings
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.OeDepartmentLabel == oe.Department && m.IsActive, ct);

        var normalizedCode = mapping?.NormalizedCode
            ?? DepartmentLabelCatalog.Mappings
                .FirstOrDefault(m => m.Label == oe.Department).Code;

        if (string.IsNullOrWhiteSpace(normalizedCode))
        {
            return new AssignmentRoutingResultDto
            {
                RuleApplied = AssignmentRoutingRuleType.ObjectiveElementDepartment,
                RuleAppliedLabel = "Rule A — OE Department",
                RoutingReason = $"OE department '{oe.Department}' has no normalized mapping.",
                Confidence = AssignmentConfidence.Low,
            };
        }

        var dept = await ResolveDepartmentByNormalizedCodeAsync(organizationId, hospitalId, normalizedCode, ct);
        if (dept == null)
        {
            dept = await EnsureComplianceDepartmentAsync(organizationId, normalizedCode, oe.Department, ct);
        }

        var owner = dept != null
            ? await ResolveDepartmentOwnerAsync(organizationId, hospitalId, dept.Id, ct)
            : null;

        var assignee = owner?.PrimaryOwnerUserId ?? owner?.BackupOwnerUserId;
        var role = mapping?.DefaultResponsibleRole ?? oe.ResponsibleRole ?? "department owner";

        return new AssignmentRoutingResultDto
        {
            RecommendedAssigneeUserId = assignee,
            RecommendedDepartmentId = dept?.Id,
            RecommendedDepartmentName = dept?.Name ?? oe.Department,
            NormalizedDepartmentCode = normalizedCode,
            RuleApplied = AssignmentRoutingRuleType.ObjectiveElementDepartment,
            RuleAppliedLabel = "Rule A — OE Department",
            RoutingReason = assignee != null
                ? $"OE '{oe.Code}' → {oe.Department} → {dept?.Name} → configured owner"
                : $"OE '{oe.Code}' → {oe.Department} → {dept?.Name}; assign {role} (no owner configured)",
            Confidence = assignee != null ? AssignmentConfidence.High : AssignmentConfidence.Medium,
            CanAutoAssign = assignee != null,
        };
    }

    private async Task<AssignmentRoutingResultDto> ResolveRuleBAsync(
        Guid organizationId,
        Guid hospitalId,
        RootCauseCategory rootCause,
        CancellationToken ct)
    {
        var normalizedCode = rootCause switch
        {
            RootCauseCategory.TrainingMissing => "HUMAN_RESOURCES",
            RootCauseCategory.PolicyMissing => "QUALITY_MANAGEMENT",
            RootCauseCategory.ProcessFailure => "QUALITY_MANAGEMENT",
            RootCauseCategory.VerificationFailure => "QUALITY_MANAGEMENT",
            RootCauseCategory.DocumentationIssue => "QUALITY_MANAGEMENT",
            _ => (string?)null,
        };

        if (normalizedCode == null)
        {
            return Empty($"Root cause {rootCause} has no department routing.");
        }

        var dept = await ResolveDepartmentByNormalizedCodeAsync(organizationId, hospitalId, normalizedCode, ct);
        var owner = dept != null
            ? await ResolveDepartmentOwnerAsync(organizationId, hospitalId, dept.Id, ct)
            : null;

        var assignee = owner?.PrimaryOwnerUserId ?? owner?.BackupOwnerUserId;

        return new AssignmentRoutingResultDto
        {
            RecommendedAssigneeUserId = assignee,
            RecommendedDepartmentId = dept?.Id,
            RecommendedDepartmentName = dept?.Name,
            NormalizedDepartmentCode = normalizedCode,
            RuleApplied = AssignmentRoutingRuleType.RootCauseCategory,
            RuleAppliedLabel = "Rule B — Root Cause",
            RoutingReason = $"Root cause {rootCause} → {normalizedCode}",
            Confidence = assignee != null ? AssignmentConfidence.Medium : AssignmentConfidence.Low,
            CanAutoAssign = false,
        };
    }

    private static AssignmentRoutingResultDto ResolveRuleC(Finding finding)
    {
        var (code, reason, confidence) = finding.RaisedBy switch
        {
            FindingRaisedBy.InternalAudit or FindingRaisedBy.ExternalAudit or FindingRaisedBy.ComplianceTeam
                => ("QUALITY_MANAGEMENT", "Audit finding → Quality team", AssignmentConfidence.Medium),
            FindingRaisedBy.IncidentReview
                => ("MANAGEMENT", "Incident review → Department head / Management", AssignmentConfidence.Low),
            FindingRaisedBy.DepartmentAssessment
                => (MapFindingDepartment(finding.Department), $"Department assessment → {finding.Department}", AssignmentConfidence.Low),
            _ => (MapFindingDepartment(finding.Department), $"Finding source → {finding.RaisedBy}", AssignmentConfidence.Low),
        };

        return new AssignmentRoutingResultDto
        {
            NormalizedDepartmentCode = code,
            RuleApplied = AssignmentRoutingRuleType.FindingSource,
            RuleAppliedLabel = "Rule C — Finding Source",
            RoutingReason = reason,
            Confidence = confidence,
            CanAutoAssign = false,
        };
    }

    private static string MapFindingDepartment(FindingDepartment dept) =>
        dept switch
        {
            FindingDepartment.HR => "HUMAN_RESOURCES",
            FindingDepartment.Security => "FACILITY_ENGINEERING",
            FindingDepartment.Clinical => "CLINICAL_NURSING",
            _ => "QUALITY_MANAGEMENT",
        };

    private async Task<ComplianceDepartment?> ResolveDepartmentByNormalizedCodeAsync(
        Guid organizationId,
        Guid hospitalId,
        string normalizedCode,
        CancellationToken ct)
    {
        return await _db.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(
                d => d.OrganizationId == organizationId && d.Code == normalizedCode && d.IsActive,
                ct);
    }

    private async Task<ComplianceDepartment> EnsureComplianceDepartmentAsync(
        Guid organizationId,
        string code,
        string name,
        CancellationToken ct)
    {
        var existing = await _db.Departments
            .FirstOrDefaultAsync(d => d.OrganizationId == organizationId && d.Code == code, ct);

        if (existing != null)
        {
            return existing;
        }

        var dept = new ComplianceDepartment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Code = code,
            Name = name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        };

        await _db.Departments.AddAsync(dept, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return dept;
    }

    private async Task<DepartmentOwnership?> ResolveDepartmentOwnerAsync(
        Guid organizationId,
        Guid? hospitalId,
        Guid departmentId,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var hospitalSpecific = hospitalId.HasValue
            ? await _db.DepartmentOwnerships
                .AsNoTracking()
                .Where(o => o.OrganizationId == organizationId
                            && o.HospitalId == hospitalId
                            && o.DepartmentId == departmentId
                            && o.Status == DepartmentOwnershipStatus.Active
                            && o.EffectiveFrom <= now
                            && (o.EffectiveTo == null || o.EffectiveTo > now))
                .OrderByDescending(o => o.EffectiveFrom)
                .FirstOrDefaultAsync(ct)
            : null;

        if (hospitalSpecific != null)
        {
            return hospitalSpecific;
        }

        return await _db.DepartmentOwnerships
            .AsNoTracking()
            .Where(o => o.OrganizationId == organizationId
                        && o.HospitalId == null
                        && o.DepartmentId == departmentId
                        && o.Status == DepartmentOwnershipStatus.Active
                        && o.EffectiveFrom <= now
                        && (o.EffectiveTo == null || o.EffectiveTo > now))
            .OrderByDescending(o => o.EffectiveFrom)
            .FirstOrDefaultAsync(ct);
    }

    private async Task EnsureLabelMappingsSeededAsync(CancellationToken ct)
    {
        if (await _db.DepartmentLabelMappings.AnyAsync(ct))
        {
            return;
        }

        foreach (var (label, code, role) in DepartmentLabelCatalog.Mappings)
        {
            await _db.DepartmentLabelMappings.AddAsync(new DepartmentLabelMapping
            {
                Id = Guid.NewGuid(),
                OeDepartmentLabel = label,
                NormalizedCode = code,
                DefaultResponsibleRole = role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                RowVersion = new byte[8],
            }, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task EmitRecommendationEventAsync(
        Guid deficiencyId,
        AssignmentRoutingResultDto suggestion,
        CancellationToken ct)
    {
        var deficiency = await _db.ComplianceDeficiencies.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deficiencyId, ct);

        if (deficiency == null)
        {
            return;
        }

        await _eventEmitter.EmitAsync(
            ComplianceEventType.RoutingRecommendationGenerated,
            deficiencyId,
            nameof(ComplianceDeficiency),
            deficiency.OrganizationId,
            null,
            new
            {
                suggestion.RecommendedAssigneeUserId,
                suggestion.RecommendedDepartmentName,
                suggestion.RuleAppliedLabel,
                suggestion.RoutingReason,
                Confidence = suggestion.Confidence.ToString(),
            },
            ct);

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task LogRoutingDecisionAsync(
        Guid deficiencyId,
        AssignmentRoutingResultDto suggestion,
        bool applied,
        bool autoAssigned,
        CancellationToken ct,
        string? appliedUserId = null)
    {
        var deficiency = await _db.ComplianceDeficiencies.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == deficiencyId, ct);

        if (deficiency == null)
        {
            return;
        }

        await _db.AssignmentRoutingLogs.AddAsync(new AssignmentRoutingLog
        {
            Id = Guid.NewGuid(),
            OrganizationId = deficiency.OrganizationId,
            HospitalId = deficiency.HospitalId,
            EntityType = nameof(ComplianceDeficiency),
            EntityId = deficiencyId,
            RuleApplied = suggestion.RuleApplied,
            RecommendedUserId = suggestion.RecommendedAssigneeUserId,
            AppliedUserId = appliedUserId,
            Confidence = suggestion.Confidence,
            RoutingReason = suggestion.RoutingReason,
            WasAutoAssigned = autoAssigned,
            WasOverridden = false,
            CreatedAt = DateTime.UtcNow,
            RowVersion = new byte[8],
        }, ct);

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static AssignmentRoutingResultDto Empty(string reason) =>
        new()
        {
            RuleApplied = AssignmentRoutingRuleType.None,
            RuleAppliedLabel = "None",
            RoutingReason = reason,
            Confidence = AssignmentConfidence.None,
        };
}
