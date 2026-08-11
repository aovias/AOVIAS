namespace Shared.Logging.Platform;

/// <summary>
/// Canonical PRAMAN service registrations for AOVIAS Platform hosting.
/// </summary>
public static class PramanServiceCatalog
{
    public static PramanServiceRegistration Gateway { get; } = Create(
        "praman-gateway",
        "PRAMAN Gateway",
        "Gateway",
        ["routing", "bff", "api-gateway"],
        ["praman-identity", "praman-person", "praman-workforce", "praman-professional", "praman-access-governance", "praman-governance", "praman-communication", "praman-workforce-intelligence", "praman-enterprise-directory", "praman-integration", "praman-organization", "praman-compliance", "praman-audit", "praman-knowledgebase"],
        "PKG-PRAMAN-GATEWAY");

    public static PramanServiceRegistration Identity { get; } = Create(
        "praman-identity",
        "PRAMAN Identity",
        "Identity",
        ["authentication", "authorization", "identity"],
        ["praman-person"],
        "PKG-PRAMAN-IDENTITY",
        migrationPackage: "IdentityService/Migrations",
        seedData: "IdentityService/Seed");

    public static PramanServiceRegistration Person { get; } = Create(
        "praman-person",
        "PRAMAN Person",
        "Person",
        ["person", "profile", "identity"],
        ["praman-identity"],
        "PKG-PRAMAN-PERSON",
        migrationPackage: "PersonService/Migrations",
        seedData: "PersonService/Infrastructure/MasterData");

    public static PramanServiceRegistration Workforce { get; } = Create(
        "praman-workforce",
        "PRAMAN Workforce",
        "Workforce",
        ["workforce", "employment", "org-chart"],
        ["praman-person", "praman-organization"],
        "PKG-PRAMAN-WORKFORCE",
        migrationPackage: "WorkforceService/Migrations",
        seedData: "WorkforceService/Infrastructure/MasterData");

    public static PramanServiceRegistration Professional { get; } = Create(
        "praman-professional",
        "PRAMAN Professional",
        "Professional",
        ["professional", "credentialing", "clinical-privileges"],
        ["praman-workforce", "praman-organization"],
        "PKG-PRAMAN-PROFESSIONAL",
        migrationPackage: "ProfessionalService/Migrations",
        seedData: "ProfessionalService/Infrastructure/MasterData");

    public static PramanServiceRegistration AccessGovernance { get; } = Create(
        "praman-access-governance",
        "PRAMAN Access Governance",
        "AccessGovernance",
        ["authorization", "rbac", "abac", "delegation", "governance"],
        ["praman-identity", "praman-organization", "praman-person", "praman-workforce", "praman-professional"],
        "PKG-PRAMAN-ACCESS-GOVERNANCE",
        migrationPackage: "AccessGovernanceService/Migrations",
        seedData: "AccessGovernanceService/Infrastructure/MasterData");

    public static PramanServiceRegistration Governance { get; } = Create(
        "praman-governance",
        "PRAMAN Governance",
        "Governance",
        ["governance", "responsibility", "committee", "approval", "escalation"],
        ["praman-identity", "praman-organization", "praman-person", "praman-workforce", "praman-professional", "praman-access-governance"],
        "PKG-PRAMAN-GOVERNANCE",
        migrationPackage: "GovernanceService/Migrations",
        seedData: "GovernanceService/Infrastructure/MasterData");

    public static PramanServiceRegistration Communication { get; } = Create(
        "praman-communication",
        "PRAMAN Communication",
        "Communication",
        ["communication", "notifications", "messaging", "reminders"],
        ["praman-identity", "praman-person", "praman-workforce", "praman-organization"],
        "PKG-PRAMAN-COMMUNICATION",
        migrationPackage: "CommunicationService/Migrations");

    public static PramanServiceRegistration WorkforceIntelligence { get; } = Create(
        "praman-workforce-intelligence",
        "PRAMAN Workforce Intelligence",
        "Intelligence",
        ["intelligence", "analytics", "workforce", "forecasting", "recommendations"],
        ["praman-workforce", "praman-professional", "praman-organization", "praman-governance", "praman-person", "praman-communication", "praman-ai-orchestrator"],
        "PKG-PRAMAN-WORKFORCE-INTELLIGENCE",
        migrationPackage: "WorkforceIntelligenceService/Migrations");

    public static PramanServiceRegistration EnterpriseDirectory { get; } = Create(
        "praman-enterprise-directory",
        "PRAMAN Enterprise Directory",
        "Directory",
        ["directory", "search", "discovery", "profiles", "enterprise"],
        ["praman-person", "praman-workforce", "praman-professional", "praman-organization", "praman-governance", "praman-access-governance", "praman-ai-orchestrator"],
        "PKG-PRAMAN-ENTERPRISE-DIRECTORY",
        migrationPackage: "EnterpriseDirectoryService/Migrations");

    public static PramanServiceRegistration Integration { get; } = Create(
        "praman-integration",
        "PRAMAN Enterprise Integration",
        "Integration",
        ["integration", "connectors", "sync", "webhooks", "import", "export"],
        ["praman-person", "praman-workforce", "praman-organization", "praman-access-governance", "praman-communication"],
        "PKG-PRAMAN-INTEGRATION",
        migrationPackage: "IntegrationService/Migrations");

    public static PramanServiceRegistration Organization { get; } = Create(
        "praman-organization",
        "PRAMAN Organization",
        "Organization",
        ["organization", "tenant", "membership"],
        ["praman-identity"],
        "PKG-PRAMAN-ORGANIZATION",
        migrationPackage: "OrganizationService/Infrastructure/Migrations");

    public static PramanServiceRegistration Compliance { get; } = Create(
        "praman-compliance",
        "PRAMAN Compliance",
        "Compliance",
        ["compliance", "findings", "tasks", "capa"],
        ["praman-identity", "praman-organization", "praman-knowledgebase"],
        "PKG-PRAMAN-COMPLIANCE",
        migrationPackage: "ComplianceService/Migrations",
        seedData: "ComplianceService/Seed");

    public static PramanServiceRegistration Audit { get; } = Create(
        "praman-audit",
        "PRAMAN Audit",
        "Audit",
        ["audit", "evidence-trail"],
        ["praman-identity", "praman-compliance"],
        "PKG-PRAMAN-AUDIT",
        migrationPackage: "AuditService/Migrations");

    public static PramanServiceRegistration KnowledgeBase { get; } = Create(
        "praman-knowledgebase",
        "PRAMAN Knowledge Base",
        "KnowledgeBase",
        ["knowledgebase", "evidence", "frameworks"],
        ["praman-identity"],
        "PKG-PRAMAN-KNOWLEDGEBASE",
        migrationPackage: "KnowledgeBase/Infrastructure/Persistence/Migrations");

    public static PramanServiceRegistration AiOrchestrator { get; } = Create(
        "praman-ai-orchestrator",
        "PRAMAN AI Orchestrator",
        "AI",
        ["ai", "orchestration"],
        ["praman-identity", "praman-compliance", "praman-knowledgebase"],
        "PKG-PRAMAN-AI");

    public static PramanServiceRegistration OutboxRelay { get; } = Create(
        "praman-outbox-relay",
        "PRAMAN Outbox Relay",
        "Worker",
        ["messaging", "outbox-relay"],
        ["praman-compliance", "praman-audit"],
        "PKG-PRAMAN-OUTBOX");

    public static PramanServiceRegistration WebApp { get; } = Create(
        "praman-web",
        "PRAMAN Web Application",
        "WebApp",
        ["web", "angular", "operator-ui"],
        ["praman-gateway"],
        "PKG-PRAMAN-WEB");

    public static IReadOnlyList<PramanServiceRegistration> All { get; } =
    [
        Gateway,
        Identity,
        Person,
        Workforce,
        Professional,
        AccessGovernance,
        Governance,
        Communication,
        WorkforceIntelligence,
        EnterpriseDirectory,
        Integration,
        Organization,
        Compliance,
        Audit,
        KnowledgeBase,
        AiOrchestrator,
        OutboxRelay,
        WebApp
    ];

    private static PramanServiceRegistration Create(
        string serviceId,
        string serviceName,
        string category,
        IReadOnlyList<string> capabilities,
        IReadOnlyList<string> dependencies,
        string packageId,
        string? migrationPackage = null,
        string? seedData = null)
    {
        var metadata = new Dictionary<string, string>
        {
            ["healthEndpoint"] = "/health",
            ["readinessEndpoint"] = "/readiness",
            ["livenessEndpoint"] = "/liveness",
            ["registrationEndpoint"] = "/platform/registration",
            ["rollbackSupported"] = "true"
        };

        if (!string.IsNullOrWhiteSpace(migrationPackage))
        {
            metadata["migrationPackage"] = migrationPackage;
        }

        if (!string.IsNullOrWhiteSpace(seedData))
        {
            metadata["seedData"] = seedData;
        }

        return new PramanServiceRegistration
        {
            ApplicationId = "praman",
            ServiceId = serviceId,
            ServiceName = serviceName,
            Version = "1.0.0",
            HealthEndpoint = "/health",
            ReadinessEndpoint = "/readiness",
            LivenessEndpoint = "/liveness",
            Capabilities = capabilities,
            Dependencies = dependencies,
            Category = category,
            PackageId = packageId,
            DeploymentPackage = $"{packageId}.aovpkg",
            DeploymentMetadata = metadata
        };
    }
}
