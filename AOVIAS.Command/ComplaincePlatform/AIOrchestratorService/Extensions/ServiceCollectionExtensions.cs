using AIOrchestratorService.Application.IntentDetection;
using AIOrchestratorService.Application.Services;
using AIOrchestratorService.Application.WorkflowEngine;
using AIOrchestratorService.Application.AI.Ace;
using AIOrchestratorService.Application.AI.Actions;
using AIOrchestratorService.Application.AI.Autonomy;
using AIOrchestratorService.Application.AI.Configuration;
using AIOrchestratorService.Application.AI.Governance;
using AIOrchestratorService.Application.AI.Inference;
using AIOrchestratorService.Application.AI.Orchestration;
using AIOrchestratorService.Application.AI.Predictive;
using AIOrchestratorService.Application.AI.Prompting;
using AIOrchestratorService.Application.AI.Proactive;
using AIOrchestratorService.Application.AI.Reliability;
using AIOrchestratorService.Application.AI.Reporting;
using AIOrchestratorService.Application.AI.Risk;
using AIOrchestratorService.Application.AI.Knowledge;
using AIOrchestratorService.Application.AI.Retrieval;
using AIOrchestratorService.Application.AI.Validation;
using AIOrchestratorService.Application.AI.Workflow;
using AIOrchestratorService.Infrastructure.Clients;
using AIOrchestratorService.Infrastructure.LLM;
using AIOrchestratorService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace AIOrchestratorService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthenticationSystem(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection("JwtSettings");
        var secret = jwt["SecretKey"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLongForHS256Algorithm";
        var issuer = jwt["Issuer"] ?? "https://localhost:7135";
        var audience = jwt["Audience"] ?? "https://localhost:7135";

        services.AddAuthentication("Bearer")
           .AddJwtBearer("Bearer", options =>
           {
               options.RequireHttpsMetadata = false;

               options.TokenValidationParameters = new TokenValidationParameters
               {
                   ValidateIssuer = true,
                   ValidateAudience = true,
                   ValidateIssuerSigningKey = true,
                   ValidIssuer = issuer,
                   ValidAudience = audience,
                   IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
               };
           });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AiScope", policy =>
            {
                policy.RequireAssertion(context =>
                {
                    var scopeValues = context.User.FindAll("scope")
                        .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries));

                    return scopeValues.Contains("ai.api", StringComparer.OrdinalIgnoreCase) ||
                           scopeValues.Contains("compliance.api", StringComparer.OrdinalIgnoreCase);
                });
            });
        });

        return services;
    }

    public static IServiceCollection AddApiVersioningSystem(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
        });

        services.AddVersionedApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    public static IServiceCollection AddWorkflowEngine(this IServiceCollection services)
    {
        // Register WorkflowRegistry as singleton (it's a registry that holds all workflows)
        services.AddSingleton<IWorkflowRegistry, WorkflowRegistry>();

        // Register WorkflowExecutor as scoped (executes workflows per request)
        services.AddScoped<IWorkflowExecutor, WorkflowExecutor>();

        // Register workflows
        services.AddScoped<StartComplianceWorkflow>();

        return services;
    }

    public static IServiceCollection AddIntentDetection(this IServiceCollection services)
    {
        services.AddScoped<IIntentClassifier, BasicIntentClassifier>();
        return services;
    }

    public static IServiceCollection AddInternalClients(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IComplianceClient, ComplianceClient>();
        services.AddScoped<AuditClient>();

        var complianceBaseUrl = configuration["Services:ComplianceService:BaseUrl"] ?? "https://localhost:7087";
        services.AddHttpClient("ComplianceServiceClient", client =>
        {
            client.BaseAddress = new Uri(complianceBaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddHttpMessageHandler<ForwardAuthHeaderHandler>()
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .ConfigurePrimaryHttpMessageHandler(CreateInternalHttpHandler);

        var knowledgeBaseUrl = configuration["Services:KnowledgeBaseService:BaseUrl"] ?? "http://localhost:5120";
        services.AddHttpClient("KnowledgeBaseServiceClient", client =>
        {
            client.BaseAddress = new Uri(knowledgeBaseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .AddHttpMessageHandler<ForwardAuthHeaderHandler>()
        .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
        .ConfigurePrimaryHttpMessageHandler(CreateInternalHttpHandler);

        var communicationBaseUrl = configuration["Services:CommunicationService:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(communicationBaseUrl))
        {
            services.AddHttpClient("CommunicationServiceClient", client =>
            {
                client.BaseAddress = new Uri(communicationBaseUrl);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddHttpMessageHandler<ForwardAuthHeaderHandler>()
            .AddHttpMessageHandler<Shared.Logging.Correlation.CorrelationForwardingHandler>()
            .ConfigurePrimaryHttpMessageHandler(CreateInternalHttpHandler);
        }

        return services;
    }

    /// <summary>
    /// Primary handler for internal service-to-service HTTP clients. In Development it accepts the
    /// self-signed ASP.NET dev certificate so HTTPS calls to sibling services (e.g. ComplianceService
    /// on https://localhost:7087) succeed without manual cert trust, mirroring the Gateway's behaviour.
    /// In non-Development environments standard certificate validation is enforced.
    /// </summary>
    private static HttpClientHandler CreateInternalHttpHandler()
    {
        var handler = new HttpClientHandler();
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }

        return handler;
    }

    /// <summary>
    /// Registers the LLM adapter. Default implementation can be replaced with specific provider implementations.
    /// </summary>
    public static IServiceCollection AddLLMAdapter(this IServiceCollection services)
    {
        services.AddScoped<ILLMAdapter, AzureOpenAIDraftAdapter>();
        services.AddScoped<AzureOpenAIModelAdapter>();
        services.AddScoped<SarvamModelAdapter>();
        services.AddScoped<AnthropicModelAdapter>();
        services.AddScoped<OllamaModelAdapter>();
        services.AddScoped<IAIModelAdapter, ProviderRoutingModelAdapter>();

        return services;
    }

    public static IServiceCollection AddAISystem(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<AIFeatureFlagsOptions>(configuration.GetSection(AIFeatureFlagsOptions.SectionName));
        services.Configure<AzureOpenAIOptions>(configuration.GetSection(AzureOpenAIOptions.SectionName));
        services.Configure<SarvamOptions>(configuration.GetSection(SarvamOptions.SectionName));
        services.Configure<AIRetrievalPolicyOptions>(configuration.GetSection(AIRetrievalPolicyOptions.SectionName));
        services.Configure<AIConfidencePolicyOptions>(configuration.GetSection(AIConfidencePolicyOptions.SectionName));
        services.Configure<AIPerformanceOptions>(configuration.GetSection(AIPerformanceOptions.SectionName));
        services.Configure<AIUsagePolicyOptions>(configuration.GetSection(AIUsagePolicyOptions.SectionName));
        services.Configure<AIEventProcessingOptions>(configuration.GetSection(AIEventProcessingOptions.SectionName));
        services.Configure<AIProviderRoutingOptions>(configuration.GetSection(AIProviderRoutingOptions.SectionName));
        services.Configure<AutonomyControlOptions>(configuration.GetSection(AutonomyControlOptions.SectionName));

        services.PostConfigure<AIProviderRoutingOptions>(options =>
        {
            var topLevelProvider = configuration["AIProvider"];
            if (!string.IsNullOrWhiteSpace(topLevelProvider))
            {
                options.AIProvider = topLevelProvider;
            }
        });

        services.AddHttpClient("AzureOpenAI");
        services.AddHttpClient("Sarvam");
        services.AddHttpContextAccessor();

        services.AddSingleton<IAIFeaturePolicy, AIFeaturePolicy>();
        services.AddSingleton<IPromptTemplateRegistry, PromptTemplateRegistry>();
        services.AddSingleton<IJSONRepairLayer, JSONRepairLayer>();
        services.AddSingleton<IFallbackStrategy, FallbackStrategy>();
        services.AddSingleton<IMetricsTracker, MetricsTracker>();
        services.AddScoped<IComplianceContextClient, ComplianceContextClient>();
        services.AddScoped<IKnowledgeBaseClient, KnowledgeBaseClient>();
        services.AddSingleton<IEmbeddingService, HashingEmbeddingService>();
        services.AddScoped<IVectorIndex, SqlVectorIndex>();
        services.AddScoped<IKnowledgeSearchService, KnowledgeSearchService>();
        services.AddScoped<IRetrievalService, RetrievalService>();
        services.AddHostedService<Infrastructure.Background.KnowledgeIndexerHostedService>();
        services.AddSingleton<AIOrchestratorService.Application.AI.Classification.IAssistantQueryClassifier, AIOrchestratorService.Application.AI.Classification.AssistantQueryClassifier>();
        services.AddSingleton<AIOrchestratorService.Application.AI.Validation.IResponseSanitizer, AIOrchestratorService.Application.AI.Validation.ResponseSanitizer>();
        services.AddSingleton<AIOrchestratorService.Application.AI.Presentation.IResponsePresentationService, AIOrchestratorService.Application.AI.Presentation.ResponsePresentationService>();
        services.AddSingleton<IHallucinationGuard, HallucinationGuard>();
        services.AddScoped<IAIResponseValidator, AIResponseValidator>();
        services.AddScoped<IAIMediator, AIMediator>();
        services.AddScoped<IConfidenceCalibrationService, ConfidenceCalibrationService>();
        services.AddSingleton<IAIToolRegistry, AIToolRegistry>();
        services.AddSingleton<IAIToolManifest>(sp => sp.GetRequiredService<IAIToolRegistry>());
        services.AddScoped<IToolPreExecutionValidator, ToolPreExecutionValidator>();
        services.AddScoped<IAIToolExecutor, AIToolExecutor>();
        services.AddScoped<IAIOrchestrationService, AIOrchestrationService>();
        services.AddScoped<Application.AI.Memory.IConversationMemoryService, Application.AI.Memory.ConversationMemoryService>();
        services.AddScoped<Application.AI.Evidence.IEvidenceReviewerService, Application.AI.Evidence.EvidenceReviewerService>();
        services.AddScoped<Application.AI.Advisor.IComplianceAdvisorService, Application.AI.Advisor.ComplianceAdvisorService>();
        services.AddScoped<IAIAuditService, AIAuditService>();
        services.AddScoped<IAcceptanceGateService, AcceptanceGateService>();
        services.AddScoped<IStepDependencyGraph, StepDependencyGraph>();
        services.AddScoped<IPlanValidator, PlanValidator>();
        services.AddScoped<IAutonomyPolicyEngine, AutonomyPolicyEngine>();
        services.AddScoped<IExecutionSupervisor, ExecutionSupervisor>();
        services.AddScoped<IFailureClassifier, FailureClassifier>();
        services.AddScoped<ICompensationOrchestrator, CompensationOrchestrator>();
        services.AddSingleton<IEventRoutingPolicy, EventRoutingPolicy>();
        services.AddScoped<IAgentExecutionStateManager, AgentExecutionStateManager>();
        services.AddScoped<IAgentLifecycleEventPublisher, AgentLifecycleEventPublisher>();
        services.AddScoped<IAgentOrchestrator, AgentOrchestrator>();
        services.AddSingleton<IWorkflowTemplateRegistry, WorkflowTemplateRegistry>();

        // Phase 5 Sprint 4–5 — risk, predictive, reporting, workflow hooks, ACE
        services.AddScoped<IRiskIntelligenceService, RiskIntelligenceService>();
        services.AddScoped<IPredictiveAnalyticsService, PredictiveAnalyticsService>();
        services.AddScoped<IAiReportingEngine, AiReportingEngine>();
        services.AddScoped<IAiWorkflowHookService, AiWorkflowHookService>();
        services.AddScoped<IAceInsightStore, AceInsightStore>();
        services.AddScoped<IAceEngine, AceEngine>();
        services.AddSingleton<IAceNotificationBridge, AceNotificationBridge>();
        services.AddHostedService<Infrastructure.Background.AceMonitorHostedService>();

        services.AddSingleton<IProactiveSuggestionStore, ProactiveSuggestionStore>();
        services.AddHostedService<ProactiveSuggestionConsumer>();

        services.AddScoped<Application.AI.Identity.Contracts.IAIProfileService, Application.AI.Identity.Services.AIProfileService>();
        services.AddScoped<Application.AI.Identity.Contracts.IAIAutomationPolicyService, Application.AI.Identity.Services.AIAutomationPolicyService>();
        services.AddScoped<Application.AI.Identity.Contracts.IAIPromptProfileService, Application.AI.Identity.Services.AIPromptProfileService>();
        services.AddScoped<Application.AI.Identity.Contracts.IAIExecutionAuditService, Application.AI.Identity.Services.AIExecutionAuditService>();
        services.AddScoped<Application.AI.Identity.Contracts.IAIAnalyticsService, Application.AI.Identity.Services.AIAnalyticsService>();
        services.AddScoped<Application.AI.Identity.Contracts.IAIApprovalService, Application.AI.Identity.Services.AIApprovalService>();
        services.AddScoped<Application.AI.Identity.Contracts.IAIProfilePermissionService, Application.AI.Identity.Services.AIProfilePermissionService>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Application.AI.Identity.Services.AIProfileService).Assembly));
        services.AddHostedService<Infrastructure.Background.AIIdentityBootstrapHostedService>();
        services.AddHostedService<Infrastructure.Background.AIOutboxProcessorHostedService>();
        return services;
    }

    /// <summary>
    /// Registers the database context and related services.
    /// </summary>
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AIOrchestratorDbContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null));
            options.AddInterceptors(Shared.Logging.Diagnostics.LoggingDbCommandInterceptor.Instance);

            // Suppress pending model changes warning in Development
            var environment = configuration["ASPNETCORE_ENVIRONMENT"];
            if (environment == "Development")
            {
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            }
        });

        return services;
    }

    /// <summary>
    /// Registers draft generation services.
    /// </summary>
    public static IServiceCollection AddDraftGeneration(this IServiceCollection services)
    {
        // Register repository
        services.AddScoped<IDraftArtifactRepository, DraftArtifactRepository>();

        // Register language auditor
        services.AddScoped<IDraftLanguageAuditor, DraftLanguageAuditor>();

        // Register draft generation service
        services.AddScoped<IDraftGenerationService, DraftGenerationService>();

        // Register DraftingRequestBuilder as scoped (can be used per request)
        services.AddScoped<DraftingRequestBuilder>();

        return services;
    }
}
