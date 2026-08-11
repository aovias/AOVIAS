using AIOrchestratorService.Application.AI.Identity.Services;
using AIOrchestratorService.Domain.Entities;
using AIOrchestratorService.Domain.Enums;
using AIOrchestratorService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AIOrchestratorService.Tests;

public class AutomationPolicyResolverTests
{
    [Fact]
    public async Task ResolveMode_BelowThreshold_RequiresApproval()
    {
        var db = new AIOrchestratorDbContext(new DbContextOptionsBuilder<AIOrchestratorDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var profileId = Guid.NewGuid();
        db.AIAutomationPolicies.Add(new AIAutomationPolicy { Id = Guid.NewGuid(), ProfileId = profileId, Name = "default", Mode = AutomationMode.AutoExecute, ConfidenceThreshold = 0.9m, IsActive = true, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var svc = new AIAutomationPolicyService(db);
        var mode = await svc.ResolveModeAsync(profileId, 0.5m);
        Assert.Equal(AutomationMode.HumanApprovalRequired, mode);
        await db.DisposeAsync();
    }
}

public class PromptMergeTests
{
    [Fact]
    public async Task ResolveMergedPrompt_FallsBackToLegacy_WhenDbEmpty()
    {
        var db = new AIOrchestratorDbContext(new DbContextOptionsBuilder<AIOrchestratorDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var legacy = new Application.AI.Prompting.PromptTemplateRegistry();
        var svc = new AIPromptProfileService(db, legacy);
        var merged = await svc.ResolveMergedPromptAsync(null, "ExplainCompliance");
        Assert.Contains("PRAMAN", merged);
        await db.DisposeAsync();
    }
}

public class ProfilePermissionTests
{
    [Fact]
    public async Task ProfileAllowsAction_WhenGrantExists()
    {
        var db = new AIOrchestratorDbContext(new DbContextOptionsBuilder<AIOrchestratorDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var profileId = Guid.NewGuid();
        db.AIProfilePermissionGrants.Add(new AIProfilePermissionGrant { Id = Guid.NewGuid(), ProfileId = profileId, PermissionCode = "AI.Query.Read", ActionType = AIActionType.Read, Enabled = true });
        await db.SaveChangesAsync();

        var svc = new AIProfilePermissionService(db);
        Assert.True(await svc.ProfileAllowsActionAsync(profileId, AIActionType.Read));
        Assert.False(await svc.ProfileAllowsActionAsync(profileId, AIActionType.Draft));
        await db.DisposeAsync();
    }
}
