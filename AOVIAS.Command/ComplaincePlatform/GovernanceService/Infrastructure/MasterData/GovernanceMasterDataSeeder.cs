using GovernanceService.Domain.Aggregates;
using GovernanceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GovernanceService.Infrastructure.MasterData;

public static class GovernanceMasterDataSeeder
{
    public static readonly string[] Categories =
    [
        "ResponsibilityType", "CommitteeType", "ApprovalType", "ApprovalLevel", "EscalationType",
        "CommitteeRole", "MeetingType", "DecisionType", "GovernanceStatus", "GovernedResourceType", "AssigneeType"
    ];

    public static async Task SeedAsync(GovernanceDbContext db, CancellationToken ct = default)
    {
        foreach (var code in Categories)
        {
            if (!await db.LookupCategories.AnyAsync(c => c.Code == code, ct))
                db.LookupCategories.Add(new Domain.Entities.GovernanceLookupCategory { Id = Guid.NewGuid(), Code = code, Name = code });
        }
        await db.SaveChangesAsync(ct);

        await SeedValuesAsync(db, "ResponsibilityType", [
            ("ProcessOwner", "Process Owner"), ("PolicyOwner", "Policy Owner"), ("EvidenceOwner", "Evidence Owner"),
            ("CapaOwner", "CAPA Owner"), ("RiskOwner", "Risk Owner"), ("AuditLead", "Audit Lead"),
            ("NabhCoordinator", "NABH Coordinator"), ("ClinicalLead", "Clinical Lead"), ("AiSupervisor", "AI Supervisor"),
            ("PatientSafetyOfficer", "Patient Safety Officer"), ("CommitteeChairperson", "Committee Chairperson"),
            ("ActionOwner", "Action Owner"), ("QualityManager", "Quality Manager"), ("DepartmentHead", "Department Head")
        ], ct);

        await SeedValuesAsync(db, "CommitteeType", [
            ("Quality", "Quality Committee"), ("PatientSafety", "Patient Safety Committee"),
            ("InfectionControl", "Infection Control Committee"), ("NabhSteering", "NABH Steering Committee"),
            ("MortalityReview", "Mortality Review Committee"), ("ManagementReview", "Management Review")
        ], ct);

        await SeedAgendaTemplatesAsync(db, ct);

        await SeedValuesAsync(db, "ApprovalType", [
            ("PolicyApproval", "Policy Approval"), ("CapaApproval", "CAPA Approval"),
            ("EvidenceApproval", "Evidence Approval"), ("AuditApproval", "Audit Approval")
        ], ct);

        await SeedValuesAsync(db, "AssigneeType", [
            ("Person", "Person"), ("Employment", "Employment"), ("Position", "Position"),
            ("Role", "Role"), ("Department", "Department"), ("Committee", "Committee"), ("User", "User")
        ], ct);

        await SeedValuesAsync(db, "GovernedResourceType", [
            ("Framework", "Framework"), ("Standard", "Standard"), ("ObjectiveElement", "Objective Element"),
            ("Policy", "Policy"), ("Evidence", "Evidence"), ("Capa", "CAPA"), ("Risk", "Risk"), ("Task", "Task"),
            ("Department", "Department"), ("ComplianceJourney", "Compliance Journey")
        ], ct);

        await SeedValuesAsync(db, "MeetingType", [
            ("Regular", "Regular Meeting"), ("Emergency", "Emergency Meeting"), ("Annual", "Annual Review"),
            ("ManagementReview", "Management Review Meeting")
        ], ct);

        await SeedValuesAsync(db, "DecisionType", [
            ("Approval", "Approval"), ("Action", "Action Item"), ("PolicyChange", "Policy Change")
        ], ct);

        await SeedValuesAsync(db, "CommitteeRole", [
            ("Chairperson", "Chairperson"), ("Secretary", "Secretary"), ("Member", "Member"), ("Invitee", "Invitee")
        ], ct);
    }

    private static async Task SeedValuesAsync(GovernanceDbContext db, string categoryCode, (string Code, string Name)[] values, CancellationToken ct)
    {
        var cat = await db.LookupCategories.FirstAsync(c => c.Code == categoryCode, ct);
        var order = 0;
        foreach (var (code, name) in values)
        {
            if (await db.LookupValues.AnyAsync(v => v.CategoryId == cat.Id && v.Code == code, ct)) continue;
            db.LookupValues.Add(new Domain.Entities.GovernanceLookupValue { Id = Guid.NewGuid(), CategoryId = cat.Id, Code = code, Name = name, SortOrder = order++ });
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedAgendaTemplatesAsync(GovernanceDbContext db, CancellationToken ct)
    {
        await EnsureTemplateAsync(db, "Standard", "Standard Committee Agenda", false, 0, [
            "Call to order / attendance", "Previous minutes", "Standing reports", "New business", "Actions & close"
        ], ct);
        await EnsureTemplateAsync(db, "ManagementReview", "Management Review Agenda", true, 1, [
            "Quality policy and objectives",
            "Incident / patient safety summary",
            "CAPA / deficiency status",
            "Risk register review",
            "Internal / external audit outcomes",
            "Resource needs and decisions"
        ], ct);
        await EnsureTemplateAsync(db, "PatientSafety", "Patient Safety Committee Agenda", false, 2, [
            "Safety events review", "Open RCA / CAPA", "Indicators", "Actions"
        ], ct);
    }

    private static async Task EnsureTemplateAsync(
        GovernanceDbContext db, string code, string name, bool isMr, int sort, string[] sections, CancellationToken ct)
    {
        if (await db.AgendaTemplates.AnyAsync(t => t.Code == code, ct)) return;
        db.AgendaTemplates.Add(new Domain.Entities.AgendaTemplate
        {
            Id = Guid.NewGuid(),
            Code = code,
            Name = name,
            IsManagementReview = isMr,
            SortOrder = sort,
            SectionsJson = System.Text.Json.JsonSerializer.Serialize(sections),
            Description = name
        });
        await db.SaveChangesAsync(ct);
    }
}

public class GovernanceMasterDataBootstrapHostedService(IServiceProvider sp) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = sp.CreateScope();
        await GovernanceMasterDataSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<GovernanceDbContext>(), stoppingToken);
    }
}
