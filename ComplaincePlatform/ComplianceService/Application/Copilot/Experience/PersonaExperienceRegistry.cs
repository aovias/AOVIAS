using ComplianceService.Domain.Copilot;
using ComplianceService.Domain.Copilot.Enums;

namespace ComplianceService.Application.Copilot.Experience;

/// <summary>Sprint 13E — single source of persona experience definitions.</summary>
public static class PersonaExperienceRegistry
{
    private static readonly IReadOnlyList<PersonaExperienceDefinition> All = BuildAll();

    public static IReadOnlyList<PersonaExperienceDefinition> GetAll() => All;

    public static PersonaExperienceDefinition Get(CopilotPersona persona) =>
        All.FirstOrDefault(d => d.Persona == persona)
        ?? All.First(d => d.Persona == CopilotPersona.AccreditationCoach);

    private static IReadOnlyList<PersonaExperienceDefinition> BuildAll() =>
    [
        new PersonaExperienceDefinition
        {
            Persona = CopilotPersona.AccreditationCoach,
            DisplayName = "Accreditation Coach",
            Description = "Guides accreditation readiness, stage unlocks, and framework compliance.",
            Icon = "school",
            Theme = "accreditation",
            PrimaryObjectives = ["Unlock maturity stages", "Close readiness gaps", "Resolve blocking deficiencies"],
            RecommendedActions = ["Generate readiness summary", "Review stage blockers", "Create remediation plan"],
            DefaultQuestions = [
                "What blocks accreditation?",
                "What should I fix first?",
                "How do I unlock the next stage?",
                "Which evidence gaps matter most?",
            ],
            PriorityCategories = [
                RecommendationCategory.Accreditation,
                RecommendationCategory.Verification,
                RecommendationCategory.Evidence,
                RecommendationCategory.Deficiency,
            ],
            SupportedDrafts = [DraftType.ReadinessSummary, DraftType.EvidenceChecklist, DraftType.AuditPreparationBrief],
            SupportedProposals = [ProposalType.CreateReadinessPlan, ProposalType.OpenEvidenceChecklist, ProposalType.CreateRemediationPlan],
        },
        new PersonaExperienceDefinition
        {
            Persona = CopilotPersona.QualityManagerAssistant,
            DisplayName = "Quality Manager Assistant",
            Description = "Supports quality managers with task prioritization and compliance planning.",
            Icon = "verified",
            Theme = "quality",
            PrimaryObjectives = ["Prioritize compliance tasks", "Close evidence gaps", "Track SLA adherence"],
            RecommendedActions = ["Review top recommendations", "Assign remediation tasks", "Generate evidence checklist"],
            DefaultQuestions = [
                "What tasks need attention today?",
                "Which deficiencies are overdue?",
                "What evidence is missing?",
            ],
            PriorityCategories = [
                RecommendationCategory.Task,
                RecommendationCategory.Deficiency,
                RecommendationCategory.Evidence,
                RecommendationCategory.SLA,
            ],
            SupportedDrafts = [DraftType.EvidenceChecklist, DraftType.ManagementReviewSummary, DraftType.ReadinessSummary],
            SupportedProposals = [ProposalType.CreateTask, ProposalType.AssignTask, ProposalType.OpenEvidenceChecklist],
        },
        new PersonaExperienceDefinition
        {
            Persona = CopilotPersona.EvidenceAdvisor,
            DisplayName = "Evidence Advisor",
            Description = "Helps collect, verify, and link evidence to obligations.",
            Icon = "folder_open",
            Theme = "evidence",
            PrimaryObjectives = ["Close evidence gaps", "Clear verification backlog", "Map evidence to obligations"],
            RecommendedActions = ["Open evidence checklist", "Review pending verification", "Upload missing evidence"],
            DefaultQuestions = [
                "Which obligations are missing evidence?",
                "What is pending verification?",
                "Which evidence should I upload first?",
            ],
            PriorityCategories = [
                RecommendationCategory.Evidence,
                RecommendationCategory.Verification,
                RecommendationCategory.Accreditation,
            ],
            SupportedDrafts = [DraftType.EvidenceChecklist, DraftType.ReadinessSummary],
            SupportedProposals = [ProposalType.OpenEvidenceChecklist, ProposalType.CreateTask],
        },
        new PersonaExperienceDefinition
        {
            Persona = CopilotPersona.CapaAdvisor,
            DisplayName = "CAPA Advisor",
            Description = "Advises on deficiency remediation, CAPA effectiveness, and RCA.",
            Icon = "assignment_turned_in",
            Theme = "capa",
            PrimaryObjectives = ["Close overdue CAPAs", "Address recurring deficiencies", "Schedule effectiveness reviews"],
            RecommendedActions = ["Create CAPA draft", "Ensure remediation task", "Schedule effectiveness review"],
            DefaultQuestions = [
                "Show recurring issues.",
                "Which CAPAs are ineffective?",
                "What deficiencies need CAPA?",
                "Which effectiveness reviews are overdue?",
            ],
            PriorityCategories = [
                RecommendationCategory.CAPA,
                RecommendationCategory.Deficiency,
                RecommendationCategory.Risk,
                RecommendationCategory.SLA,
            ],
            SupportedDrafts = [DraftType.CapaDraft, DraftType.ManagementReviewSummary],
            SupportedProposals = [
                ProposalType.CreateCapa,
                ProposalType.CreateTask,
                ProposalType.ScheduleEffectivenessReview,
                ProposalType.CreateRemediationPlan,
            ],
        },
        new PersonaExperienceDefinition
        {
            Persona = CopilotPersona.ExecutiveAdvisor,
            DisplayName = "Executive Advisor",
            Description = "Executive compliance summaries, risk exposure, and governance oversight.",
            Icon = "insights",
            Theme = "executive",
            PrimaryObjectives = ["Monitor readiness trends", "Surface leadership risks", "Track escalations"],
            RecommendedActions = ["Generate executive briefing", "Review readiness summary", "Approve high-impact proposals"],
            DefaultQuestions = [
                "What changed this week?",
                "What needs leadership attention?",
                "How is accreditation readiness trending?",
                "What are our top compliance risks?",
            ],
            PriorityCategories = [
                RecommendationCategory.Executive,
                RecommendationCategory.Analytics,
                RecommendationCategory.Escalation,
                RecommendationCategory.Risk,
                RecommendationCategory.Accreditation,
                RecommendationCategory.Department,
            ],
            SupportedDrafts = [DraftType.ExecutiveComplianceBriefing, DraftType.ReadinessSummary, DraftType.ManagementReviewSummary],
            SupportedProposals = [ProposalType.GenerateExecutiveBriefing, ProposalType.CreateReadinessPlan, ProposalType.CreateFinding],
        },
        new PersonaExperienceDefinition
        {
            Persona = CopilotPersona.DepartmentHeadAssistant,
            DisplayName = "Department Head Assistant",
            Description = "Department-level tasks, SLA adherence, and team assignments.",
            Icon = "groups",
            Theme = "department",
            PrimaryObjectives = ["Clear department tasks", "Prevent SLA breaches", "Assign team remediation"],
            RecommendedActions = ["Review department proposals", "Assign tasks", "Track open deficiencies"],
            DefaultQuestions = [
                "What does my department need to fix?",
                "Which tasks are overdue in my department?",
                "What proposals need my approval?",
            ],
            PriorityCategories = [
                RecommendationCategory.Department,
                RecommendationCategory.Task,
                RecommendationCategory.SLA,
                RecommendationCategory.Deficiency,
            ],
            SupportedDrafts = [DraftType.ManagementReviewSummary, DraftType.EvidenceChecklist],
            SupportedProposals = [ProposalType.AssignTask, ProposalType.CreateTask, ProposalType.CreateRemediationPlan],
        },
        new PersonaExperienceDefinition
        {
            Persona = CopilotPersona.AuditorAssistant,
            DisplayName = "Auditor Assistant",
            Description = "Supports audit finding review, observations, and verification workflows.",
            Icon = "search",
            Theme = "audit",
            PrimaryObjectives = ["Review open findings", "Clear verification backlog", "Document audit observations"],
            RecommendedActions = ["Create finding proposal", "Review evidence verification", "Open audit brief"],
            DefaultQuestions = [
                "What findings are open?",
                "Which items need verification?",
                "What evidence supports this finding?",
            ],
            PriorityCategories = [
                RecommendationCategory.Verification,
                RecommendationCategory.Escalation,
                RecommendationCategory.Evidence,
                RecommendationCategory.Deficiency,
            ],
            SupportedDrafts = [DraftType.AuditPreparationBrief, DraftType.EvidenceChecklist],
            SupportedProposals = [ProposalType.CreateFinding, ProposalType.OpenEvidenceChecklist],
        },
    ];
}
