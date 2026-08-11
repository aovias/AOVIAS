using ComplianceService.Infrastructure.Notifications;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace ComplianceService.Tests;

public class ComplianceNotificationPublisherSelectionTests
{
    [Fact]
    public void Selects_Console_When_BaseUrl_Missing()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Assert.False(ComplianceNotificationOptions.IsCommunicationConfigured(config));
        Assert.Null(ComplianceNotificationOptions.ResolveBaseUrl(config));
    }

    [Fact]
    public void Selects_Communication_When_CommunicationService_BaseUrl_Present()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CommunicationService:BaseUrl"] = "https://localhost:7215",
            })
            .Build();

        Assert.True(ComplianceNotificationOptions.IsCommunicationConfigured(config));
        Assert.Equal("https://localhost:7215", ComplianceNotificationOptions.ResolveBaseUrl(config));
    }

    [Fact]
    public void Selects_Communication_When_Communication_BaseUrl_Present()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Communication:BaseUrl"] = "https://localhost:7215",
            })
            .Build();

        Assert.True(ComplianceNotificationOptions.IsCommunicationConfigured(config));
        Assert.Equal("https://localhost:7215", ComplianceNotificationOptions.ResolveBaseUrl(config));
    }

    [Fact]
    public void Prefers_CommunicationService_BaseUrl_Over_Communication_BaseUrl()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CommunicationService:BaseUrl"] = "https://comm-svc",
                ["Communication:BaseUrl"] = "https://legacy",
            })
            .Build();

        Assert.Equal("https://comm-svc", ComplianceNotificationOptions.ResolveBaseUrl(config));
    }

    [Theory]
    [InlineData("evidence.approved", "EvidenceApproved", "evidence-approved")]
    [InlineData("evidence.rejected", "EvidenceRejected", "evidence-rejected")]
    [InlineData("framework.activated", "FrameworkActivated", "framework-activated")]
    [InlineData("oe.ownership.assigned", "OeOwnershipAssigned", "task-assigned")]
    [InlineData("TaskAssigned", "TaskAssigned", "task-assigned")]
    [InlineData("CapaOverdue", "CapaOverdue", "capa-overdue")]
    [InlineData("AuditScheduled", "AuditScheduled", "audit-scheduled")]
    public void Maps_EventTypes_To_Templates(string input, string expectedEvent, string expectedTemplate)
    {
        var normalized = CommunicationComplianceNotificationPublisher.NormalizeEventType(input);
        Assert.Equal(expectedEvent, normalized);
        Assert.Equal(expectedTemplate, CommunicationComplianceNotificationPublisher.MapTemplateCode(normalized));
    }
}
