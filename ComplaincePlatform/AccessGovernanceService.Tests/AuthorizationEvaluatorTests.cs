using AccessGovernanceService.Application.Contracts;
using AccessGovernanceService.Application.Services;
using AccessGovernanceService.Domain.Aggregates;
using AccessGovernanceService.Domain.Enums;
using AccessGovernanceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace AccessGovernanceService.Tests;

public class AuthorizationEvaluatorTests
{
    private static AccessGovernanceDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AccessGovernanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AccessGovernanceDbContext(options);
    }

    private static AuthorizationEvaluatorService CreateEvaluator(
        AccessGovernanceDbContext db,
        Mock<IWorkforceServiceClient>? workforce = null,
        Mock<IProfessionalServiceClient>? professional = null,
        Mock<IOrganizationServiceClient>? organization = null)
    {
        return new AuthorizationEvaluatorService(
            db,
            (workforce ?? new Mock<IWorkforceServiceClient>()).Object,
            (professional ?? new Mock<IProfessionalServiceClient>()).Object,
            (organization ?? new Mock<IOrganizationServiceClient>()).Object);
    }

    [Fact]
    public async Task Duplicate_permission_code_rejected()
    {
        await using var db = CreateDb();
        var svc = new PermissionAppService(db);
        await svc.CreateAsync(new CreatePermissionRequest { Code = "Test.Perm.View", Module = "Test", Feature = "Perm", Action = "View" });
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(new CreatePermissionRequest { Code = "Test.Perm.View", Module = "Test", Feature = "Perm", Action = "View" }));
    }

    [Fact]
    public async Task Duplicate_active_role_assignment_rejected()
    {
        await using var db = CreateDb();
        var roleSvc = new RoleAppService(db);
        var role = await roleSvc.CreateAsync(new CreateRoleRequest { RoleCode = "R1", RoleName = "Role 1" }, Guid.NewGuid());
        var userId = Guid.NewGuid();
        await roleSvc.AssignRoleAsync(role.Id, new AssignRoleRequest { UserId = userId }, Guid.NewGuid());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            roleSvc.AssignRoleAsync(role.Id, new AssignRoleRequest { UserId = userId }, Guid.NewGuid()));
    }

    [Fact]
    public async Task Circular_delegation_rejected()
    {
        await using var db = CreateDb();
        var perm = Permission.Create("Test.Deleg", "Test", "Deleg", "Use", "d", null, null, null);
        db.Permissions.Add(perm);
        await db.SaveChangesAsync();
        var svc = new DelegationAppService(db);
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await svc.CreateAsync(new CreateDelegationRequest { FromUserId = a, ToUserId = b, PermissionId = perm.Id, EffectiveFrom = DateTime.UtcNow.AddDays(-1), RequiresApproval = false }, Guid.NewGuid());
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(new CreateDelegationRequest { FromUserId = b, ToUserId = a, PermissionId = perm.Id, EffectiveFrom = DateTime.UtcNow.AddDays(-1), RequiresApproval = false }, Guid.NewGuid()));
    }

    [Fact]
    public async Task Delegation_without_permission_rejected()
    {
        await using var db = CreateDb();
        var svc = new DelegationAppService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(new CreateDelegationRequest { FromUserId = Guid.NewGuid(), ToUserId = Guid.NewGuid(), PermissionId = Guid.Empty, EffectiveFrom = DateTime.UtcNow, RequiresApproval = false }, Guid.NewGuid()));
    }

    [Fact]
    public async Task Role_permission_grants_access()
    {
        await using var db = CreateDb();
        var perm = Permission.Create("Compliance.Standard.View", "Compliance", "Standard", "View", "view", null, null, null);
        db.Permissions.Add(perm);
        var role = RoleDefinition.Create("OFFICER", "Officer", ScopeType.Platform, null, null, null, null);
        role.GrantPermission(perm.Id, Guid.Empty);
        role.AssignToUser(Guid.NewGuid(), null, null, null, null, null, null, Guid.Empty);
        db.RoleDefinitions.Add(role);
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db);
        var userId = role.Assignments.First().UserId;
        var decision = await evaluator.EvaluateAsync(new EvaluateAuthorizationRequest
        {
            SubjectId = userId,
            Resource = "Compliance",
            Action = "Standard.View"
        });
        Assert.True(decision.Allowed);
        Assert.Contains("OFFICER", decision.MatchedRoles);
        Assert.NotEmpty(decision.EvaluationTrace);
    }

    [Fact]
    public async Task Scoped_assignment_denies_out_of_scope()
    {
        await using var db = CreateDb();
        var perm = Permission.Create("Compliance.Task.View", "Compliance", "Task", "View", "v", null, null, null);
        db.Permissions.Add(perm);
        var orgId = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();
        var role = RoleDefinition.Create("ORG_VIEWER", "Org Viewer", ScopeType.Organization, orgId, null, null, null);
        role.GrantPermission(perm.Id, Guid.Empty);
        var userId = Guid.NewGuid();
        role.AssignToUser(userId, orgId, null, null, null, null, null, Guid.Empty);
        db.RoleDefinitions.Add(role);
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db);
        var denied = await evaluator.EvaluateAsync(new EvaluateAuthorizationRequest
        {
            SubjectId = userId,
            OrganizationId = otherOrg,
            Resource = "Compliance",
            Action = "Task.View"
        });
        Assert.False(denied.Allowed);
    }

    [Fact]
    public async Task Inherited_role_permission_grants_access()
    {
        await using var db = CreateDb();
        var perm = Permission.Create("Audit.Report.View", "Audit", "Report", "View", "v", null, null, null);
        db.Permissions.Add(perm);
        var parent = RoleDefinition.Create("PARENT", "Parent", ScopeType.Platform, null, null, null, null);
        parent.GrantPermission(perm.Id, Guid.Empty);
        var child = RoleDefinition.Create("CHILD", "Child", ScopeType.Platform, null, null, null, null);
        child.SetParentRole(parent.Id);
        var userId = Guid.NewGuid();
        child.AssignToUser(userId, null, null, null, null, null, null, Guid.Empty);
        db.RoleDefinitions.Add(parent);
        db.RoleDefinitions.Add(child);
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db);
        var decision = await evaluator.EvaluateAsync(new EvaluateAuthorizationRequest { SubjectId = userId, Resource = "Audit", Action = "Report.View" });
        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task Abac_doctor_credential_policy_permits()
    {
        await using var db = CreateDb();
        var equalsOp = Guid.NewGuid();
        var catId = Guid.NewGuid();
        db.LookupCategories.Add(new Domain.Entities.AccessGovernanceLookupCategory { Id = catId, Code = "PolicyOperator", Name = "PolicyOperator" });
        db.LookupValues.Add(new Domain.Entities.AccessGovernanceLookupValue { Id = equalsOp, CategoryId = catId, Code = "Equals", Name = "Equals", SortOrder = 0 });
        await db.SaveChangesAsync();
        var policy = AuthorizationPolicy.Create("DOCTOR_APPROVE", "Doctor approve", 10, null, null);
        var rule = policy.AddRule(PolicyEffect.Permit, 1);
        rule.AddCondition("professional_category", equalsOp, "Doctor");
        rule.AddCondition("credential_approved", equalsOp, "True");
        policy.Publish();
        db.AuthorizationPolicies.Add(policy);
        await db.SaveChangesAsync();

        var professional = new Mock<IProfessionalServiceClient>();
        professional.Setup(p => p.GetByPersonIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfessionalContextDto { Category = "Doctor", HasActiveRegistration = true, HasActiveLicense = true });

        var evaluator = CreateEvaluator(db, professional: professional);
        var decision = await evaluator.EvaluateAsync(new EvaluateAuthorizationRequest
        {
            SubjectId = Guid.NewGuid(),
            PersonId = Guid.NewGuid(),
            Resource = "Clinical",
            Action = "Indicator.Approve"
        });
        Assert.True(decision.Allowed);
        Assert.Contains("DOCTOR_APPROVE", decision.MatchedPolicies);
    }

    [Fact]
    public async Task Rls_denies_resource_outside_department()
    {
        await using var db = CreateDb();
        var perm = Permission.Create("HR.Employee.View", "HR", "Employee", "View", "v", null, null, null);
        db.Permissions.Add(perm);
        var deptId = Guid.NewGuid();
        var role = RoleDefinition.Create("DEPT_HEAD", "Dept Head", ScopeType.Department, null, null, deptId, null);
        var userId = Guid.NewGuid();
        role.GrantPermission(perm.Id, Guid.Empty);
        role.AssignToUser(userId, null, null, deptId, null, null, null, Guid.Empty);
        db.RoleDefinitions.Add(role);
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db);
        var otherDept = Guid.NewGuid();
        var decision = await evaluator.EvaluateAsync(new EvaluateAuthorizationRequest
        {
            SubjectId = userId,
            DepartmentId = deptId,
            Resource = "HR",
            Action = "Employee.View",
            Context = new Dictionary<string, string> { ["resource_department_id"] = otherDept.ToString() }
        });
        Assert.False(decision.Allowed);
        Assert.Contains("Row-level", decision.Reason);
    }

    [Fact]
    public async Task Explicit_deny_policy_wins()
    {
        await using var db = CreateDb();
        var equalsOp = Guid.NewGuid();
        var policy = AuthorizationPolicy.Create("DENY_ALL", "Deny", 100, null, null);
        var rule = policy.AddRule(PolicyEffect.Deny, 1);
        rule.AddCondition("action", equalsOp, "Verify");
        policy.Publish();
        db.AuthorizationPolicies.Add(policy);
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db);
        var decision = await evaluator.EvaluateAsync(new EvaluateAuthorizationRequest
        {
            SubjectId = Guid.NewGuid(),
            Resource = "Compliance",
            Action = "Verify"
        });
        Assert.False(decision.Allowed);
        Assert.Contains("DENY_ALL", decision.MatchedPolicies);
    }

    [Fact]
    public async Task Approval_authority_satisfies_approve_action()
    {
        await using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.ApprovalAuthorities.Add(ApprovalAuthority.Create(userId, null, Guid.NewGuid(), "CAPA", null, null, null, DateTime.UtcNow.AddDays(-1), null, Guid.NewGuid()));
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db);
        var decision = await evaluator.EvaluateAsync(new EvaluateAuthorizationRequest { SubjectId = userId, Resource = "CAPA", Action = "Approve" });
        Assert.True(decision.Allowed);
    }

    [Fact]
    public async Task Effective_permissions_includes_delegations()
    {
        await using var db = CreateDb();
        var perm = Permission.Create("Evidence.Document.Verify", "Evidence", "Document", "Verify", "v", null, null, null);
        db.Permissions.Add(perm);
        var userId = Guid.NewGuid();
        db.Delegations.Add(Delegation.Create(Guid.NewGuid(), userId, null, null, perm.Id, null, null, null, null, DateTime.UtcNow.AddDays(-1), null, false, Guid.NewGuid()));
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db);
        var perms = await evaluator.GetEffectivePermissionsDetailedAsync(userId);
        Assert.Contains(perms, p => p.Code == "Evidence.Document.Verify" && p.Sources.Any(s => s.StartsWith("Delegation:")));
    }

    [Fact]
    public async Task Soft_deleted_role_excluded_from_evaluation()
    {
        await using var db = CreateDb();
        var perm = Permission.Create("Evidence.Document.Upload", "Evidence", "Document", "Upload", "u", null, null, null);
        db.Permissions.Add(perm);
        var role = RoleDefinition.Create("UPLOADER", "Uploader", ScopeType.Platform, null, null, null, null);
        var userId = Guid.NewGuid();
        role.GrantPermission(perm.Id, Guid.Empty);
        role.AssignToUser(userId, null, null, null, null, null, null, Guid.Empty);
        role.SoftDelete();
        db.RoleDefinitions.Add(role);
        await db.SaveChangesAsync();

        var evaluator = CreateEvaluator(db);
        var decision = await evaluator.EvaluateAsync(new EvaluateAuthorizationRequest { SubjectId = userId, Resource = "Evidence", Action = "Document.Upload" });
        Assert.False(decision.Allowed);
    }

    [Fact]
    public async Task Evaluation_trace_records_pipeline_steps()
    {
        await using var db = CreateDb();
        var evaluator = CreateEvaluator(db);
        var decision = await evaluator.EvaluateAsync(new EvaluateAuthorizationRequest { SubjectId = Guid.NewGuid(), Resource = "X", Action = "Y" });
        Assert.False(decision.Allowed);
        Assert.Contains(decision.EvaluationTrace, t => t.Step == "DefaultDeny");
    }
}
