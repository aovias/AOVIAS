using AIOrchestratorService.Domain;
using AIOrchestratorService.Infrastructure.Clients;

namespace AIOrchestratorService.Application.WorkflowEngine;

public class StartComplianceWorkflow : IWorkflow
{
    private readonly IComplianceClient _complianceClient;
    private readonly AuditClient _auditClient;
    private readonly ILogger<StartComplianceWorkflow> _logger;

    public StartComplianceWorkflow(
        IComplianceClient complianceClient,
        AuditClient auditClient,
        ILogger<StartComplianceWorkflow> logger)
    {
        _complianceClient = complianceClient;
        _auditClient = auditClient;
        _logger = logger;
    }

    public async Task ExecuteAsync(OrchestrationContext context)
    {
        var organizationId = context.RequestContext.OrganizationId 
            ?? throw new InvalidOperationException("OrganizationId is required in RequestContext");

        // Extract regulation from entities or use default
        var regulation = context.IntentResult.Entities.TryGetValue("regulation", out var regValue) 
            ? regValue 
            : "NABH"; // Default regulation if not specified

        _logger.LogInformation("Creating compliance program for regulation: {Regulation}, organization: {OrganizationId}", 
            regulation, organizationId);

        var programId = await _complianceClient.CreateProgramAsync(regulation, organizationId);

        _logger.LogInformation("Compliance program created with ID: {ProgramId}", programId);

        _logger.LogInformation("Loading controls for program: {ProgramId}, regulation: {Regulation}", 
            programId, regulation);

        await _complianceClient.LoadControlsAsync(programId, regulation);

        _logger.LogInformation("Controls loaded for program: {ProgramId}", programId);

        _logger.LogInformation("Assigning tasks for program: {ProgramId}, organization: {OrganizationId}", 
            programId, organizationId);

        await _complianceClient.AssignTasksAsync(programId, organizationId);

        _logger.LogInformation("Tasks assigned for program: {ProgramId}", programId);

        // Generate audit after compliance tasks
        _logger.LogInformation("Generating audit for organization {OrganizationId} and program {ProgramId}", 
            organizationId, programId);

        var auditId = await _auditClient.GenerateAudit(organizationId, programId);

        _logger.LogInformation("Generated audit with ID: {AuditId} for program {ProgramId}", auditId, programId);

        // Store auditId in intent entities so the controller can include it in the response
        context.IntentResult.Entities["AuditId"] = auditId.ToString();
    }
}
