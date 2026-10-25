using ComplianceService.Domain.Enums;
using Shared.Infrastructure.Domain;

namespace ComplianceService.Domain
{
    /// <summary>
    /// Enterprise risk register entry (extends legacy compliance risk scoring entity).
    /// Risks may be hospital-level standalone or linked to OE / Task / Finding / Incident / Deficiency.
    /// </summary>
    public class Risk : BaseEntity
    {
        public Guid TenantId { get; set; }
        public Guid HospitalId { get; set; }

        public Guid? ObjectiveElementId { get; set; }
        public ObjectiveElement? ObjectiveElement { get; set; }

        public Guid? TaskId { get; set; }
        public ComplianceTask? Task { get; set; }

        public Guid? FindingId { get; set; }
        public Finding? Finding { get; set; }

        public Guid? IncidentId { get; set; }
        public Incident? Incident { get; set; }

        public Guid? DeficiencyId { get; set; }
        public ComplianceDeficiency? Deficiency { get; set; }

        public Guid? DepartmentId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }

        /// <summary>Risk category (e.g. Clinical, Operational, Financial, Regulatory).</summary>
        public string? Category { get; set; }

        public ComplianceSeverity Severity { get; set; }

        /// <summary>Inherent likelihood (1-5).</summary>
        public int Likelihood { get; set; }

        /// <summary>Inherent impact (1-5).</summary>
        public int Impact { get; set; }

        /// <summary>Inherent risk score = Likelihood × Impact (not persisted).</summary>
        public int RiskScore => Likelihood * Impact;

        public int? ResidualLikelihood { get; set; }
        public int? ResidualImpact { get; set; }

        /// <summary>Residual score when both residual dimensions are set.</summary>
        public int? ResidualRiskScore =>
            ResidualLikelihood.HasValue && ResidualImpact.HasValue
                ? ResidualLikelihood.Value * ResidualImpact.Value
                : null;

        public Guid? OwnerUserId { get; set; }
        public string? OwnerName { get; set; }

        public string? MitigationPlan { get; set; }

        public DateTime? ReviewDueDate { get; set; }

        public RiskStatus Status { get; set; } = RiskStatus.Identified;

        public DateTime IdentifiedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsMutable => Status != RiskStatus.Closed;

        public void EnsureMutable()
        {
            if (!IsMutable)
                throw new InvalidOperationException("Closed risks are immutable.");
        }

        public void TransitionTo(RiskStatus target, DateTime? utcNow = null)
        {
            if (Status == target) return;
            if (!Status.CanTransitionTo(target))
                throw new InvalidOperationException($"Cannot transition risk from {Status} to {target}.");

            var now = utcNow ?? DateTime.UtcNow;
            Status = target;
            LastUpdatedAt = now;

            if (target == RiskStatus.Closed)
                ClosedAt = now;
            else
                ClosedAt = null;
        }

        public static void ValidateScoreDimensions(int likelihood, int impact)
        {
            if (likelihood is < 1 or > 5)
                throw new ArgumentOutOfRangeException(nameof(likelihood), "Likelihood must be between 1 and 5.");
            if (impact is < 1 or > 5)
                throw new ArgumentOutOfRangeException(nameof(impact), "Impact must be between 1 and 5.");
        }

        public static void ValidateResidualDimensions(int? residualLikelihood, int? residualImpact)
        {
            if (residualLikelihood is < 1 or > 5)
                throw new ArgumentOutOfRangeException(nameof(residualLikelihood), "Residual likelihood must be between 1 and 5.");
            if (residualImpact is < 1 or > 5)
                throw new ArgumentOutOfRangeException(nameof(residualImpact), "Residual impact must be between 1 and 5.");
        }
    }
}
