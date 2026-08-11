using AccessGovernanceService.Domain.Aggregates;
using AccessGovernanceService.Domain.Entities;
using AccessGovernanceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AccessGovernanceService.Infrastructure.MasterData;

public static class AccessGovernanceMasterDataSeeder
{
    public static readonly string[] Categories =
    [
        "ResponsibilityType", "ApprovalLevel", "PermissionGroup", "DelegationReason",
        "RoleCategory", "PolicyType", "PolicyOperator", "ResourceType", "ActionType"
    ];

    public static async Task SeedAsync(AccessGovernanceDbContext db, CancellationToken ct = default)
    {
        foreach (var code in Categories)
        {
            if (!await db.LookupCategories.AnyAsync(c => c.Code == code, ct))
                db.LookupCategories.Add(new AccessGovernanceLookupCategory { Id = Guid.NewGuid(), Code = code, Name = code });
        }
        await db.SaveChangesAsync(ct);

        await SeedLookupValuesAsync(db, "PolicyOperator", [
            ("Equals", "Equals"), ("NotEquals", "Not Equals"), ("In", "In"),
            ("Contains", "Contains"), ("GreaterThan", "Greater Than"), ("Exists", "Exists")], ct);
        await SeedLookupValuesAsync(db, "ApprovalLevel", [("L1", "Level 1"), ("L2", "Level 2"), ("L3", "Level 3")], ct);
        await SeedLookupValuesAsync(db, "ResponsibilityType", [("ProcessOwner", "Process Owner"), ("EvidenceOwner", "Evidence Owner"), ("CapaOwner", "CAPA Owner")], ct);
        await SeedLookupValuesAsync(db, "PermissionGroup", [("Compliance", "Compliance"), ("Audit", "Audit"), ("Evidence", "Evidence"), ("AI", "AI & Copilot"), ("Intelligence", "Workforce Intelligence")], ct);

        if (!await db.Permissions.AnyAsync(p => p.Code.StartsWith("Intelligence."), ct))
        {
            var intelligencePerms = new[]
            {
                ("Intelligence.Dashboard.Read", "Intelligence", "Dashboard", "Read"),
                ("Intelligence.Forecast.Read", "Intelligence", "Forecast", "Read"),
                ("Intelligence.Recommendations.Read", "Intelligence", "Recommendations", "Read"),
                ("Intelligence.Search.Read", "Intelligence", "Search", "Read")
            };
            foreach (var (code, mod, feat, act) in intelligencePerms)
                db.Permissions.Add(Permission.Create(code, mod, feat, act, code, null, null, null));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Permissions.AnyAsync(p => p.Code.StartsWith("Directory."), ct))
        {
            var directoryPerms = new[]
            {
                ("Directory.Search.Read", "Directory", "Search", "Read", null),
                ("Directory.Profile.Read", "Directory", "Profile", "Read", null),
                ("Directory.Contact.Read", "Directory", "Contact", "Read", """{"mask":["email","phone"]}"""),
                ("Directory.Credential.Read", "Directory", "Credential", "Read", """{"mask":["licenseNumber","certificationNumber","professionalNumber"]}"""),
                ("Directory.SavedSearch.Manage", "Directory", "SavedSearch", "Manage", null),
                ("Directory.History.Read", "Directory", "History", "Read", null),
                ("Directory.NLSearch.Use", "Directory", "NLSearch", "Use", null)
            };
            foreach (var (code, mod, feat, act, mask) in directoryPerms)
                db.Permissions.Add(Permission.Create(code, mod, feat, act, code, null, null, null, mask));
            await db.SaveChangesAsync(ct);
        }

        foreach (var roleCode in new[] { "ORG_ADMIN", "SUPER_ADMIN" })
        {
            var role = await db.RoleDefinitions.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.RoleCode == roleCode, ct);
            if (role == null) continue;
            foreach (var perm in await db.Permissions.Where(p => p.Code.StartsWith("Directory.")).ToListAsync(ct))
            {
                if (!role.Permissions.Any(a => a.PermissionId == perm.Id && !a.IsRevoked))
                    role.GrantPermission(perm.Id, Guid.Empty);
            }
        }
        await db.SaveChangesAsync(ct);

        if (!await db.Permissions.AnyAsync(p => p.Code.StartsWith("Integration."), ct))
        {
            var integrationPerms = new[]
            {
                ("Integration.Connector.Read", "Integration", "Connector", "Read"),
                ("Integration.Connector.Manage", "Integration", "Connector", "Manage"),
                ("Integration.Sync.Read", "Integration", "Sync", "Read"),
                ("Integration.Sync.Execute", "Integration", "Sync", "Execute"),
                ("Integration.Mapping.Read", "Integration", "Mapping", "Read"),
                ("Integration.Mapping.Manage", "Integration", "Mapping", "Manage"),
                ("Integration.Webhook.Read", "Integration", "Webhook", "Read"),
                ("Integration.Webhook.Manage", "Integration", "Webhook", "Manage"),
                ("Integration.Import.Execute", "Integration", "Import", "Execute"),
                ("Integration.Export.Execute", "Integration", "Export", "Execute"),
                ("Integration.Monitor.Read", "Integration", "Monitor", "Read"),
                ("Integration.Audit.Read", "Integration", "Audit", "Read")
            };
            foreach (var (code, mod, feat, act) in integrationPerms)
                db.Permissions.Add(Permission.Create(code, mod, feat, act, code, null, null, null));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Permissions.AnyAsync(p => p.Code.StartsWith("AI."), ct))
        {
            var aiPerms = new[]
            {
                ("AI.Profile.Manage", "AI", "Profile", "Manage"),
                ("AI.Query.Read", "AI", "Query", "Read"),
                ("AI.Assist.Suggest", "AI", "Assist", "Suggest"),
                ("AI.Action.Preview", "AI", "Action", "Preview"),
                ("AI.Action.Execute", "AI", "Action", "Execute"),
                ("AI.Workflow.Start", "AI", "Workflow", "Start"),
                ("AI.Workflow.Approve", "AI", "Workflow", "Approve"),
                ("AI.Prompt.Manage", "AI", "Prompt", "Manage"),
                ("AI.Policy.Manage", "AI", "Policy", "Manage"),
                ("AI.Analytics.View", "AI", "Analytics", "View"),
                ("Copilot.Recommendation.View", "Copilot", "Recommendation", "View"),
                ("Copilot.Draft.Generate", "Copilot", "Draft", "Generate"),
                ("Copilot.Proposal.Approve", "Copilot", "Proposal", "Approve"),
                ("Copilot.Proposal.Execute", "Copilot", "Proposal", "Execute")
            };
            foreach (var (code, mod, feat, act) in aiPerms)
                db.Permissions.Add(Permission.Create(code, mod, feat, act, code, null, null, null));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Permissions.AnyAsync(p => p.Code.StartsWith("Notification."), ct))
        {
            var notificationPerms = new[]
            {
                ("Notification.View", "Notification", "Notification", "View"),
                ("Notification.Manage", "Notification", "Notification", "Manage"),
                ("Notification.Template.Manage", "Notification", "Template", "Manage"),
                ("Notification.Provider.Manage", "Notification", "Provider", "Manage"),
                ("Notification.Preference.Manage", "Notification", "Preference", "Manage"),
                ("Notification.Queue.View", "Notification", "Queue", "View"),
                ("Notification.History.View", "Notification", "History", "View"),
            };
            foreach (var (code, mod, feat, act) in notificationPerms)
                db.Permissions.Add(Permission.Create(code, mod, feat, act, code, null, null, null));
            await db.SaveChangesAsync(ct);

            foreach (var roleCode in new[] { "ORG_ADMIN", "SUPER_ADMIN" })
            {
                var role = await db.RoleDefinitions.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.RoleCode == roleCode, ct);
                if (role == null) continue;
                foreach (var perm in await db.Permissions.Where(p => p.Code.StartsWith("Notification.")).ToListAsync(ct))
                {
                    if (!role.Permissions.Any(a => a.PermissionId == perm.Id))
                        role.GrantPermission(perm.Id, Guid.Empty);
                }
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Permissions.AnyAsync(p => p.Code.StartsWith("Audit.View"), ct))
        {
            var auditPerms = new[]
            {
                ("Audit.View", "Audit", "Audit", "View"),
                ("Audit.Export", "Audit", "Audit", "Export"),
                ("Audit.Manage", "Audit", "Audit", "Manage"),
            };
            foreach (var (code, mod, feat, act) in auditPerms)
                db.Permissions.Add(Permission.Create(code, mod, feat, act, code, null, null, null));
            await db.SaveChangesAsync(ct);

            foreach (var roleCode in new[] { "ORG_ADMIN", "SUPER_ADMIN" })
            {
                var role = await db.RoleDefinitions.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.RoleCode == roleCode, ct);
                if (role == null) continue;
                foreach (var perm in await db.Permissions.Where(p => p.Code.StartsWith("Audit.")).ToListAsync(ct))
                {
                    if (!role.Permissions.Any(a => a.PermissionId == perm.Id))
                        role.GrantPermission(perm.Id, Guid.Empty);
                }
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Permissions.AnyAsync(p => p.Code.StartsWith("Users."), ct))
        {
            var userPerms = new[]
            {
                ("Users.View", "Users", "Users", "View"),
                ("Users.Create", "Users", "Users", "Create"),
                ("Users.Update", "Users", "Users", "Update"),
                ("Users.Disable", "Users", "Users", "Disable"),
                ("Users.Delete", "Users", "Users", "Delete"),
                ("Users.ResetPassword", "Users", "Users", "ResetPassword"),
                ("PasswordManagement.Manage", "PasswordManagement", "PasswordManagement", "Manage")
            };
            foreach (var (code, mod, feat, act) in userPerms)
                db.Permissions.Add(Permission.Create(code, mod, feat, act, code, null, null, null));
            await db.SaveChangesAsync(ct);

            await SeedLookupValuesAsync(db, "PermissionGroup", [("Identity", "Identity & Access")], ct);

            foreach (var roleCode in new[] { "ORG_ADMIN", "SUPER_ADMIN" })
            {
                var role = await db.RoleDefinitions.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.RoleCode == roleCode, ct);
                if (role == null)
                {
                    role = RoleDefinition.Create(roleCode, roleCode.Replace('_', ' '), Domain.Enums.ScopeType.Platform, null, null, null, null, true);
                    db.RoleDefinitions.Add(role);
                    await db.SaveChangesAsync(ct);
                }

                foreach (var perm in await db.Permissions.Where(p => p.Code.StartsWith("Users.") || p.Code == "PasswordManagement.Manage").ToListAsync(ct))
                {
                    if (!role.Permissions.Any(a => a.PermissionId == perm.Id))
                        role.GrantPermission(perm.Id, Guid.Empty);
                }
            }
            await db.SaveChangesAsync(ct);
        }

        if (!await db.Permissions.AnyAsync(p => p.Code == "Compliance.Standard.View", ct))
        {
            var compliancePerms = new[]
            {
                ("Compliance.Standard.View", "Compliance", "Standard", "View"),
                ("Compliance.Evidence.Verify", "Compliance", "Evidence", "Verify"),
                ("Audit.Finding.Approve", "Audit", "Finding", "Approve"),
                ("Evidence.Document.Upload", "Evidence", "Document", "Upload"),
                ("CAPA.Action.Approve", "CAPA", "Action", "Approve")
            };
            foreach (var (code, mod, feat, act) in compliancePerms)
                db.Permissions.Add(Permission.Create(code, mod, feat, act, code, null, null, null));
            await db.SaveChangesAsync(ct);
        }

        if (!await db.RoleDefinitions.AnyAsync(r => r.RoleCode == "COMPLIANCE_OFFICER", ct))
        {
            var role = RoleDefinition.Create("COMPLIANCE_OFFICER", "Compliance Officer", Domain.Enums.ScopeType.Platform, null, null, null, null, true);
            db.RoleDefinitions.Add(role);
            await db.SaveChangesAsync(ct);
            var viewPerm = await db.Permissions.FirstAsync(p => p.Code == "Compliance.Standard.View", ct);
            role.GrantPermission(viewPerm.Id, Guid.Empty);
        }

        foreach (var roleCode in new[] { "ORG_ADMIN", "SUPER_ADMIN" })
        {
            var role = await db.RoleDefinitions.Include(r => r.Permissions).FirstOrDefaultAsync(r => r.RoleCode == roleCode, ct);
            if (role == null) continue;
            var viewPerm = await db.Permissions.FirstOrDefaultAsync(p => p.Code == "Compliance.Standard.View", ct);
            if (viewPerm == null) continue;
            if (!role.Permissions.Any(a => a.PermissionId == viewPerm.Id && !a.IsRevoked))
                role.GrantPermission(viewPerm.Id, Guid.Empty);
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedLookupValuesAsync(AccessGovernanceDbContext db, string categoryCode, (string Code, string Name)[] values, CancellationToken ct)
    {
        var cat = await db.LookupCategories.FirstAsync(c => c.Code == categoryCode, ct);
        var order = 0;
        foreach (var (code, name) in values)
        {
            if (await db.LookupValues.AnyAsync(v => v.CategoryId == cat.Id && v.Code == code, ct)) continue;
            db.LookupValues.Add(new AccessGovernanceLookupValue { Id = Guid.NewGuid(), CategoryId = cat.Id, Code = code, Name = name, SortOrder = order++ });
        }
        await db.SaveChangesAsync(ct);
    }
}

public class AccessGovernanceMasterDataBootstrapHostedService(IServiceProvider sp) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessGovernanceDbContext>();
        await AccessGovernanceMasterDataSeeder.SeedAsync(db, stoppingToken);
    }
}
