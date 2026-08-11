using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;
using Shared.Infrastructure.DTO;

namespace ComplianceService.Domain
{
    public class ComplianceTask : BaseEntity
    {
        private decimal _weight = 1.0m;

        public Guid HospitalId { get; set; }
        public Guid ComplianceStandardId { get; set; }
        public ComplianceStandard ComplianceStandard { get; set; } = null!;
        public Guid ObjectiveElementId { get; set; }
        public ObjectiveElement ObjectiveElement { get; set; } = null!;

        /// <summary>
        /// Optional link to the compliance deficiency that triggered this remediation task.
        /// One deficiency may have many tasks; tasks without a source are bulk-generated defaults.
        /// </summary>
        public Guid? SourceDeficiencyId { get; set; }

        public Guid? ControlId { get; set; }

        /// <summary>Optional program association (future use; never required for flows).</summary>
        public Guid? ProgramId { get; set; }
        public ComplianceProgram? Program { get; set; }

        /// <summary>Hospital department (Organization department id).</summary>
        public Guid? DepartmentId { get; set; }

        /// <summary>Denormalized label for UI (set on create/update).</summary>
        public string? DepartmentName { get; set; }

        public string AssignedToUserId { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public ComplianceTaskStatus Status { get; set; } = ComplianceTaskStatus.Pending;
        public string? Title { get; set; }
        public string? Description { get; set; }

        /// <summary>Business category (Compliance, CAPA, Audit, …).</summary>
        public TaskCategory Category { get; set; } = TaskCategory.Compliance;

        /// <summary>Execution kind (Standard, Approval, Review).</summary>
        public TaskKind TaskKind { get; set; } = TaskKind.Standard;

        /// <summary>
        /// Optional predecessor task. Complete is blocked while the blocker is not Done.
        /// </summary>
        public Guid? BlockedByTaskId { get; set; }

        /// <summary>
        /// When set, this task was materialized from a <see cref="RecurringTaskDefinition"/>.
        /// </summary>
        public Guid? RecurringDefinitionId { get; set; }

        /// <summary>UTC timestamp when a TaskOverdue escalation event was last emitted.</summary>
        public DateTime? OverdueEscalatedAt { get; set; }
        /// <summary>Free-text maturity state (legacy; prefer <see cref="MaturityStage"/> for new code).</summary>
        public string? MaturityState { get; set; }

        /// <summary>
        /// Typed maturity stage classification, derived from the linked <see cref="ObjectiveElement.Classification"/>
        /// at task creation time. Replaces the free-text <see cref="MaturityState"/> field.
        /// </summary>
        public ObjectiveElementClassification? MaturityStage { get; set; }
        public ExplainabilityDto? Explainability { get; set; }

        /// <summary>
        /// Criticality level of the compliance task.
        /// Default value: Medium
        /// </summary>
        public TaskCriticalityLevel Criticality { get; set; } = TaskCriticalityLevel.Medium;

        /// <summary>
        /// Execution priority (distinct from <see cref="Criticality"/> which drives scoring).
        /// Default value: Medium
        /// </summary>
        public ComplianceTaskPriority Priority { get; set; } = ComplianceTaskPriority.Medium;

        /// <summary>
        /// Weight for compliance scoring (0-100 range recommended).
        /// Default value: 1.0
        /// </summary>
        public decimal Weight
        {
            get => _weight;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("Weight cannot be negative.", nameof(value));
                }
                _weight = value;
            }
        }
    }
}
