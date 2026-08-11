using AIOrchestratorService.Application.AI.Identity.Contracts;
using AIOrchestratorService.Application.AI.Prompting;
using AIOrchestratorService.Domain.Entities;
using AIOrchestratorService.Domain.Enums;
using AIOrchestratorService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIOrchestratorService.Infrastructure.Background;

public sealed class AIIdentityBootstrapHostedService(IServiceProvider sp) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AIOrchestratorDbContext>();
        await SeedProfilesAsync(db, stoppingToken);
        await SeedPromptsAsync(db, scope.ServiceProvider.GetRequiredService<IPromptTemplateRegistry>(), stoppingToken);
        await SeedDefaultPoliciesAsync(db, stoppingToken);
    }

    private static async Task SeedProfilesAsync(AIOrchestratorDbContext db, CancellationToken ct)
    {
        if (await db.AIAssistantProfiles.AnyAsync(ct)) return;
        var profiles = new (string Code, string Name, string Module, string? Persona, AIActionType[] Actions)[]
        {
            ("compliance-assistant", "Compliance Assistant", "Compliance", "AccreditationCoach", [AIActionType.Read, AIActionType.Suggest, AIActionType.Recommend]),
            ("audit-assistant", "Audit Assistant", "Audit", "AuditorAssistant", [AIActionType.Read, AIActionType.Analyze]),
            ("risk-assistant", "Risk Assistant", "Compliance", "CapaAdvisor", [AIActionType.Analyze, AIActionType.Recommend]),
            ("policy-assistant", "Policy Assistant", "Governance", "QualityManagerAssistant", [AIActionType.Read, AIActionType.Summarize]),
            ("evidence-assistant", "Evidence Assistant", "Compliance", "EvidenceAdvisor", [AIActionType.Read, AIActionType.Suggest]),
            ("meeting-assistant", "Meeting Assistant", "Governance", null, [AIActionType.Summarize, AIActionType.Draft]),
            ("training-assistant", "Training Assistant", "Workforce", "DepartmentHeadAssistant", [AIActionType.Read, AIActionType.Recommend]),
            ("clinical-indicator-assistant", "Clinical Indicator Assistant", "Compliance", null, [AIActionType.Analyze, AIActionType.Recommend]),
            ("executive-assistant", "Executive Assistant", "Governance", "ExecutiveAdvisor", [AIActionType.Read, AIActionType.Summarize, AIActionType.Recommend])
        };
        foreach (var (code, name, module, persona, actions) in profiles)
        {
            var id = Guid.NewGuid();
            db.AIAssistantProfiles.Add(new AIAssistantProfile
            {
                Id = id, Code = code, Name = name, Description = name, ModuleCode = module,
                CopilotPersonaKey = persona, Status = AIProfileStatus.Active, Version = 1, CreatedAt = DateTime.UtcNow
            });
            db.AIIdentities.Add(new AIIdentity { Id = Guid.NewGuid(), ProfileId = id, PlatformId = $"AI-{id:N}", DisplayName = name, LifecycleStatus = AIIdentityLifecycleStatus.Active, CreatedAt = DateTime.UtcNow });
            foreach (var action in actions)
                db.AIProfilePermissionGrants.Add(new AIProfilePermissionGrant { Id = Guid.NewGuid(), ProfileId = id, PermissionCode = $"AI.{action}", ActionType = action, Enabled = true });
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedPromptsAsync(AIOrchestratorDbContext db, IPromptTemplateRegistry registry, CancellationToken ct)
    {
        if (await db.AIPromptProfiles.AnyAsync(ct)) return;
        var templates = new[] { "ExplainCompliance", "SuggestMapping", "RiskAnalysis", "TaskRecommendation" };
        foreach (var code in templates)
        {
            var t = registry.ResolveOrDefault(new Application.AI.Models.AIRequest { PromptTemplateId = code, PromptTemplateVersion = "1.0" });
            var pid = Guid.NewGuid();
            db.AIPromptProfiles.Add(new AIPromptProfile { Id = pid, Code = code, Name = code, LayerType = PromptLayerType.Template, Status = PromptProfileStatus.Published, CurrentVersion = 1, CreatedAt = DateTime.UtcNow });
            db.AIPromptVersions.Add(new AIPromptVersion { Id = Guid.NewGuid(), PromptProfileId = pid, Version = 1, SystemPrompt = t.SystemPrompt, PublishedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow });
        }
        var systemId = Guid.NewGuid();
        db.AIPromptProfiles.Add(new AIPromptProfile { Id = systemId, Code = "praman-system", Name = "PRAMAN System", LayerType = PromptLayerType.System, Status = PromptProfileStatus.Published, CurrentVersion = 1, CreatedAt = DateTime.UtcNow });
        db.AIPromptVersions.Add(new AIPromptVersion { Id = Guid.NewGuid(), PromptProfileId = systemId, Version = 1, SystemPrompt = "You are a governed PRAMAN AI assistant. Never bypass authorization or modify data without approval.", PublishedAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedDefaultPoliciesAsync(AIOrchestratorDbContext db, CancellationToken ct)
    {
        if (await db.AIAutomationPolicies.AnyAsync(ct)) return;
        var profiles = await db.AIAssistantProfiles.ToListAsync(ct);
        foreach (var p in profiles)
        {
            db.AIAutomationPolicies.Add(new AIAutomationPolicy
            {
                Id = Guid.NewGuid(), ProfileId = p.Id, Name = "default", Mode = AutomationMode.HumanApprovalRequired,
                ConfidenceThreshold = 0.75m, RiskLevel = "Medium", IsActive = true, CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync(ct);
    }
}

public sealed class AIOutboxProcessorHostedService(IServiceProvider sp) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AIOrchestratorDbContext>();
                var pending = await db.AIOutboxMessages.Where(m => m.ProcessedOn == null).Take(50).ToListAsync(stoppingToken);
                foreach (var msg in pending) msg.ProcessedOn = DateTime.UtcNow;
                if (pending.Count > 0) await db.SaveChangesAsync(stoppingToken);
            }
            catch { /* retry next cycle */ }
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
