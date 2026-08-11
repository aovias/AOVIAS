using System;
using Compliance.MasterData.Contracts;
using ComplianceService.Application.AI;
using ComplianceService.Application.AI.Contracts;
using ComplianceService.Application.AI.Providers;
using ComplianceService.Application.Contracts;
using ComplianceService.Application.DTOs;
using ComplianceService.Application.Services;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Clients;
using ComplianceService.Infrastructure.Extensions;
using ComplianceService.Infrastructure.Filters;
using ComplianceService.Infrastructure.Persistence;
using ComplianceService.Infrastructure.Repositories;
using ComplianceService.Infrastructure.Services;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using ComplianceService.Infrastructure.Filters;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Platform.Dashboard.Extensions;
using Shared.Communication;
using Shared.Infrastructure.Api;
using Shared.Infrastructure.DTO;
using ComplianceService.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shared.Infrastructure.Services;
using Shared.Infrastructure.Caching;
using Shared.Logging;
using Shared.Logging.Diagnostics;
using Shared.Logging.Health;
using Shared.Logging.OpenTelemetry;
using Shared.Logging.Platform;
using Shared.Authorization;
using Shared.Governance;

SerilogBootstrap.CreateBootstrapLogger("ComplianceService");

var builder = WebApplication.CreateBuilder(args);
builder.ConfigureSerilog("ComplianceService");
builder.AddAoviasOpenTelemetry("ComplianceService");
builder.Services.AddPramanLogging();
builder.Services.AddPramanAoviasPlatform(PramanServiceCatalog.Compliance);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ComplianceDbContext>("database");

var redisConnection = RedisConfiguration.GetConnectionString(builder.Configuration);
if (redisConnection != null)
{
    builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
}

const long EvidenceMultipartMaxBytes = 10_485_760;
builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = EvidenceMultipartMaxBytes);
builder.Services.Configure<KestrelServerOptions>(o => o.Limits.MaxRequestBodySize = EvidenceMultipartMaxBytes * 20);
builder.Services.Configure<EvidenceOptions>(builder.Configuration.GetSection(EvidenceOptions.SectionName));

// Add services to the container.
builder.Services.AddSingleton<IgnoreRequestAbortedExceptionFilter>();
builder.Services.AddControllers(options =>
    {
        options.Filters.Add<IgnoreRequestAbortedExceptionFilter>();
    })
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false));
    });
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var traceId = context.HttpContext.TraceIdentifier;
        var errors = context.ModelState
            .Where(kvp => kvp.Value?.Errors.Count > 0)
            .SelectMany(kvp => kvp.Value!.Errors.Select(e => new ApiError
            {
                Field = kvp.Key,
                Message = string.IsNullOrEmpty(e.ErrorMessage)
                    ? e.Exception?.Message ?? "Invalid value."
                    : e.ErrorMessage
            }))
            .ToList();
        if (errors.Count == 0)
        {
            errors.Add(new ApiError { Message = "Input validation failed." });
        }

        var envelope = ApiResponseFactory.Failure<object>(ApiResponseCode.VALIDATION_FAILED, errors, traceId);
        var meta = ApiResponseCodes.Registry[ApiResponseCode.VALIDATION_FAILED];
        return new ObjectResult(envelope) { StatusCode = meta.HttpStatus };
    };
});
builder.Services.AddEndpointsApiExplorer();
// Swagger UI temporarily disabled - Swashbuckle.AspNetCore is not compatible with .NET 10
// OpenAPI JSON is still available via Microsoft.AspNetCore.OpenApi at /openapi/v1.json
// TODO: Re-enable Swagger UI once Swashbuckle.AspNetCore supports .NET 10
// builder.Services.AddSwaggerGen();
builder.Services.AddCorsConfiguration(builder.Configuration);

builder.Services.AddDatabase(builder.Configuration);
builder.Services.AddMasterData();

builder.Services.AddSecurityServices();
// JWT validation - validates tokens issued by IdentityService
builder.Services.AddJwtValidation(builder.Configuration);
builder.Services.AddTransient<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();
builder.Services.AddPramanAuthorization(builder.Configuration);
builder.Services.AddPramanGovernance(builder.Configuration);
builder.Services.AddCorsConfiguration(builder.Configuration);
builder.Services.AddRateLimiting(builder.Configuration);

// Hangfire configuration for background jobs
builder.Services.AddHangfireConfiguration(builder.Configuration);

builder.Services.AddApiVersioningSystem();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "application/json" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

// Register UserContextService first (required by repositories)
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IOrganizationServiceClient, OrganizationServiceClient>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["OrganizationService:BaseUrl"] ?? "https://localhost:7181";
    client.BaseAddress = new Uri(baseUrl);
})
.AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
.AddHttpMessageHandler<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();

builder.Services.AddHttpClient<IKnowledgeBaseServiceClient, KnowledgeBaseServiceClient>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var baseUrl = configuration["KnowledgeBaseService:BaseUrl"] ?? "https://localhost:7167";
    client.BaseAddress = new Uri(baseUrl);
})
.AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>();
builder.Services.AddHttpClient<IAiProfileBridgeClient, AiProfileBridgeClient>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(configuration["AIOrchestratorService:BaseUrl"] ?? "https://localhost:5003");
})
.AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
.AddHttpMessageHandler<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();
builder.Services.AddHttpClient<IAiOrchestratorAssistClient, AiOrchestratorAssistClient>((sp, client) =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(configuration["AIOrchestratorService:BaseUrl"] ?? "https://localhost:5003");
})
.AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
.AddHttpMessageHandler<Shared.Infrastructure.Auth.InternalApiKeyForwardingHandler>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IUserContextService, ComplianceService.Infrastructure.Services.UserContextService>();

// Execution context: the tenant source that also works on internal API-key routes, where there is
// no user principal for IUserContextService to read. ComplianceDbContext prefers this and falls
// back to IUserContextService for direct user requests. See ADR-002.
builder.Services.AddScoped<Shared.Infrastructure.Execution.IExecutionContextAccessor,
    Shared.Infrastructure.Execution.ExecutionContextAccessor>();

builder.Services.AddScoped<IRegulationRepository, RegulationRepository>();
builder.Services.AddScoped<IComplianceProgramRepository, ComplianceProgramRepository>();
builder.Services.AddScoped<IControlRepository, ControlRepository>();
builder.Services.AddScoped<IComplianceTaskRepository, ComplianceTaskRepository>();
builder.Services.AddScoped<IComplianceTaskAppService, ComplianceTaskAppService>();
builder.Services.AddScoped<ITaskQueryService, TaskQueryService>();
builder.Services.AddScoped<ITaskExecutionService, TaskExecutionService>();
builder.Services.AddScoped<IEvidenceRepository, EvidenceRepository>();
builder.Services.AddScoped<IHospitalComplianceRepository, HospitalComplianceRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Compliance Journey Engine repositories
builder.Services.AddScoped<IComplianceJourneyRepository, ComplianceJourneyRepository>();
builder.Services.AddScoped<IComplianceJourneyHistoryRepository, ComplianceJourneyHistoryRepository>();
builder.Services.AddScoped<IFrameworkConfigurationRepository, FrameworkConfigurationRepository>();
builder.Services.AddScoped<IComplianceAuditFindingRepository, ComplianceAuditFindingRepository>();

builder.Services.AddScoped<IFindingAdapter, FindingAdapter>();
builder.Services.AddScoped<IFindingQueryService, FindingQueryService>();
builder.Services.AddScoped<IFindingsWorkspaceQueryService, FindingsWorkspaceQueryService>();
builder.Services.AddScoped<AddFindingCommentCommandHandler>();
builder.Services.AddScoped<AddFindingAttachmentCommandHandler>();
builder.Services.AddScoped<PatchFindingStatusCommandHandler>();
builder.Services.AddScoped<UpdateFindingComplianceContextCommandHandler>();
builder.Services.AddScoped<IFindingAuditTrailService, FindingAuditTrailService>();
builder.Services.AddScoped<IFindingPromotionEligibilityService, FindingPromotionEligibilityService>();
builder.Services.AddScoped<IFindingPromotionService, FindingPromotionService>();
builder.Services.AddScoped<IIncidentQueryService, IncidentQueryService>();
builder.Services.AddScoped<IIncidentCommandService, IncidentCommandService>();
builder.Services.AddScoped<IIncidentPromotionService, IncidentPromotionService>();
builder.Services.AddScoped<IClinicalIndicatorQueryService, ClinicalIndicatorQueryService>();
builder.Services.AddScoped<IClinicalIndicatorCommandService, ClinicalIndicatorCommandService>();
builder.Services.AddScoped<ClinicalIndicatorMetricsService>();
builder.Services.AddScoped<ClinicalIndicatorCatalogSeeder>();
builder.Services.AddScoped<IEnterpriseComplianceGraphService, EnterpriseComplianceGraphService>();
builder.Services.AddScoped<IAuditorWorkspaceQueryService, AuditorWorkspaceQueryService>();
builder.Services.AddScoped<IAuditorTraceabilityService, AuditorTraceabilityService>();
builder.Services.AddScoped<IAuditorReportService, AuditorReportService>();
builder.Services.AddScoped<IAuditScheduleService, AuditScheduleService>();
builder.Services.AddScoped<IAuditGenerateFacadeService, AuditGenerateFacadeService>();
builder.Services.AddScoped<IAuditSimulationAppService, AuditSimulationAppService>();
builder.Services.AddScoped<IDocumentationQueryService, DocumentationQueryService>();
builder.Services.AddScoped<IDocumentationCommandService, DocumentationCommandService>();
builder.Services.AddScoped<IDocumentationAuthoringService, DocumentationAuthoringService>();
builder.Services.AddScoped<IDocumentationIntelligenceService, DocumentationIntelligenceService>();
builder.Services.AddScoped<IRiskRegisterAppService, RiskRegisterAppService>();
builder.Services.AddScoped<IDataMigrationValidationService, DataMigrationValidationService>();
builder.Services.AddScoped<IShadowComparisonService, ShadowComparisonService>();
builder.Services.AddScoped<IAIContextBundleService, AIContextBundleService>();
builder.Services.AddScoped<IAIDeficiencyContextProvider, AIDeficiencyContextProvider>();
builder.Services.AddScoped<IAICapaContextProvider, AICapaContextProvider>();
builder.Services.AddScoped<IAISlaContextProvider, AISlaContextProvider>();
builder.Services.AddScoped<IAIMaturityContextProvider, AIMaturityContextProvider>();
builder.Services.AddScoped<IAIAnalyticsContextProvider, AIAnalyticsContextProvider>();
builder.Services.AddScoped<IAIExplainContextService, AIExplainContextService>();

// Sprint 13A – Copilot Core Foundation
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.ICopilotRecommendationRepository, ComplianceService.Infrastructure.Copilot.Repositories.CopilotRecommendationRepository>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IAiDraftRepository, ComplianceService.Infrastructure.Copilot.Repositories.AiDraftRepository>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.ICopilotPersonaRouter, ComplianceService.Application.Copilot.Services.CopilotPersonaRouter>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.ICopilotPersonaService, ComplianceService.Application.Copilot.Services.CopilotPersonaService>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.ICopilotRecommendationService, ComplianceService.Application.Copilot.Services.CopilotRecommendationService>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.ICopilotDraftService, ComplianceService.Application.Copilot.Services.CopilotDraftService>();

// Sprint 13B – Recommendation Engine
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRankingService, ComplianceService.Application.Copilot.Services.RecommendationRankingService>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationExplainerService, ComplianceService.Application.Copilot.Services.RecommendationExplainerService>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationEngine, ComplianceService.Application.Copilot.Services.RecommendationEngine>();

// Sprint 13C – Draft Generation Engine
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IDraftGenerationService, ComplianceService.Application.Copilot.Services.DraftGenerationService>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IDraftContentGenerator, ComplianceService.Application.Copilot.Services.DraftContentGenerators.TemplateDraftContentGenerator>();

// Sprint 13C – Draft Templates (registered as IDraftTemplate for DraftTemplateRegistry)
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IDraftTemplate, ComplianceService.Application.Copilot.Templates.CapaDraftTemplate>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IDraftTemplate, ComplianceService.Application.Copilot.Templates.ExecutiveComplianceBriefingTemplate>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IDraftTemplate, ComplianceService.Application.Copilot.Templates.ReadinessSummaryTemplate>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IDraftTemplate, ComplianceService.Application.Copilot.Templates.EvidenceChecklistTemplate>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Templates.DraftTemplateRegistry>();

// Sprint 13D – Approval Card Framework
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.ICopilotActionProposalRepository,
    ComplianceService.Infrastructure.Copilot.Repositories.CopilotActionProposalRepository>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IActionProposalGenerator,
    ComplianceService.Application.Copilot.Services.ActionProposalGenerator>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IApprovalPolicyService,
    ComplianceService.Application.Copilot.Services.ApprovalPolicyService>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IProposalExecutionService,
    ComplianceService.Application.Copilot.Services.ProposalExecutionService>();

// Sprint 13E – Persona Experience Layer
builder.Services.AddScoped<ComplianceService.Application.Copilot.Services.PersonaLandingStateBuilder>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IPersonaExperienceService,
    ComplianceService.Application.Copilot.Services.PersonaExperienceService>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.ICopilotActivityService,
    ComplianceService.Application.Copilot.Services.CopilotActivityService>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.ICopilotActivityRepository,
    ComplianceService.Infrastructure.Copilot.Repositories.CopilotActivityRepository>();

// Phase 3 Sprint 1 – Role Experience / Persona Intelligence
builder.Services.AddScoped<ComplianceService.Application.RoleExperience.IRoleExperienceService,
    ComplianceService.Application.RoleExperience.RoleExperienceService>();

// Sprint 13F – Copilot Observability
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.ICopilotAnalyticsRepository,
    ComplianceService.Infrastructure.Copilot.Repositories.CopilotAnalyticsRepository>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.ICopilotAnalyticsService,
    ComplianceService.Application.Copilot.Services.CopilotAnalyticsService>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationImpactAnalysisService,
    ComplianceService.Application.Copilot.Services.RecommendationImpactAnalysisService>();

// Sprint 13B – Recommendation Rules (registered as IRecommendationRule via open-generic enumerable)
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Accreditation.StageUnlockBlockedRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Accreditation.MissingEvidenceRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Accreditation.VerificationBacklogRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Accreditation.ReadinessGapRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Deficiency.CriticalDeficiencyRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Incident.CriticalIncidentRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Quality.IndicatorThresholdBreachRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Graph.TraceabilityGapRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Deficiency.RecurringDeficiencyRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Deficiency.AgingDeficiencyRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Deficiency.UnassignedDeficiencyRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Capa.OverdueCAPARule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Capa.IneffectiveCAPARule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Capa.RecurringPatternRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Sla.SlaBreachRiskRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Sla.EscalationCandidateRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Evidence.MissingEvidenceObligationRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Evidence.PendingVerificationRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Executive.ReadinessDeclineRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Executive.CriticalEscalationExposureRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.Executive.DepartmentRiskRule>();
// Phase 3 Sprint 1 – role-specific recommendation stubs
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.RoleExperience.OwnerExecutiveAttentionRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.RoleExperience.QualityManagerDailyPrioritiesRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.RoleExperience.DepartmentHeadTaskBacklogRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.RoleExperience.ConsultantPortfolioRiskRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.RoleExperience.AuditorVerificationQueueRule>();
// Phase 3 Sprint 2 – Owner executive NBA rules
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.RoleExperience.OwnerTopBusinessRisksRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.RoleExperience.OwnerDecliningDepartmentsRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.RoleExperience.OwnerNabHReadinessTodayRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.RoleExperience.OwnerApprovalsPendingRule>();
// Phase 3 Sprint 3 – Quality Manager operational NBA rules
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.RoleExperience.QualityManagerUrgentCapasRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.RoleExperience.QualityManagerLaggingDepartmentsRule>();
builder.Services.AddScoped<ComplianceService.Application.Copilot.Contracts.IRecommendationRule, ComplianceService.Application.Copilot.Rules.RoleExperience.QualityManagerOverdueRiskThisWeekRule>();

// Framework Repository and Query Service (CQRS read side)
builder.Services.AddScoped<IFrameworkRepository, FrameworkRepository>();
builder.Services.AddScoped<IFrameworkQueryService, FrameworkQueryService>();
builder.Services.AddScoped<FrameworkLifecycleService>();
builder.Services.AddScoped<ObjectiveElementOwnershipAppService>();
// Phase 2 Sprint 4 — Notification Platform: CommunicationService HTTP when BaseUrl configured; else console.
if (ComplianceNotificationOptions.IsCommunicationConfigured(builder.Configuration))
{
    builder.Services.AddPramanCommunication(builder.Configuration);
    builder.Services.AddScoped<IComplianceNotificationPublisher, CommunicationComplianceNotificationPublisher>();
}
else
{
    builder.Services.AddSingleton<IComplianceNotificationPublisher, ConsoleComplianceNotificationPublisher>();
}

// Framework Enrollment Command Handler
builder.Services.AddScoped<EnrollHospitalInFrameworkCommandHandler>();

// Domain Event Dispatcher - Outbox pattern
builder.Services.AddScoped<IDomainEventDispatcher, OutboxDomainEventDispatcher>();
builder.Services.AddScoped<RegulationService>();
builder.Services.AddScoped<IRegulationService, RegulationService>();
builder.Services.AddScoped<IReasoningCapture, ReasoningCapture>();

// Compliance Scoring Service
builder.Services.AddScoped<AuditSimulationService>();
// Canonical single source of truth for compliance percentages (all screens route through this).
builder.Services.AddScoped<ComplianceService.Application.Contracts.IComplianceScoreService, ComplianceService.Application.Services.ComplianceScoreService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IComplianceScoringService, ComplianceScoringService>();
builder.Services.AddScoped<ComplianceService.Domain.IComplianceScoringService, ComplianceScoringService>();

// Snapshot Service
builder.Services.AddScoped<ISnapshotService, SnapshotService>();
builder.Services.AddScoped<IProgramOverviewSnapshotReadService, ProgramOverviewSnapshotReadService>();
builder.Services.AddScoped<IComplianceFrameworkSnapshotRecalculationService, ComplianceFrameworkSnapshotRecalculationService>();
builder.Services.AddSingleton<IComplianceSnapshotRecalculationQueue, ComplianceSnapshotRecalculationQueue>();

// Projection Handler
builder.Services.AddScoped<IComplianceProjectionHandler, ComplianceSnapshotProjector>();

// Outbox Processor
builder.Services.AddScoped<OutboxProcessor>();

// Command Service
builder.Services.AddScoped<ComplianceCommandService>();

// Background Jobs
builder.Services.AddScoped<ComplianceService.Application.BackgroundJobs.ComplianceSnapshotJob>();
builder.Services.AddScoped<ComplianceService.Application.BackgroundJobs.AnalyticsDailySnapshotJob>();
builder.Services.AddScoped<ComplianceService.Application.BackgroundJobs.ComplianceExpiryJob>();
builder.Services.AddScoped<ComplianceService.Application.BackgroundJobs.EvidenceExpiryJob>();
builder.Services.AddScoped<ComplianceService.Application.BackgroundJobs.ComplianceRenewalWindowJob>();
builder.Services.AddScoped<ComplianceService.Application.BackgroundJobs.ComplianceEscalationJob>();
builder.Services.AddScoped<ComplianceService.Application.BackgroundJobs.RecurringTaskMaterializationJob>();
builder.Services.AddScoped<ComplianceService.Application.BackgroundJobs.TaskOverdueEscalationJob>();
builder.Services.AddScoped<ComplianceService.Application.BackgroundJobs.SlaChaseJob>();

// Integration Event Publisher (Outbox-only, no direct broker usage)
builder.Services.AddScoped<IIntegrationEventPublisher, ComplianceService.Infrastructure.Services.IntegrationEventPublisher>();

// Framework-specific journey policies and factory
builder.Services.AddScoped<IFrameworkJourneyPolicy, DefaultJourneyPolicy>();
builder.Services.AddScoped<IFrameworkJourneyPolicy, NabhJourneyPolicy>();
builder.Services.AddScoped<IFrameworkJourneyPolicy, NablJourneyPolicy>();
builder.Services.AddScoped<IFrameworkJourneyPolicy, FireNocJourneyPolicy>();
builder.Services.AddScoped<IFrameworkJourneyPolicy, PollutionJourneyPolicy>();
builder.Services.AddScoped<IFrameworkJourneyPolicyFactory, FrameworkJourneyPolicyFactory>();

// Compliance Journey Service and task generation
builder.Services.AddScoped<IComplianceTaskGenerationService, ComplianceTaskGenerationService>();
builder.Services.AddScoped<IComplianceTaskEnsureService, ComplianceTaskEnsureService>();
builder.Services.AddScoped<ComplianceJourneyService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IJourneyWorkspaceService, ComplianceService.Application.Services.JourneyWorkspaceAppService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IEvidenceBinderQueryService, ComplianceService.Application.Services.EvidenceBinderQueryService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IEvidenceBinderService, ComplianceService.Application.Services.EvidenceBinderService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IObjectiveJourneyService, ComplianceService.Application.Services.ObjectiveJourneyAppService>();
builder.Services.AddScoped<WizardOnboardingAssessmentService>();

// Compliance Audit & Reporting Services
builder.Services.AddScoped<ComplianceAuditService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.Configure<ComplianceService.Infrastructure.Configs.ReadinessWeightOptions>(
    builder.Configuration.GetSection(ComplianceService.Infrastructure.Configs.ReadinessWeightOptions.SectionName));
builder.Services.AddScoped<ComplianceService.Application.Reporting.Contracts.IComplianceReportService, ComplianceService.Application.Reporting.Services.ComplianceReportService>();
builder.Services.AddScoped<ComplianceService.Application.Reporting.Contracts.IAuditReportService, ComplianceService.Application.Reporting.Services.AuditReportService>();
builder.Services.AddScoped<ComplianceService.Application.Reporting.Contracts.ITaskReportService, ComplianceService.Application.Reporting.Services.TaskReportService>();
builder.Services.AddScoped<ComplianceService.Application.Reporting.Contracts.IEvidenceReportService, ComplianceService.Application.Reporting.Services.EvidenceReportService>();
builder.Services.AddScoped<ComplianceService.Application.Reporting.Contracts.IReadinessReportService, ComplianceService.Application.Reporting.Services.ReadinessReportService>();
builder.Services.AddScoped<ComplianceService.Application.Reporting.Contracts.ICustomReportService, ComplianceService.Application.Reporting.Services.CustomReportService>();
builder.Services.AddScoped<ComplianceService.Application.Reporting.Contracts.IReportExportService, ComplianceService.Application.Reporting.Export.ReportExportService>();
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IDashboardAppService, DashboardAppService>();
// DEF-M5-007 — DashboardSnapshotService implements IDashboardSnapshotService *and*
// IDashboardSnapshotBuilder. Only the first interface was registered, while the projection engine's
// factory below resolves the concrete type, so every dashboard rebuild threw
// "No service for type 'DashboardSnapshotService' has been registered". Registering the concrete
// type once and forwarding both interfaces to it keeps a single instance per scope.
builder.Services.AddScoped<DashboardSnapshotService>();
builder.Services.AddScoped<IDashboardSnapshotService>(sp => sp.GetRequiredService<DashboardSnapshotService>());

// Platform dashboard foundation (H1.1)
builder.Services.AddPlatformDashboard();
builder.Services.Configure<ComplianceService.Application.Dashboard.Summary.DashboardSummaryOptions>(
    builder.Configuration.GetSection(ComplianceService.Application.Dashboard.Summary.DashboardSummaryOptions.SectionName));
builder.Services.Configure<ComplianceService.Application.Dashboard.Projection.DashboardProjectionOptions>(
    builder.Configuration.GetSection(ComplianceService.Application.Dashboard.Projection.DashboardProjectionOptions.SectionName));
builder.Services.AddSingleton<ComplianceService.Application.Dashboard.Summary.IDashboardSummaryCache, ComplianceService.Application.Dashboard.Summary.DashboardSummaryCache>();
builder.Services.AddScoped<ComplianceService.Application.Dashboard.Summary.IDashboardSummaryEngine, ComplianceService.Application.Dashboard.Summary.DashboardSummaryEngine>();
builder.Services.AddScoped<ComplianceService.Application.Dashboard.Projection.IDashboardProjectionRepository, ComplianceService.Infrastructure.Persistence.DashboardProjectionRepository>();
builder.Services.AddScoped<ComplianceService.Application.Dashboard.Projection.DashboardProjectionUpdater>();
builder.Services.AddScoped<ComplianceService.Application.Dashboard.Projection.IDashboardProjectionEngine, ComplianceService.Application.Dashboard.Projection.DashboardProjectionEngine>();
builder.Services.AddScoped<ComplianceService.Application.Dashboard.Projection.DashboardProjectionRebuildService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IDashboardSnapshotBuilder>(sp =>
    sp.GetRequiredService<ComplianceService.Application.Services.DashboardSnapshotService>());
builder.Services.AddScoped<Platform.Dashboard.Contracts.IDashboardProvider, ComplianceService.Application.Dashboard.Praman.PramanDashboardProvider>();
builder.Services.AddScoped<ComplianceService.Application.Dashboard.DashboardContextBuilder>();

// Compliance Maturity Dashboard services
builder.Services.AddScoped<ComplianceService.Application.Services.ComplianceMaturityIndexService>();
builder.Services.AddScoped<ComplianceService.Application.Services.PredictedGapEngineService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IComplianceMaturityAggregationService, ComplianceService.Application.Services.ComplianceMaturityAggregationService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IStageUnlockingService, ComplianceService.Application.Services.StageUnlockingService>();
builder.Services.AddScoped<ComplianceService.Application.Services.MaturityStageUnlockService>();

// Compliance Command Center (Control Center) services
builder.Services.AddScoped<ComplianceService.Application.Contracts.IControlCenterRepository, ComplianceService.Infrastructure.Repositories.ControlCenterRepository>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IControlCenterAggregationService, ComplianceService.Application.Services.ControlCenterAggregationService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IControlCenterService, ComplianceService.Application.Services.ControlCenterApplicationService>();

// Obligation Engine — OrganizationComplianceObligation services
builder.Services.AddScoped<ComplianceService.Application.Contracts.IComplianceJourneyContextResolver, ComplianceService.Application.Services.ComplianceJourneyContextResolver>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IObligationRepository, ComplianceService.Infrastructure.Repositories.ObligationRepository>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IObligationQueryService, ComplianceService.Application.Services.ObligationQueryService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IObligationSynchronizationService, ComplianceService.Application.Services.ObligationSynchronizationService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IObligationProjectionService, ComplianceService.Application.Services.ObligationProjectionService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IDeficiencyRemediationTaskService, ComplianceService.Application.Services.DeficiencyRemediationTaskService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IDeficiencyTaskLifecycleService, ComplianceService.Application.Services.DeficiencyTaskLifecycleService>();
builder.Services.AddScoped<ComplianceService.Application.Services.UpdateObligationFromAssessmentCommandHandler>();
builder.Services.AddScoped<ComplianceService.Application.Services.UpdateObligationJourneyProgressCommandHandler>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IOnboardingObligationAssessmentService, ComplianceService.Application.Services.OnboardingObligationAssessmentService>();
builder.Services.AddScoped<ComplianceService.Application.Services.InProcessOutboxProcessor>();
builder.Services.AddScoped<ComplianceService.Application.Services.IObligationNextActionService, ComplianceService.Application.Services.ObligationNextActionService>();
builder.Services.AddScoped<ComplianceService.Application.Services.RebuildObligationAssessmentCommandHandler>();

// Deficiency Engine
builder.Services.AddScoped<ComplianceService.Application.Contracts.IDeficiencyRuleEngine, ComplianceService.Application.Services.DeficiencyRuleEngine>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IDeficiencySeverityEngine, ComplianceService.Application.Services.DeficiencySeverityEngine>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IDeficiencyEvaluationService, ComplianceService.Application.Services.DeficiencyEvaluationService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IDeficiencyRepository, ComplianceService.Infrastructure.Repositories.DeficiencyRepository>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IDeficiencyQueryService, ComplianceService.Application.Services.DeficiencyQueryService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IDeficiencyCommandCenterQueryService, ComplianceService.Application.Services.DeficiencyCommandCenterQueryService>();
builder.Services.AddScoped<ComplianceService.Application.Services.EvaluateObligationDeficienciesCommandHandler>();
builder.Services.AddScoped<ComplianceService.Application.Services.PatchDeficiencyStatusCommandHandler>();
builder.Services.AddScoped<ComplianceService.Application.Services.PatchDeficiencyRootCauseCommandHandler>();
builder.Services.AddScoped<ComplianceService.Application.Services.PatchDeficiencyRcaCommandHandler>();
builder.Services.AddScoped<ComplianceService.Application.Services.PatchDeficiencyRemediationCommandHandler>();
builder.Services.AddScoped<ComplianceService.Application.Services.PatchDeficiencyCorrectiveActionCommandHandler>();
builder.Services.AddScoped<ComplianceService.Application.Services.PatchDeficiencyPreventiveActionCommandHandler>();
builder.Services.AddScoped<ComplianceService.Application.Services.PatchDeficiencyEffectivenessReviewCommandHandler>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IRecurrenceEvaluationService, ComplianceService.Application.Services.RecurrenceEvaluationService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.ICapaQueryService, ComplianceService.Application.Services.CapaQueryService>();
builder.Services.AddScoped<ComplianceService.Application.Services.IDeficiencyAuditTrailService, ComplianceService.Application.Services.DeficiencyAuditTrailService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IDeficiencyTaskOwnershipSyncService, ComplianceService.Application.Services.DeficiencyTaskOwnershipSyncService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IAssignmentRoutingService, ComplianceService.Application.Services.AssignmentRoutingService>();
builder.Services.AddScoped<ComplianceService.Application.Services.DepartmentOwnershipAppService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.ISlaPolicyService, ComplianceService.Application.Services.SlaPolicyService>();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IBusinessCalendarProvider, ComplianceService.Infrastructure.Services.OrganizationBusinessCalendarProvider>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.ISlaEvaluationService, ComplianceService.Application.Services.SlaEvaluationService>();
builder.Services.AddScoped<ComplianceService.Application.Contracts.IEscalationService, ComplianceService.Application.Services.EscalationService>();
builder.Services.AddScoped<ComplianceService.Application.Analytics.IDeficiencyMetricsService, ComplianceService.Application.Analytics.DeficiencyMetricsService>();
builder.Services.AddScoped<ComplianceService.Application.Analytics.IAnalyticsAggregationService, ComplianceService.Application.Analytics.AnalyticsAggregationService>();
builder.Services.AddScoped<ComplianceService.Application.Analytics.IDeficiencyComplianceEventService, ComplianceService.Application.Analytics.DeficiencyComplianceEventService>();
builder.Services.AddScoped<ComplianceService.Application.Analytics.IAnalyticsQueryService, ComplianceService.Application.Analytics.AnalyticsQueryService>();
builder.Services.AddScoped<ComplianceService.Application.Services.IGovernanceDomainKpiService, ComplianceService.Application.Services.GovernanceDomainKpiService>();
builder.Services.AddScoped<ComplianceService.Application.Analytics.IAnalyticsSnapshotService, ComplianceService.Application.Analytics.AnalyticsSnapshotService>();
builder.Services.AddScoped<ComplianceService.Application.Services.AssignDeficiencyCommandHandler>();
builder.Services.AddScoped<ComplianceService.Application.Services.AcceptDeficiencyCommandHandler>();
builder.Services.AddScoped<ComplianceService.Application.Services.RejectDeficiencyOwnershipCommandHandler>();

// Copilot services - enforce Explainability in all responses
builder.Services.AddScoped<IExplanationComposer, ExplanationComposer>();
builder.Services.AddScoped<ILLMExplanationAdapter, LLMExplanationAdapter>();
builder.Services.AddScoped<ICopilotResponseFactory, CopilotResponseFactory>();

// Scoring and NA Engine
builder.Services.Configure<ComplianceService.Infrastructure.Configs.ScoringOptions>(
    builder.Configuration.GetSection("Scoring"));
builder.Services.Configure<ComplianceService.Infrastructure.Configs.MigrationSafetyOptions>(
    builder.Configuration.GetSection(ComplianceService.Infrastructure.Configs.MigrationSafetyOptions.SectionName));
builder.Services.AddScoped<ComplianceService.Application.Scoring.IScoringService, ComplianceService.Application.Scoring.ScoringService>();
builder.Services.AddScoped<IDeterministicNabhScoringService, DeterministicNabhScoringService>();
builder.Services.AddScoped<IScoringParityService, ScoringParityService>();
builder.Services.AddScoped<IComplianceEventEmitter, ComplianceEventEmitter>();
builder.Services.AddScoped<ComplianceService.Application.Workflow.IWorkflowRuleEngine, ComplianceService.Application.Workflow.WorkflowRuleEngine>();
builder.Services.AddScoped<ComplianceService.Application.Workflow.IWorkflowRule, ComplianceService.Application.Workflow.Rules.EvidenceApprovedWorkflowRule>();
builder.Services.AddScoped<ComplianceService.Application.Workflow.IWorkflowRule, ComplianceService.Application.Workflow.Rules.AuditSessionClosedWorkflowRule>();

// Aggregation Engine
builder.Services.AddScoped<ComplianceService.Application.Aggregation.IAggregationService, ComplianceService.Application.Aggregation.AggregationService>();

// Operational mode → OE classification policy (stateless; singleton)
builder.Services.AddSingleton<ComplianceService.Domain.Policies.IOperationalModeClassificationPolicy, ComplianceService.Domain.Policies.OperationalModeClassificationPolicy>();
builder.Services.AddSingleton<ComplianceService.Domain.Policies.IOperationalModeProgressionPolicy, ComplianceService.Domain.Policies.OperationalModeProgressionPolicy>();

// Action Plans and Decision Engine
builder.Services.AddScoped<ComplianceService.Application.ActionPlans.IActionPlanService, ComplianceService.Application.ActionPlans.ActionPlanService>();
builder.Services.AddScoped<ComplianceService.Application.Decision.IRuleEngine, ComplianceService.Application.Decision.RuleEngine>();
builder.Services.AddScoped<ComplianceService.Application.Decision.DecisionService>();

var app = builder.Build();

// Correlation + structured request logging must run first so every log line is enriched.
app.UsePramanLogging();
app.UsePramanLifecycleLogging("ComplianceService");

// Configure the HTTP request pipeline.
// Enforce HTTPS outside Development. Gateway Development BaseUrls use http://localhost:5016;
// redirecting those calls to https://localhost:7087 strips Authorization (HttpClient) and
// surfaces as 401s for dashboard/copilot after login.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseResponseCompression();
app.UseDashboardRequestTiming();

// Global exception handling - must be early in the pipeline
app.UseMiddleware<ComplianceService.Middleware.ExceptionHandlingMiddleware>();
app.UseMiddleware<ComplianceService.Middleware.RequestCostLoggingMiddleware>();

// CORS must be AFTER UseRouting but BEFORE UseAuthentication/UseAuthorization
app.UseCors("AngularApp");

// Rate limiting
app.UseRateLimiter();

// Apply database migrations and seeding asynchronously with timeout to prevent deadlocks
if (app.Environment.IsDevelopment())
{
    // Swagger UI temporarily disabled - Swashbuckle.AspNetCore is not compatible with .NET 10
    // OpenAPI JSON is still available via Microsoft.AspNetCore.OpenApi at /openapi/v1.json
    // TODO: Re-enable Swagger UI once Swashbuckle.AspNetCore supports .NET 10
    // app.UseSwagger();
    // app.UseSwaggerUI();
    
    // Apply database migrations in Development with timeout to prevent hanging
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Applying database migrations for ComplianceService...");
            
            var dbContext = scope.ServiceProvider.GetRequiredService<ComplianceDbContext>();
            
            // Use ConfigureAwait(false) and add timeout to prevent deadlocks
            using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
            {
                await dbContext.Database.MigrateAsync(cts.Token).ConfigureAwait(false);
            }
            
            logger.LogInformation("Database migrations applied successfully for ComplianceService.");
        }
    }
    catch (OperationCanceledException)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError("Database migration timed out after 5 minutes. Application will continue but database may not be up to date.");
    }
    catch (Exception ex)
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error applying database migrations. Application will continue but database may not be up to date.");
    }
}

// Run master data seeding with timeout to prevent deadlocks
try
{
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Running master data seeding...");
        
        var runner = scope.ServiceProvider.GetRequiredService<MasterDataSeederRunner>();

        // Use ConfigureAwait(false) and add timeout to prevent deadlocks
        using (var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)))
        {
            await runner.RunAsync(cts.Token).ConfigureAwait(false);
            logger.LogInformation("Master data seeding completed successfully.");

            var docDb = scope.ServiceProvider.GetRequiredService<ComplianceDbContext>();
            await DocumentationTemplateSeeder.SeedAsync(docDb, cts.Token).ConfigureAwait(false);
            logger.LogInformation("QDMS template seeding completed.");
        }
    }
}
catch (OperationCanceledException)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError("Master data seeding timed out after 5 minutes. Application will continue.");
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Error during master data seeding. Application will continue.");
}

// Migration safety gate: block unified finding mode when critical integrity issues exist.
try
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var migrationSafety = scope.ServiceProvider
        .GetRequiredService<Microsoft.Extensions.Options.IOptions<ComplianceService.Infrastructure.Configs.MigrationSafetyOptions>>()
        .Value;
    var validation = scope.ServiceProvider.GetRequiredService<IDataMigrationValidationService>();
    var report = await validation.ValidateAsync(CancellationToken.None);
    logger.LogInformation(
        "Migration validation report: tasksWithoutOE={TasksWithoutOE}, evidenceWithoutTask={EvidenceWithoutTask}, findingsWithoutScope={FindingsWithoutScope}, hasCritical={HasCritical}",
        report.TasksWithoutObjectiveElementId,
        report.EvidenceWithoutTaskId,
        report.FindingsWithoutScopeLinkage,
        report.HasCriticalIssues);

    if (migrationSafety.UseUnifiedFinding && report.HasCriticalIssues)
    {
        throw new InvalidOperationException(
            "Migration safety gate failed: critical data inconsistencies detected. Disable UseUnifiedFinding or remediate data.");
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "Migration safety validation failed.");
    throw;
}

app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard — non-Development requires authenticated ComplianceScope (see phase-7/02-performance.md).
// Development allows local-only access for operator convenience; never leave Authorization empty in any env.
IDashboardAuthorizationFilter[] hangfireAuth = app.Environment.IsDevelopment()
    ? new IDashboardAuthorizationFilter[] { new LocalRequestsOnlyAuthorizationFilter() }
    : new IDashboardAuthorizationFilter[] { new HangfireAuthorizationFilter() };
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = hangfireAuth,
    DashboardTitle = "Compliance Service - Background Jobs",
    StatsPollingInterval = 2000
});

// Schedule recurring background jobs
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        // Schedule nightly compliance snapshot recalculation
        // Runs daily at 2 AM UTC
        // AddOrUpdate is idempotent - will add if not exists, update if exists
        RecurringJob.AddOrUpdate<ComplianceService.Application.BackgroundJobs.ComplianceSnapshotJob>(
            "nightly-compliance-snapshot-recalculation",
            job => job.RecalculateAllHospitals(),
            Cron.Daily(2), // 2 AM UTC
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        logger.LogInformation(
            "Scheduled recurring job: 'Nightly Compliance Snapshot Recalculation' (ID: nightly-compliance-snapshot-recalculation) - Daily at 2:00 AM UTC");

        // Schedule compliance expiry job - runs daily at 1 AM UTC
        RecurringJob.AddOrUpdate<ComplianceService.Application.BackgroundJobs.ComplianceExpiryJob>(
            "compliance-expiry-job",
            job => job.ProcessExpiredJourneysAsync(CancellationToken.None),
            Cron.Daily(1), // 1 AM UTC
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        logger.LogInformation(
            "Scheduled recurring job: 'Compliance Expiry Job' (ID: compliance-expiry-job) - Daily at 1:00 AM UTC");

        // Schedule evidence expiry job - runs daily at 1:30 AM UTC (near ComplianceExpiryJob)
        RecurringJob.AddOrUpdate<ComplianceService.Application.BackgroundJobs.EvidenceExpiryJob>(
            "evidence-expiry-job",
            job => job.ProcessExpiredEvidenceAsync(CancellationToken.None),
            "30 1 * * *", // 1:30 AM UTC
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        logger.LogInformation(
            "Scheduled recurring job: 'Evidence Expiry Job' (ID: evidence-expiry-job) - Daily at 1:30 AM UTC");

        // Schedule compliance renewal window job - runs daily at 2 AM UTC
        RecurringJob.AddOrUpdate<ComplianceService.Application.BackgroundJobs.ComplianceRenewalWindowJob>(
            "compliance-renewal-window-job",
            job => job.ProcessRenewalWindowJourneysAsync(CancellationToken.None),
            Cron.Daily(2), // 2 AM UTC
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        logger.LogInformation(
            "Scheduled recurring job: 'Compliance Renewal Window Job' (ID: compliance-renewal-window-job) - Daily at 2:00 AM UTC");

        // Schedule compliance escalation job - runs daily at 3 AM UTC
        RecurringJob.AddOrUpdate<ComplianceService.Application.BackgroundJobs.ComplianceEscalationJob>(
            "compliance-escalation-job",
            job => job.ProcessEscalationsAsync(CancellationToken.None),
            Cron.Daily(3), // 3 AM UTC
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        logger.LogInformation(
            "Scheduled recurring job: 'Compliance Escalation Job' (ID: compliance-escalation-job) - Daily at 3:00 AM UTC");

        RecurringJob.AddOrUpdate<ComplianceService.Application.BackgroundJobs.AnalyticsDailySnapshotJob>(
            "analytics-daily-snapshot",
            job => job.GenerateAllDailySnapshotsAsync(CancellationToken.None),
            Cron.Daily(4),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        RecurringJob.AddOrUpdate<ComplianceService.Application.BackgroundJobs.RecurringTaskMaterializationJob>(
            "recurring-task-materialization",
            job => job.MaterializeDueOccurrencesAsync(CancellationToken.None),
            Cron.Hourly(),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        logger.LogInformation(
            "Scheduled recurring job: 'Recurring Task Materialization' (ID: recurring-task-materialization) - Hourly UTC");

        RecurringJob.AddOrUpdate<ComplianceService.Application.BackgroundJobs.TaskOverdueEscalationJob>(
            "task-overdue-escalation",
            job => job.ProcessOverdueTasksAsync(CancellationToken.None),
            Cron.Daily(5),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        logger.LogInformation(
            "Scheduled recurring job: 'Task Overdue Escalation' (ID: task-overdue-escalation) - Daily at 5:00 AM UTC");

        RecurringJob.AddOrUpdate<ComplianceService.Application.BackgroundJobs.SlaChaseJob>(
            "sla-chase",
            job => job.ChaseOpenSlaAsync(CancellationToken.None),
            Cron.Daily(6),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        logger.LogInformation(
            "Scheduled recurring job: 'SLA Chase' (ID: sla-chase) - Daily at 6:00 AM UTC");

        // Phase 4 Sprint 1 — QDMS document control jobs
        RecurringJob.AddOrUpdate<ComplianceService.Application.BackgroundJobs.DocumentReviewDueJob>(
            "document-review-due",
            job => job.ProcessReviewDueAsync(CancellationToken.None),
            Cron.Daily(7),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        logger.LogInformation(
            "Scheduled recurring job: 'Document Review Due' (ID: document-review-due) - Daily at 7:00 AM UTC");

        RecurringJob.AddOrUpdate<ComplianceService.Application.BackgroundJobs.DocumentExpiryJob>(
            "document-expiry",
            job => job.ProcessExpiredDocumentsAsync(CancellationToken.None),
            "30 7 * * *", // 7:30 AM UTC
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        logger.LogInformation(
            "Scheduled recurring job: 'Document Expiry' (ID: document-expiry) - Daily at 7:30 AM UTC");

        RecurringJob.AddOrUpdate<ComplianceService.Application.BackgroundJobs.DocumentAckReminderJob>(
            "document-ack-reminder",
            job => job.ProcessAckRemindersAsync(CancellationToken.None),
            Cron.Daily(8),
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        logger.LogInformation(
            "Scheduled recurring job: 'Document Ack Reminder' (ID: document-ack-reminder) - Daily at 8:00 AM UTC");

        // Phase 4 Sprint 4 — Enterprise risk register review-due
        RecurringJob.AddOrUpdate<ComplianceService.Application.BackgroundJobs.RiskReviewDueJob>(
            "risk-review-due",
            job => job.ProcessReviewDueAsync(CancellationToken.None),
            "0 8 * * *", // 8:00 AM UTC (after document ack reminders)
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc
            });

        logger.LogInformation(
            "Scheduled recurring job: 'Risk Review Due' (ID: risk-review-due) - Daily at 8:00 AM UTC");

        logger.LogInformation(
            "Scheduled recurring job: 'Analytics Daily Snapshot' (ID: analytics-daily-snapshot) - Daily at 4:00 AM UTC");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error scheduling recurring Hangfire jobs. Application will continue but background jobs may not be scheduled.");
    }
}

app.MapControllers();
app.UsePramanObservability();
app.MapPramanAoviasPlatform();

// ============================================================================
// TEMPORARY TEST CODE - DELETE AFTER VERIFICATION
// ============================================================================
// Uncomment the block below to run the Regulation Enrollment test
// ============================================================================
/*
using (var scope = app.Services.CreateScope())
{
    await ComplianceService.TempRegulationEnrollmentTest.RunTestAsync(scope.ServiceProvider);
}
*/
app.Run();
