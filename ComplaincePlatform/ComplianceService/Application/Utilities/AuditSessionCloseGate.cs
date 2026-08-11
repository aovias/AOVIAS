using ComplianceService.Domain;
using Shared.Infrastructure.Domain.Enums;

namespace ComplianceService.Application.Utilities;

/// <summary>
/// Blocks audit session close when linked findings are still open.
/// A finding is considered closed for the gate when it is Resolved, Closed, Accepted, or Promoted.
/// Accepted findings without CAPA are auto-promoted by AuditSessionClosed workflow rule.
/// </summary>
public static class AuditSessionCloseGate
{
    public static bool IsFindingResolvedForClose(Finding finding)
    {
        if (finding == null) return true;

        if (finding.Status == FindingStatus.Closed)
            return true;

        if (finding.Status == FindingStatus.Accepted)
            return true;

        if (finding.IsResolved)
            return true;

        if (finding.PromotedDeficiencyId.HasValue)
            return true;

        return false;
    }

    public static IReadOnlyList<Finding> GetBlockingFindings(IEnumerable<Finding> findings) =>
        findings.Where(f => !IsFindingResolvedForClose(f)).ToList();

    public static void EnsureCanClose(IEnumerable<Finding> findings)
    {
        var blocking = GetBlockingFindings(findings);
        if (blocking.Count == 0)
            return;

        var sample = string.Join(", ", blocking.Take(5).Select(f =>
            string.IsNullOrWhiteSpace(f.FindingNumber) ? f.Id.ToString("N")[..8] : f.FindingNumber));

        throw new InvalidOperationException(
            $"Cannot close audit session while {blocking.Count} finding(s) remain open (not Resolved/Closed/Accepted/Promoted). Examples: {sample}");
    }
}
