using ComplianceService.Application.Contracts;
using ComplianceService.Domain;
using ComplianceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ComplianceService.Application.Services;

/// <summary>
/// Assembles compliance context by loading relevant data from persistence
/// and validating relationships. Fails safely if data is incomplete.
/// No AI calls, no inference or guessing - pure data loading and validation.
/// </summary>
public class ComplianceContextAssembler : IComplianceContextAssembler
{
    private readonly IComplianceProgramRepository _programRepository;
    private readonly IControlRepository _controlRepository;
    private readonly IComplianceTaskRepository _taskRepository;
    private readonly IEvidenceRepository _evidenceRepository;
    private readonly IRegulationRepository _regulationRepository;
    private readonly ComplianceDbContext _db;
    private readonly IAuditDataProvider? _auditDataProvider;
    private readonly ILogger<ComplianceContextAssembler> _logger;

    public ComplianceContextAssembler(
        IComplianceProgramRepository programRepository,
        IControlRepository controlRepository,
        IComplianceTaskRepository taskRepository,
        IEvidenceRepository evidenceRepository,
        IRegulationRepository regulationRepository,
        ComplianceDbContext db,
        ILogger<ComplianceContextAssembler> logger,
        IAuditDataProvider? auditDataProvider = null)
    {
        _programRepository = programRepository;
        _controlRepository = controlRepository;
        _taskRepository = taskRepository;
        _evidenceRepository = evidenceRepository;
        _regulationRepository = regulationRepository;
        _db = db;
        _auditDataProvider = auditDataProvider;
        _logger = logger;
    }

    public async Task<ComplianceContext> AssembleAsync(CopilotIntent intent, CancellationToken ct = default)
    {
        if (intent == null)
        {
            throw new ArgumentNullException(nameof(intent));
        }

        var context = new ComplianceContext();

        try
        {
            // Traversal Strategy:
            // 1. Load primary entities directly from intent IDs (no inference)
            // 2. Traverse relationships to load related entities
            // 3. Validate relationships between loaded entities
            // 4. Load audit data if applicable (when AuditId provided or program has audit)

            // Step 1: Load primary entities from intent
            // Traversal: Direct lookup by ID - no guessing, no inference
            await LoadPrimaryEntitiesAsync(intent, context, ct);

            // Step 2: Traverse relationships to load related entities
            // Traversal: Follow foreign keys to discover related entities
            await TraverseRelationshipsAsync(intent, context, ct);

            // Step 3: Validate relationships between loaded entities
            // Validation: Check referential integrity without modifying data
            ValidateRelationships(context);

            // Step 4: Load audit data when applicable
            // Traversal: Load audit session and findings if AuditId provided or program has audit
            await LoadAuditDataAsync(intent, context, ct);
        }
        catch (Exception ex)
        {
            // Rule: Do NOT throw exceptions for missing optional data
            // Log error and mark context as incomplete with clear reason
            _logger.LogError(ex, "Error assembling compliance context for intent: {@Intent}", intent);
            context.ValidationErrors.Add($"Error loading data: {ex.Message}. Context may be incomplete.");
        }

        // Mark context as incomplete if validation errors exist
        if (context.ValidationErrors.Count > 0)
        {
            _logger.LogWarning("Compliance context assembled with {ErrorCount} validation error(s)", 
                context.ValidationErrors.Count);
        }

        return context;
    }

    /// <summary>
    /// Loads primary entities directly from intent identifiers.
    /// Traversal: Direct lookup - no relationship traversal at this stage.
    /// </summary>
    private async Task LoadPrimaryEntitiesAsync(CopilotIntent intent, ComplianceContext context, CancellationToken ct)
    {
        // Traversal Rule: Load Regulation if RegulationId is provided
        // No inference - only load if ID is explicitly provided
        if (intent.RegulationId.HasValue && intent.RegulationId.Value != Guid.Empty)
        {
            try
            {
                context.Regulation = await _regulationRepository.GetAsync(intent.RegulationId.Value);
                if (context.Regulation == null)
                {
                    // Rule: Mark as incomplete with clear reason - do NOT throw exception
                    context.ValidationErrors.Add(
                        $"Regulation with ID {intent.RegulationId.Value} not found. " +
                        "Regulation data is missing from persistence.");
                }
            }
            catch (Exception ex)
            {
                // Rule: Do NOT throw - log and mark as incomplete
                _logger.LogWarning(ex, "Failed to load regulation {RegulationId}", intent.RegulationId.Value);
                context.ValidationErrors.Add(
                    $"Failed to load regulation {intent.RegulationId.Value}: {ex.Message}. " +
                    "Regulation data could not be retrieved.");
            }
        }

        // Traversal Rule: Load ComplianceProgram if ProgramId is provided
        // No inference - only load if ID is explicitly provided
        if (intent.ProgramId.HasValue && intent.ProgramId.Value != Guid.Empty)
        {
            try
            {
                context.ComplianceProgram = await _programRepository.GetAsync(intent.ProgramId.Value);
                if (context.ComplianceProgram == null)
                {
                    context.ValidationErrors.Add(
                        $"Compliance program with ID {intent.ProgramId.Value} not found. " +
                        "Program data is missing from persistence.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load program {ProgramId}", intent.ProgramId.Value);
                context.ValidationErrors.Add(
                    $"Failed to load program {intent.ProgramId.Value}: {ex.Message}. " +
                    "Program data could not be retrieved.");
            }
        }

        // Traversal Rule: Load Control if ControlId is provided
        // No inference - only load if ID is explicitly provided
        if (intent.ControlId.HasValue && intent.ControlId.Value != Guid.Empty)
        {
            try
            {
                context.Control = await _controlRepository.GetAsync(intent.ControlId.Value);
                if (context.Control == null)
                {
                    context.ValidationErrors.Add(
                        $"Control with ID {intent.ControlId.Value} not found. " +
                        "Control data is missing from persistence.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load control {ControlId}", intent.ControlId.Value);
                context.ValidationErrors.Add(
                    $"Failed to load control {intent.ControlId.Value}: {ex.Message}. " +
                    "Control data could not be retrieved.");
            }
        }

        // Traversal Rule: Load ComplianceTask if TaskId is provided
        // No inference - only load if ID is explicitly provided
        if (intent.TaskId.HasValue && intent.TaskId.Value != Guid.Empty)
        {
            try
            {
                context.ComplianceTask = await _taskRepository.GetAsync(intent.TaskId.Value);
                if (context.ComplianceTask == null)
                {
                    context.ValidationErrors.Add(
                        $"Compliance task with ID {intent.TaskId.Value} not found. " +
                        "Task data is missing from persistence.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load task {TaskId}", intent.TaskId.Value);
                context.ValidationErrors.Add(
                    $"Failed to load task {intent.TaskId.Value}: {ex.Message}. " +
                    "Task data could not be retrieved.");
            }
        }
    }

    /// <summary>
    /// Traverses relationships to load related entities.
    /// Traversal Strategy:
    /// - If Control is loaded but Program is not: Load Program via Control.ProgramId
    /// - If Task is loaded but ComplianceStandard is not: Load standard via Task.ComplianceStandardId
    /// - If Program is loaded: Load all Controls and Tasks for that Program
    /// - If Task is loaded: Load all Evidence for that Task
    /// </summary>
    private async Task TraverseRelationshipsAsync(CopilotIntent intent, ComplianceContext context, CancellationToken ct)
    {
        // Traversal Rule: Control -> Program relationship
        // If Control is loaded but Program is not, traverse via Control.ProgramId
        // This follows the foreign key relationship: Control.ProgramId -> Program.Id
        if (context.Control != null && context.ComplianceProgram == null)
        {
            try
            {
                context.ComplianceProgram = await _programRepository.GetAsync(context.Control.ProgramId);
                if (context.ComplianceProgram == null)
                {
                    context.ValidationErrors.Add(
                        $"Program {context.Control.ProgramId} referenced by control {context.Control.Id} not found. " +
                        "Control-Program relationship is broken - program data is missing.");
                }
            }
            catch (Exception ex)
            {
                // Rule: Do NOT throw - mark as incomplete
                _logger.LogWarning(ex, "Failed to traverse Control->Program relationship for control {ControlId}", 
                    context.Control.Id);
                context.ValidationErrors.Add(
                    $"Failed to load program {context.Control.ProgramId} for control {context.Control.Id}: {ex.Message}. " +
                    "Control-Program relationship could not be traversed.");
            }
        }

        // Traversal Rule: Task -> ComplianceStandard
        if (context.ComplianceTask != null && context.ComplianceStandard == null)
        {
            try
            {
                context.ComplianceStandard = await _db.ComplianceStandards.AsNoTracking()
                    .Include(s => s.Chapter)
                    .FirstOrDefaultAsync(s => s.Id == context.ComplianceTask.ComplianceStandardId, ct);
                if (context.ComplianceStandard == null)
                {
                    context.ValidationErrors.Add(
                        $"Compliance standard {context.ComplianceTask.ComplianceStandardId} referenced by task {context.ComplianceTask.Id} not found. " +
                        "Task-standard relationship is broken.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to traverse Task->ComplianceStandard for task {TaskId}",
                    context.ComplianceTask.Id);
                context.ValidationErrors.Add(
                    $"Failed to load compliance standard {context.ComplianceTask.ComplianceStandardId} for task {context.ComplianceTask.Id}: {ex.Message}.");
            }
        }

        // Traversal Rule: Program -> Controls collection
        // If Program is loaded, traverse to load all Controls for that Program
        // This follows the one-to-many relationship: Program.Id -> Controls.ProgramId
        if (context.ComplianceProgram != null)
        {
            try
            {
                context.ProgramControls = await _controlRepository.GetByProgramIdAsync(context.ComplianceProgram.Id);
                // Note: Empty list is valid - program may have no controls yet
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to traverse Program->Controls relationship for program {ProgramId}", 
                    context.ComplianceProgram.Id);
                context.ValidationErrors.Add(
                    $"Failed to load controls for program {context.ComplianceProgram.Id}: {ex.Message}. " +
                    "Program-Controls relationship could not be traversed.");
            }
        }

        // Traversal Rule: Program -> Tasks collection
        // If Program is loaded, traverse to load all Tasks for that Program
        // This follows the relationship: Program.Id -> Tasks (via Program context)
        if (context.ComplianceProgram != null)
        {
            try
            {
                context.ProgramTasks = await _taskRepository.GetByProgramIdAsync(context.ComplianceProgram.Id);
                // Note: Empty list is valid - program may have no tasks yet
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to traverse Program->Tasks relationship for program {ProgramId}", 
                    context.ComplianceProgram.Id);
                context.ValidationErrors.Add(
                    $"Failed to load tasks for program {context.ComplianceProgram.Id}: {ex.Message}. " +
                    "Program-Tasks relationship could not be traversed.");
            }
        }

        // Traversal Rule: Task -> Evidence collection
        // If Task is loaded, traverse to load all Evidence for that Task
        // This follows the one-to-many relationship: Task.Id -> Evidence.TaskId
        if (context.ComplianceTask != null)
        {
            try
            {
                var allEvidence = await _evidenceRepository.Query()
                    .Where(e => e.TaskId == context.ComplianceTask.Id)
                    .ToListAsync(ct);
                context.Evidence = allEvidence;
                // Note: Empty list is valid - task may have no evidence yet
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to traverse Task->Evidence relationship for task {TaskId}", 
                    context.ComplianceTask.Id);
                context.ValidationErrors.Add(
                    $"Failed to load evidence for task {context.ComplianceTask.Id}: {ex.Message}. " +
                    "Task-Evidence relationship could not be traversed.");
            }
        }
    }

    /// <summary>
    /// Validates relationships between loaded entities.
    /// Validation Strategy:
    /// - Check Control.ProgramId matches Program.Id
    /// - Check Task.ComplianceStandardId matches ComplianceStandard.Id
    /// - Check Evidence.TaskId matches Task.Id
    /// - Check Program.HospitalId matches Regulation.HospitalId
    /// </summary>
    private void ValidateRelationships(ComplianceContext context)
    {
        // Validation Rule: Control must belong to Program
        // Check: Control.ProgramId == Program.Id
        if (context.Control != null && context.ComplianceProgram != null)
        {
            if (context.Control.ProgramId != context.ComplianceProgram.Id)
            {
                context.ValidationErrors.Add(
                    $"Control {context.Control.Id} does not belong to program {context.ComplianceProgram.Id}. " +
                    $"Control belongs to program {context.Control.ProgramId}. " +
                    "Control-Program relationship is invalid.");
            }
        }

        // Validation Rule: Task must reference loaded ComplianceStandard when both present
        if (context.ComplianceTask != null && context.ComplianceStandard != null)
        {
            if (context.ComplianceTask.ComplianceStandardId != context.ComplianceStandard.Id)
            {
                context.ValidationErrors.Add(
                    $"Compliance task {context.ComplianceTask.Id} does not match loaded standard {context.ComplianceStandard.Id}. " +
                    $"Task references standard {context.ComplianceTask.ComplianceStandardId}.");
            }
        }

        // Validation Rule: Evidence must belong to Task
        // Check: Evidence.TaskId == Task.Id for all evidence items
        if (context.Evidence.Any() && context.ComplianceTask != null)
        {
            var invalidEvidence = context.Evidence
                .Where(e => e.TaskId != context.ComplianceTask.Id)
                .ToList();

            foreach (var evidence in invalidEvidence)
            {
                context.ValidationErrors.Add(
                    $"Evidence {evidence.Id} does not belong to task {context.ComplianceTask.Id}. " +
                    $"Evidence belongs to task {evidence.TaskId}. " +
                    "Evidence-Task relationship is invalid.");
            }
        }

        // Validation Rule: Program and Regulation must belong to same Hospital
        // Check: Program.HospitalId == Regulation.HospitalId
        if (context.ComplianceProgram != null && context.Regulation != null)
        {
            if (context.ComplianceProgram.HospitalId != context.Regulation.HospitalId)
            {
                context.ValidationErrors.Add(
                    $"Compliance program {context.ComplianceProgram.Id} and regulation {context.Regulation.Id} " +
                    $"belong to different hospitals (Program: {context.ComplianceProgram.HospitalId}, " +
                    $"Regulation: {context.Regulation.HospitalId}). " +
                    "Program-Regulation organization mismatch detected.");
            }
        }
    }

    /// <summary>
    /// Loads audit data when applicable.
    /// Traversal Strategy:
    /// - If AuditId is provided: Load audit session by ID (if supported)
    /// - If Program is loaded: Load audit session by ProgramId
    /// - If AuditSession is found: Load audit findings for that session
    /// </summary>
    private async Task LoadAuditDataAsync(CopilotIntent intent, ComplianceContext context, CancellationToken ct)
    {
        if (_auditDataProvider == null)
        {
            // Rule: Audit data provider not available - not an error, just mark as unavailable
            _logger.LogDebug("Audit data provider not available - audit data will not be loaded");
            return;
        }

        try
        {
            // Traversal Rule: Load audit session by AuditId if provided
            // Direct lookup by audit identifier
            if (intent.AuditId.HasValue && intent.AuditId.Value != Guid.Empty)
            {
                // Note: This would require GetAuditSessionByIdAsync method in IAuditDataProvider
                // For now, we only support loading by program ID
                _logger.LogDebug("AuditId provided but direct lookup by AuditId not yet supported");
            }

            // Traversal Rule: Load audit session by ProgramId if Program is loaded
            // This follows the relationship: Program.Id -> AuditSession.ComplianceProgramId
            if (context.ComplianceProgram != null)
            {
                context.AuditSession = await _auditDataProvider.GetAuditSessionByProgramIdAsync(
                    context.ComplianceProgram.Id, ct);

                // Traversal Rule: If AuditSession is found, load audit findings
                // This follows the one-to-many relationship: AuditSession.Id -> AuditFindings.AuditSessionId
                if (context.AuditSession != null)
                {
                    try
                    {
                        context.AuditFindings = await _auditDataProvider.GetAuditFindingsBySessionIdAsync(
                            context.AuditSession.Id, ct);
                        // Note: Empty list is valid - audit session may have no findings yet
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to load audit findings for session {AuditSessionId}", 
                            context.AuditSession.Id);
                        context.ValidationErrors.Add(
                            $"Failed to load audit findings for session {context.AuditSession.Id}: {ex.Message}. " +
                            "Audit session-findings relationship could not be traversed.");
                    }
                }
                else
                {
                    // Rule: No audit session found - not an error, just informational
                    _logger.LogDebug("No audit session found for program {ProgramId}", context.ComplianceProgram.Id);
                }
            }
        }
        catch (Exception ex)
        {
            // Rule: Do NOT throw - mark as incomplete
            _logger.LogWarning(ex, "Failed to load audit data");
            context.ValidationErrors.Add(
                $"Failed to load audit data: {ex.Message}. " +
                "Audit data could not be retrieved - context may be incomplete.");
        }
    }
}
