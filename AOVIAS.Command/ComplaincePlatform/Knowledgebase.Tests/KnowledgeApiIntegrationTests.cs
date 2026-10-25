using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeBase.Application.DTOs;
using FrameworkListDto = KnowledgeBase.Application.DTOs.FrameworkDto;
using KnowledgeBase.Domain.Entities;
using KnowledgeBase.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure.DTO;
using Xunit;

namespace Knowledgebase.Tests;

public sealed class KnowledgeApiIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task Get_frameworks_returns_seeded_framework()
    {
        using var factory = new KnowledgeWebApplicationFactory();
        var client = factory.CreateClient();
        await SeedMinimalAsync(factory);

        var res = await client.GetAsync("/api/v1.0/frameworks");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiResponse<List<FrameworkListDto>>>(body, JsonOptions);
        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Data);
        Assert.NotEmpty(envelope.Data!);
        Assert.Equal("NABH_HOSP_6TH", envelope.Data![0].Code);
    }

    [Fact]
    public async Task Ai_ask_persists_interaction()
    {
        using var factory = new KnowledgeWebApplicationFactory();
        var client = factory.CreateClient();
        var fwId = await SeedMinimalAsync(factory);

        var body = new AiAskRequestDto(Guid.NewGuid(), fwId, null, "What is compliance?");
        var res = await client.PostAsJsonAsync("/api/v1.0/ai/ask", body);
        res.EnsureSuccessStatusCode();
        var text = await res.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiResponse<AiAskResponseDto>>(text, JsonOptions);
        Assert.NotNull(envelope?.Data?.InteractionId);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeBaseContext>();
        var stored = await db.AIInteractions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == envelope!.Data!.InteractionId);
        Assert.NotNull(stored);
        Assert.Contains("What is compliance?", stored!.Prompt);
    }

    [Fact]
    public async Task Decision_post_with_valid_interaction_succeeds()
    {
        using var factory = new KnowledgeWebApplicationFactory();
        var client = factory.CreateClient();
        var (fwId, nodeId) = await SeedWithNodeAsync(factory);

        var ask = new AiAskRequestDto(Guid.NewGuid(), fwId, nodeId, "Q");
        var askRes = await client.PostAsJsonAsync("/api/v1.0/ai/ask", ask);
        askRes.EnsureSuccessStatusCode();
        var askText = await askRes.Content.ReadAsStringAsync();
        var askEnv = JsonSerializer.Deserialize<ApiResponse<AiAskResponseDto>>(askText, JsonOptions);
        var interactionId = askEnv!.Data!.InteractionId;

        var dec = new CreateDecisionRequestDto(
            Guid.NewGuid(),
            fwId,
            nodeId,
            interactionId,
            "Problem",
            "Suggest",
            "Final",
            "Pending",
            0.9m);
        var decRes = await client.PostAsJsonAsync("/api/v1.0/decisions", dec);
        decRes.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Get_knowledge_by_node_code_matches_get_by_node_id()
    {
        using var factory = new KnowledgeWebApplicationFactory();
        var client = factory.CreateClient();
        var (_, nodeId) = await SeedWithNodeAsync(factory);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KnowledgeBaseContext>();
            db.KnowledgeItems.Add(new KnowledgeItem
            {
                Id = Guid.NewGuid(),
                NodeId = nodeId,
                Explanation = "Explain",
                WhyItMatters = "Why",
                HowToComply = "How",
                EvidenceRequired = "E",
                AuditChecklist = "A",
                RiskLevel = "Low",
                ImpactLevel = "Low",
                Tags = "t",
                Version = 1,
                IsActive = true
            });
            await db.SaveChangesAsync();
        }

        var byId = await client.GetAsync($"/api/v1.0/knowledge/node/{nodeId}");
        byId.EnsureSuccessStatusCode();
        var byCode = await client.GetAsync("/api/v1.0/knowledge/node/code/CH1");
        byCode.EnsureSuccessStatusCode();

        var idEnv = JsonSerializer.Deserialize<ApiResponse<List<KnowledgeItemDto>>>(
            await byId.Content.ReadAsStringAsync(), JsonOptions);
        var codeEnv = JsonSerializer.Deserialize<ApiResponse<List<KnowledgeItemDto>>>(
            await byCode.Content.ReadAsStringAsync(), JsonOptions);
        Assert.NotNull(idEnv?.Data);
        Assert.NotNull(codeEnv?.Data);
        Assert.Equal(idEnv!.Data!.Count, codeEnv!.Data!.Count);
        Assert.Equal(idEnv.Data[0].Explanation, codeEnv.Data[0].Explanation);
        Assert.Equal(idEnv.Data[0].NodeId, codeEnv.Data[0].NodeId);
    }

    [Fact]
    public async Task Get_node_details_by_code_matches_get_by_node_id()
    {
        using var factory = new KnowledgeWebApplicationFactory();
        var client = factory.CreateClient();
        var (_, nodeId) = await SeedWithNodeAsync(factory);

        var byId = await client.GetAsync($"/api/v1.0/nodes/{nodeId}/details");
        byId.EnsureSuccessStatusCode();
        var byCode = await client.GetAsync("/api/v1.0/nodes/code/CH1/details");
        byCode.EnsureSuccessStatusCode();

        var idEnv = JsonSerializer.Deserialize<ApiResponse<NodeDetailsDto>>(
            await byId.Content.ReadAsStringAsync(), JsonOptions);
        var codeEnv = JsonSerializer.Deserialize<ApiResponse<NodeDetailsDto>>(
            await byCode.Content.ReadAsStringAsync(), JsonOptions);
        Assert.NotNull(idEnv?.Data?.Node);
        Assert.NotNull(codeEnv?.Data?.Node);
        Assert.Equal(idEnv!.Data!.Node.Id, codeEnv!.Data!.Node.Id);
        Assert.Equal(idEnv.Data.Node.Code, codeEnv.Data.Node.Code);
    }

    [Fact]
    public async Task Get_framework_questionnaire_returns_generated_sections()
    {
        using var factory = new KnowledgeWebApplicationFactory();
        var client = factory.CreateClient();
        var (fwId, _) = await SeedQuestionnaireHierarchyAsync(factory);

        var res = await client.GetAsync($"/api/v1.0/frameworks/{fwId}/questionnaire");
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiResponse<FrameworkQuestionnaireDto>>(body, JsonOptions);

        Assert.NotNull(envelope);
        Assert.True(envelope!.Success);
        Assert.NotNull(envelope.Data);
        Assert.Equal(fwId, envelope.Data!.FrameworkId);
        Assert.Single(envelope.Data.Sections);
        Assert.Single(envelope.Data.Sections[0].Questions);

        var question = envelope.Data.Sections[0].Questions[0];
        Assert.Equal("OE title — Ensure compliance", question.QuestionText);
        Assert.Contains("Maintain records", question.HelpText);
        Assert.Contains("Document policy", question.HelpText);
        Assert.Equal("CORE", question.Category);
        Assert.Equal(3.0m, question.Weight);
    }

    [Fact]
    public async Task Get_framework_questionnaire_unknown_framework_returns_not_found()
    {
        using var factory = new KnowledgeWebApplicationFactory();
        var client = factory.CreateClient();
        await SeedMinimalAsync(factory);

        var res = await client.GetAsync($"/api/v1.0/frameworks/{Guid.NewGuid()}/questionnaire");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiResponse<FrameworkQuestionnaireDto>>(body, JsonOptions);
        Assert.NotNull(envelope);
        Assert.False(envelope!.Success);
        Assert.Equal("RESOURCE_NOT_FOUND", envelope.Code);
    }

    [Fact]
    public async Task Post_assessments_from_wizard_persists_items_and_returns_summary()
    {
        using var factory = new KnowledgeWebApplicationFactory();
        var client = factory.CreateClient();
        var (fwId, oeId) = await SeedQuestionnaireHierarchyAsync(factory);
        var facilityId = Guid.NewGuid();

        var request = new SubmitWizardAssessmentRequestDto(
            facilityId,
            fwId,
            [new WizardAssessmentAnswerDto(oeId.ToString(), "OE title", "yes")]);

        var res = await client.PostAsJsonAsync("/api/v1.0/assessments/from-wizard", request);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadAsStringAsync();
        var envelope = JsonSerializer.Deserialize<ApiResponse<SubmitWizardAssessmentResponseDto>>(body, JsonOptions);

        Assert.NotNull(envelope?.Data);
        Assert.NotEqual(Guid.Empty, envelope!.Data!.AssessmentId);
        Assert.True(envelope.Data.Summary.OverallPercent >= 0);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeBaseContext>();
        var item = await db.AssessmentItems.AsNoTracking()
            .FirstOrDefaultAsync(x =>
                x.AssessmentId == envelope.Data.AssessmentId && x.NodeId == oeId);
        Assert.NotNull(item);
        Assert.Equal(5, item!.Score);
        Assert.Equal("Met", item.ComplianceStatus);
    }

    [Fact]
    public async Task Post_assessments_gap_analysis_generates_report()
    {
        using var factory = new KnowledgeWebApplicationFactory();
        var client = factory.CreateClient();
        var (fwId, oeId) = await SeedQuestionnaireHierarchyAsync(factory);
        var facilityId = Guid.NewGuid();

        var submit = new SubmitWizardAssessmentRequestDto(
            facilityId,
            fwId,
            [new WizardAssessmentAnswerDto(oeId.ToString(), "OE title", "no")]);

        var submitRes = await client.PostAsJsonAsync("/api/v1.0/assessments/from-wizard", submit);
        submitRes.EnsureSuccessStatusCode();
        var submitBody = await submitRes.Content.ReadAsStringAsync();
        var submitEnvelope = JsonSerializer.Deserialize<ApiResponse<SubmitWizardAssessmentResponseDto>>(submitBody, JsonOptions);
        var assessmentId = submitEnvelope!.Data!.AssessmentId;

        var gapRes = await client.PostAsync($"/api/v1.0/assessments/{assessmentId}/gap-analysis", null);
        gapRes.EnsureSuccessStatusCode();
        var gapBody = await gapRes.Content.ReadAsStringAsync();
        var gapEnvelope = JsonSerializer.Deserialize<ApiResponse<GapAnalysisReportDto>>(gapBody, JsonOptions);

        Assert.NotNull(gapEnvelope?.Data);
        Assert.Equal(1, gapEnvelope!.Data!.CriticalCount);
        Assert.Equal(0, gapEnvelope.Data.CompliantCount);
        Assert.Single(gapEnvelope.Data.Items);
        Assert.Equal(GapCategory.Critical, gapEnvelope.Data.Items[0].Category);
        Assert.Equal("Maintain records", gapEnvelope.Data.Items[0].Recommendation);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeBaseContext>();
        var report = await db.GapAnalysisReports.AsNoTracking()
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.AssessmentId == assessmentId);
        Assert.NotNull(report);
        Assert.Single(report!.Items);
    }

    private static async Task<(Guid FrameworkId, Guid OeId)> SeedQuestionnaireHierarchyAsync(KnowledgeWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeBaseContext>();
        await db.Database.EnsureCreatedAsync();

        var fwId = Guid.NewGuid();
        var chapterId = Guid.NewGuid();
        var standardId = Guid.NewGuid();
        var oeId = Guid.NewGuid();
        var classificationId = Guid.NewGuid();

        db.Frameworks.Add(new Framework
        {
            Id = fwId,
            Code = "NABH_HOSP_6TH",
            Name = "NABH",
            Version = "6",
            Description = "Test",
            IsActive = true
        });
        db.ComplianceNodes.Add(new ComplianceNode
        {
            Id = chapterId,
            FrameworkId = fwId,
            ParentId = null,
            Level = 1,
            Code = "AAC",
            Title = "Access Assessment",
            Description = "",
            NodeType = "Chapter",
            Sequence = 1
        });
        db.ComplianceNodes.Add(new ComplianceNode
        {
            Id = standardId,
            FrameworkId = fwId,
            ParentId = chapterId,
            Level = 2,
            Code = "AAC.1",
            Title = "Standard",
            Description = "",
            NodeType = "Standard",
            Sequence = 1
        });
        db.ComplianceNodes.Add(new ComplianceNode
        {
            Id = oeId,
            FrameworkId = fwId,
            ParentId = standardId,
            Level = 3,
            Code = "AAC.1.a",
            Title = "OE title",
            Description = "",
            NodeType = "ObjectiveElement",
            Sequence = 1
        });
        db.RequirementDetails.Add(new RequirementDetail
        {
            Id = Guid.NewGuid(),
            NodeId = standardId,
            Intent = "Ensure compliance",
            MeasurableElements = "Document policy",
            Classification = "CORE",
            IsMandatory = true,
            ApplicableDepartments = "All",
            RequiresDocumentation = true
        });
        db.RequirementScores.Add(new RequirementScore
        {
            Id = Guid.NewGuid(),
            NodeId = standardId,
            MaxScore = 5,
            Weight = 3.0m,
            ClassificationId = classificationId
        });
        db.KnowledgeItems.Add(new KnowledgeItem
        {
            Id = Guid.NewGuid(),
            NodeId = oeId,
            Explanation = "Explain",
            WhyItMatters = "Why",
            HowToComply = "Maintain records",
            EvidenceRequired = "E",
            AuditChecklist = "A",
            RiskLevel = "Low",
            ImpactLevel = "Low",
            Tags = "t",
            Version = 1,
            IsActive = true
        });
        await db.SaveChangesAsync();
        return (fwId, oeId);
    }

    private static async Task<Guid> SeedMinimalAsync(KnowledgeWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeBaseContext>();
        await db.Database.EnsureCreatedAsync();

        var fwId = Guid.NewGuid();
        db.Frameworks.Add(new Framework
        {
            Id = fwId,
            Code = "NABH_HOSP_6TH",
            Name = "NABH",
            Version = "6",
            Description = "Test",
            IsActive = true
        });
        await db.SaveChangesAsync();
        return fwId;
    }

    private static async Task<(Guid FrameworkId, Guid NodeId)> SeedWithNodeAsync(KnowledgeWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<KnowledgeBaseContext>();
        await db.Database.EnsureCreatedAsync();

        var fwId = Guid.NewGuid();
        var nodeId = Guid.NewGuid();
        db.Frameworks.Add(new Framework
        {
            Id = fwId,
            Code = "NABH_HOSP_6TH",
            Name = "NABH",
            Version = "6",
            Description = "Test",
            IsActive = true
        });
        db.ComplianceNodes.Add(new ComplianceNode
        {
            Id = nodeId,
            FrameworkId = fwId,
            ParentId = null,
            Level = 1,
            Code = "CH1",
            Title = "Chapter",
            Description = "",
            NodeType = "Chapter",
            Sequence = 1
        });
        await db.SaveChangesAsync();
        return (fwId, nodeId);
    }
}
