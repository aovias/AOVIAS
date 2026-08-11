using KnowledgeBase.Application.AssessmentSummary;
using KnowledgeBase.Application.Contracts;
using KnowledgeBase.Application.GapAnalysis;
using KnowledgeBase.Application.Questionnaire;
using KnowledgeBase.Application.Services;
using KnowledgeBase.Infrastructure.Persistence;
using KnowledgeBase.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBase.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddKnowledgebasePersistenceForSqlServer(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<KnowledgeBaseContext>(o =>
            o.UseSqlServer(connectionString)
             .AddInterceptors(Shared.Logging.Diagnostics.LoggingDbCommandInterceptor.Instance));
        RegisterKnowledgebaseStack(services);
        return services;
    }

    /// <summary>In-memory database for integration tests (environment <c>Testing</c>).</summary>
    public static IServiceCollection AddKnowledgebasePersistenceForTests(this IServiceCollection services)
    {
        services.AddDbContext<KnowledgeBaseContext>(o =>
            o.UseInMemoryDatabase("kb-test-" + Guid.NewGuid().ToString("N")));
        RegisterKnowledgebaseStack(services);
        return services;
    }

    private static void RegisterKnowledgebaseStack(IServiceCollection services)
    {
        services.AddScoped<IFrameworkRepository, FrameworkRepository>();
        services.AddScoped<INodeRepository, NodeRepository>();
        services.AddScoped<IKnowledgeRepository, KnowledgeRepository>();
        services.AddScoped<IRequirementRepository, RequirementRepository>();
        services.AddScoped<IScoringRepository, ScoringRepository>();
        services.AddScoped<IAssessmentRepository, AssessmentRepository>();
        services.AddScoped<IGapAnalysisRepository, GapAnalysisRepository>();
        services.AddScoped<IWizardQuestionRepository, WizardQuestionRepository>();
        services.AddScoped<IAIRepository, AIRepository>();
        services.AddScoped<IDecisionRepository, DecisionRepository>();

        services.AddSingleton<IAssessmentSummaryCalculator, AssessmentSummaryCalculator>();
        services.AddSingleton<IQuestionnaireGenerator, QuestionnaireGenerator>();
        services.AddSingleton<IGapClassifier, GapClassifier>();
        services.AddSingleton<IGapRecommendationBuilder, GapRecommendationBuilder>();
        services.AddScoped<ICompliancePromptBuilder, CompliancePromptBuilder>();
        services.AddScoped<AssessmentContextBuilder>();
        services.AddScoped<GapAnalysisOrchestrator>();
        services.AddScoped<KnowledgebaseQueryAppService>();
        services.AddScoped<QuestionnaireAppService>();
        services.AddScoped<AiAskAppService>();
        services.AddScoped<DecisionAppService>();
        services.AddScoped<AssessmentAppService>();
        services.AddScoped<GapAnalysisAppService>();
        services.AddScoped<CoreAssessmentService>();
        services.AddSingleton<MaturityProgressionService>();
    }

    public static IServiceCollection AddKnowledgebaseAIServices(this IServiceCollection services)
    {
        services.AddScoped<IAIService, MockAIService>();
        return services;
    }
}
