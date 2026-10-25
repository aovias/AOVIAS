using AIOrchestratorService.Application.AI.Identity.Contracts;
using AIOrchestratorService.Application.AI.Prompting;
using AIOrchestratorService.Domain.Entities;
using AIOrchestratorService.Domain.Enums;
using AIOrchestratorService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AIOrchestratorService.Application.AI.Identity.Services;

public sealed class AIProfileService(AIOrchestratorDbContext db) : IAIProfileService
{
    public async Task<IReadOnlyList<AIProfileDto>> ListAsync(CancellationToken ct = default)
        => await db.AIAssistantProfiles.AsNoTracking().Where(p => !p.IsDeleted)
            .OrderBy(p => p.Name).Select(p => Map(p, null)).ToListAsync(ct);

    public async Task<AIProfileDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var p = await db.AIAssistantProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (p == null) return null;
        var identity = await db.AIIdentities.AsNoTracking().FirstOrDefaultAsync(i => i.ProfileId == id, ct);
        return Map(p, identity?.PlatformId);
    }

    public async Task<AIProfileDto?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var p = await db.AIAssistantProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code && !x.IsDeleted, ct);
        return p == null ? null : Map(p, null);
    }

    public async Task<AIProfileDto?> GetByCopilotPersonaAsync(string personaKey, CancellationToken ct = default)
    {
        var p = await db.AIAssistantProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.CopilotPersonaKey == personaKey && !x.IsDeleted && x.Status == AIProfileStatus.Active, ct);
        return p == null ? null : Map(p, null);
    }

    public async Task<AIProfileDto> UpsertAsync(UpsertAIProfileRequest request, CancellationToken ct = default)
    {
        var existing = await db.AIAssistantProfiles.FirstOrDefaultAsync(p => p.Code == request.Code, ct);
        if (existing == null)
        {
            existing = new AIAssistantProfile
            {
                Id = Guid.NewGuid(), Code = request.Code, CreatedAt = DateTime.UtcNow
            };
            db.AIAssistantProfiles.Add(existing);
            var platformId = $"AI-{existing.Id:N}";
            db.AIIdentities.Add(new AIIdentity
            {
                Id = Guid.NewGuid(), ProfileId = existing.Id, PlatformId = platformId,
                DisplayName = request.Name, LifecycleStatus = AIIdentityLifecycleStatus.Active, CreatedAt = DateTime.UtcNow
            });
            db.AIOutboxMessages.Add(new AIOutboxMessage { Id = Guid.NewGuid(), AggregateId = existing.Id, Type = "AIProfileCreated", Payload = JsonSerializer.Serialize(new { existing.Id, request.Code }), OccurredOn = DateTime.UtcNow });
        }
        existing.Name = request.Name;
        existing.Description = request.Description;
        existing.ModuleCode = request.ModuleCode;
        existing.CopilotPersonaKey = request.CopilotPersonaKey;
        existing.Status = request.Publish ? AIProfileStatus.Active : AIProfileStatus.Draft;
        existing.Version++;
        existing.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        return Map(existing, null);
    }

    private static AIProfileDto Map(AIAssistantProfile p, string? platformId) => new()
    {
        Id = p.Id, Code = p.Code, Name = p.Name, Description = p.Description, ModuleCode = p.ModuleCode,
        Status = p.Status.ToString(), Version = p.Version, CopilotPersonaKey = p.CopilotPersonaKey, PlatformId = platformId
    };
}

public sealed class AIAutomationPolicyService(AIOrchestratorDbContext db) : IAIAutomationPolicyService
{
    public async Task<IReadOnlyList<AIAutomationPolicyDto>> ListAsync(Guid? profileId, CancellationToken ct = default)
    {
        var q = db.AIAutomationPolicies.AsNoTracking().Where(p => p.IsActive);
        if (profileId.HasValue) q = q.Where(p => p.ProfileId == profileId);
        return await q.Select(p => new AIAutomationPolicyDto
        {
            Id = p.Id, ProfileId = p.ProfileId, Name = p.Name, Mode = p.Mode.ToString(),
            BusinessHoursOnly = p.BusinessHoursOnly, ConfidenceThreshold = p.ConfidenceThreshold,
            RiskLevel = p.RiskLevel, IsActive = p.IsActive
        }).ToListAsync(ct);
    }

    public async Task<AIAutomationPolicyDto> UpsertAsync(UpsertAutomationPolicyRequest request, CancellationToken ct = default)
    {
        var policy = await db.AIAutomationPolicies.FirstOrDefaultAsync(p => p.ProfileId == request.ProfileId && p.Name == request.Name, ct)
            ?? new AIAutomationPolicy { Id = Guid.NewGuid(), ProfileId = request.ProfileId, CreatedAt = DateTime.UtcNow };
        if (policy.Name != request.Name) db.AIAutomationPolicies.Add(policy);
        policy.Name = request.Name;
        policy.Mode = request.Mode;
        policy.BusinessHoursOnly = request.BusinessHoursOnly;
        policy.ConfidenceThreshold = request.ConfidenceThreshold;
        policy.RiskLevel = request.RiskLevel;
        policy.IsActive = true;
        db.AIOutboxMessages.Add(new AIOutboxMessage { Id = Guid.NewGuid(), AggregateId = policy.Id, Type = "AutomationPolicyChanged", Payload = "{}", OccurredOn = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        return new AIAutomationPolicyDto { Id = policy.Id, ProfileId = policy.ProfileId, Name = policy.Name, Mode = policy.Mode.ToString(), BusinessHoursOnly = policy.BusinessHoursOnly, ConfidenceThreshold = policy.ConfidenceThreshold, RiskLevel = policy.RiskLevel, IsActive = policy.IsActive };
    }

    public async Task<AutomationMode> ResolveModeAsync(Guid profileId, decimal confidence, CancellationToken ct = default)
    {
        var policy = await db.AIAutomationPolicies.AsNoTracking()
            .Where(p => p.ProfileId == profileId && p.IsActive).OrderByDescending(p => p.CreatedAt).FirstOrDefaultAsync(ct);
        if (policy == null) return AutomationMode.HumanApprovalRequired;
        if (policy.Mode == AutomationMode.ManualOnly) return AutomationMode.ManualOnly;
        if (confidence < policy.ConfidenceThreshold) return AutomationMode.HumanApprovalRequired;
        return policy.Mode;
    }
}

public sealed class AIPromptProfileService(AIOrchestratorDbContext db, IPromptTemplateRegistry legacyRegistry) : IAIPromptProfileService
{
    public async Task<IReadOnlyList<AIPromptProfileDto>> ListAsync(CancellationToken ct = default)
        => await db.AIPromptProfiles.AsNoTracking().Where(p => !p.IsDeleted)
            .Select(p => new AIPromptProfileDto { Id = p.Id, Code = p.Code, Name = p.Name, LayerType = p.LayerType.ToString(), ProfileId = p.ProfileId, Status = p.Status.ToString(), CurrentVersion = p.CurrentVersion })
            .ToListAsync(ct);

    public async Task<AIPromptProfileDto> UpsertAsync(UpsertPromptRequest request, CancellationToken ct = default)
    {
        var profile = await db.AIPromptProfiles.FirstOrDefaultAsync(p => p.Code == request.Code, ct);
        if (profile == null)
        {
            profile = new AIPromptProfile { Id = Guid.NewGuid(), Code = request.Code, CreatedAt = DateTime.UtcNow };
            db.AIPromptProfiles.Add(profile);
        }
        profile.Name = request.Name;
        profile.LayerType = request.LayerType;
        profile.ProfileId = request.ProfileId;
        var version = profile.CurrentVersion + (request.Publish ? 1 : 0);
        if (request.Publish)
        {
            profile.CurrentVersion = version;
            profile.Status = PromptProfileStatus.Published;
            var pv = new AIPromptVersion { Id = Guid.NewGuid(), PromptProfileId = profile.Id, Version = version, Locale = request.Locale, SystemPrompt = request.SystemPrompt, RolePrompt = request.RolePrompt, PublishedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow };
            db.AIPromptVersions.Add(pv);
            db.AIOutboxMessages.Add(new AIOutboxMessage { Id = Guid.NewGuid(), AggregateId = profile.Id, Type = "PromptPublished", Payload = JsonSerializer.Serialize(new { profile.Id, version }), OccurredOn = DateTime.UtcNow });
        }
        await db.SaveChangesAsync(ct);
        return new AIPromptProfileDto { Id = profile.Id, Code = profile.Code, Name = profile.Name, LayerType = profile.LayerType.ToString(), ProfileId = profile.ProfileId, Status = profile.Status.ToString(), CurrentVersion = profile.CurrentVersion };
    }

    public async Task<PromptEvaluateResult> EvaluateAsync(PromptEvaluateRequest request, CancellationToken ct = default)
    {
        var merged = await ResolveMergedPromptAsync(request.ProfileId, request.PromptCode, ct);
        return new PromptEvaluateResult { MergedSystemPrompt = merged, LayerCount = merged.Split("\n---\n", StringSplitOptions.RemoveEmptyEntries).Length };
    }

    public async Task<string> ResolveMergedPromptAsync(Guid? profileId, string? templateCode, CancellationToken ct = default)
    {
        var layers = new List<string>();
        var system = await db.AIPromptProfiles.AsNoTracking()
            .Where(p => p.LayerType == PromptLayerType.System && p.Status == PromptProfileStatus.Published && !p.IsDeleted)
            .Join(db.AIPromptVersions, p => p.Id, v => v.PromptProfileId, (p, v) => new { p, v })
            .Where(x => x.v.Version == x.p.CurrentVersion).Select(x => x.v.SystemPrompt).FirstOrDefaultAsync(ct);
        if (!string.IsNullOrEmpty(system)) layers.Add(system);
        if (profileId.HasValue)
        {
            var role = await db.AIPromptProfiles.AsNoTracking()
                .Where(p => p.ProfileId == profileId && p.LayerType == PromptLayerType.Role && p.Status == PromptProfileStatus.Published)
                .Join(db.AIPromptVersions, p => p.Id, v => v.PromptProfileId, (p, v) => new { p, v })
                .Where(x => x.v.Version == x.p.CurrentVersion).Select(x => x.v.SystemPrompt + (x.v.RolePrompt ?? "")).FirstOrDefaultAsync(ct);
            if (!string.IsNullOrEmpty(role)) layers.Add(role);
        }
        if (!string.IsNullOrEmpty(templateCode))
        {
            var tmpl = await db.AIPromptProfiles.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Code == templateCode && p.Status == PromptProfileStatus.Published, ct);
            if (tmpl != null)
            {
                var tv = await db.AIPromptVersions.AsNoTracking().FirstOrDefaultAsync(v => v.PromptProfileId == tmpl.Id && v.Version == tmpl.CurrentVersion, ct);
                if (tv != null) layers.Add(tv.SystemPrompt);
            }
        }
        if (layers.Count == 0)
        {
            var legacy = legacyRegistry.ResolveOrDefault(new Application.AI.Models.AIRequest { PromptTemplateId = templateCode ?? "ExplainCompliance", PromptTemplateVersion = "1.1" });
            layers.Add(legacy.SystemPrompt);
        }
        return string.Join("\n---\n", layers);
    }

    public async Task RollbackAsync(Guid promptProfileId, int targetVersion, CancellationToken ct = default)
    {
        var profile = await db.AIPromptProfiles.FirstOrDefaultAsync(p => p.Id == promptProfileId, ct) ?? throw new InvalidOperationException("Prompt not found");
        if (!await db.AIPromptVersions.AnyAsync(v => v.PromptProfileId == promptProfileId && v.Version == targetVersion, ct))
            throw new InvalidOperationException("Target version not found");
        profile.CurrentVersion = targetVersion;
        db.AIOutboxMessages.Add(new AIOutboxMessage { Id = Guid.NewGuid(), AggregateId = profile.Id, Type = "PromptRolledBack", Payload = JsonSerializer.Serialize(new { promptProfileId, targetVersion }), OccurredOn = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
    }
}

public sealed class AIExecutionAuditService(AIOrchestratorDbContext db) : IAIExecutionAuditService
{
    public async Task<Guid> RecordAsync(AIExecutionAuditWriteRequest request, CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        db.AIExecutionAudits.Add(new AIExecutionAudit
        {
            Id = id, RequestedByUserId = request.RequestedByUserId, ProfileId = request.ProfileId, PromptVersionId = request.PromptVersionId,
            HospitalId = request.HospitalId, UseCase = request.UseCase, ModelProvider = request.ModelProvider, ModelName = request.ModelName,
            InputTokens = request.InputTokens, OutputTokens = request.OutputTokens, LatencyMs = request.LatencyMs, Confidence = request.Confidence,
            ToolsUsedJson = request.ToolsUsedJson, DocumentsAccessedJson = request.DocumentsAccessedJson, Outcome = request.Outcome,
            TraceId = request.TraceId, OccurredAtUtc = DateTime.UtcNow
        });
        var eventType = request.Outcome == ExecutionOutcome.Failed ? "AIExecutionFailed" : "AIExecutionCompleted";
        db.AIOutboxMessages.Add(new AIOutboxMessage { Id = Guid.NewGuid(), AggregateId = id, Type = eventType, Payload = "{}", OccurredOn = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
        return id;
    }

    public async Task<PagedResult<AIExecutionAuditDto>> ListAsync(int page, int pageSize, Guid? profileId, CancellationToken ct = default)
    {
        var q = db.AIExecutionAudits.AsNoTracking();
        if (profileId.HasValue) q = q.Where(a => a.ProfileId == profileId);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(a => a.OccurredAtUtc).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => new AIExecutionAuditDto { Id = a.Id, ProfileId = a.ProfileId, UseCase = a.UseCase, ModelProvider = a.ModelProvider, ModelName = a.ModelName, LatencyMs = a.LatencyMs, Confidence = a.Confidence, Outcome = a.Outcome.ToString(), TraceId = a.TraceId, OccurredAtUtc = a.OccurredAtUtc }).ToListAsync(ct);
        return new PagedResult<AIExecutionAuditDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<AIExecutionAuditDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var a = await db.AIExecutionAudits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        return a == null ? null : new AIExecutionAuditDto { Id = a.Id, ProfileId = a.ProfileId, UseCase = a.UseCase, ModelProvider = a.ModelProvider, ModelName = a.ModelName, LatencyMs = a.LatencyMs, Confidence = a.Confidence, Outcome = a.Outcome.ToString(), TraceId = a.TraceId, OccurredAtUtc = a.OccurredAtUtc };
    }
}

public sealed class AIAnalyticsService(AIOrchestratorDbContext db) : IAIAnalyticsService
{
    public async Task<AIAnalyticsDto> GetDashboardAsync(CancellationToken ct = default)
    {
        var audits = db.AIExecutionAudits.AsNoTracking();
        var total = await audits.CountAsync(ct);
        if (total == 0) return new AIAnalyticsDto();
        return new AIAnalyticsDto
        {
            TotalExecutions = total,
            TotalTokens = await audits.SumAsync(a => (long)a.InputTokens + a.OutputTokens, ct),
            EstimatedCost = total * 0.002m,
            AvgLatencyMs = (int)await audits.AverageAsync(a => (double)a.LatencyMs, ct),
            AcceptanceRate = await audits.CountAsync(a => a.Outcome == ExecutionOutcome.Success, ct) / (decimal)total,
            AutomationRate = await db.AIAutomationPolicies.CountAsync(p => p.Mode == AutomationMode.AutoExecute, ct) / Math.Max(1m, await db.AIAutomationPolicies.CountAsync(ct))
        };
    }
}

public sealed class AIApprovalService(AIOrchestratorDbContext db) : IAIApprovalService
{
    public async Task<IReadOnlyList<AIApprovalDto>> ListPendingAsync(CancellationToken ct = default)
        => await db.AIApprovalRequests.AsNoTracking().Where(a => a.Status == ApprovalStatus.Pending)
            .Select(a => new AIApprovalDto { Id = a.Id, ExecutionAuditId = a.ExecutionAuditId, ProfileId = a.ProfileId, Status = a.Status.ToString(), CreatedAt = a.CreatedAt }).ToListAsync(ct);

    public async Task GrantAsync(Guid approvalId, Guid approvedByUserId, CancellationToken ct = default)
    {
        var req = await db.AIApprovalRequests.FirstOrDefaultAsync(a => a.Id == approvalId, ct) ?? throw new InvalidOperationException("Not found");
        req.Status = ApprovalStatus.Granted;
        req.ApprovedByUserId = approvedByUserId;
        req.ResolvedAt = DateTime.UtcNow;
        var audit = await db.AIExecutionAudits.FirstOrDefaultAsync(a => a.Id == req.ExecutionAuditId, ct);
        if (audit != null) { audit.ApprovalStatus = ApprovalStatus.Granted; audit.ApprovedByUserId = approvedByUserId; }
        db.AIOutboxMessages.Add(new AIOutboxMessage { Id = Guid.NewGuid(), AggregateId = approvalId, Type = "AIApprovalGranted", Payload = "{}", OccurredOn = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
    }
}

public sealed class AIProfilePermissionService(AIOrchestratorDbContext db) : IAIProfilePermissionService
{
    public async Task<IReadOnlyList<ProfilePermissionDto>> ListAsync(Guid profileId, CancellationToken ct = default)
        => await db.AIProfilePermissionGrants.AsNoTracking().Where(g => g.ProfileId == profileId)
            .Select(g => new ProfilePermissionDto { Id = g.Id, PermissionCode = g.PermissionCode, ActionType = g.ActionType.ToString(), Enabled = g.Enabled }).ToListAsync(ct);

    public async Task<bool> ProfileAllowsActionAsync(Guid profileId, AIActionType action, CancellationToken ct = default)
        => await db.AIProfilePermissionGrants.AnyAsync(g => g.ProfileId == profileId && g.ActionType == action && g.Enabled, ct);
}
