namespace Aovias.Publishing.Contracts;

/// <summary>
/// Outcome of an individual quality gate (D13).
/// </summary>
public enum QualityGateStatus
{
    Passed,
    Failed,
    Skipped
}

/// <summary>
/// A single quality gate that publishing must satisfy before it can finalize.
/// </summary>
public sealed record QualityGate(string Name, QualityGateStatus Status, string Detail)
{
    public bool Blocking => Status == QualityGateStatus.Failed;
}

/// <summary>
/// Aggregated quality gate evaluation (D13). Publishing cannot reach Deployment
/// Ready unless every non-skipped gate passes.
/// </summary>
public sealed class QualityGateReport
{
    public required IReadOnlyList<QualityGate> Gates { get; init; }

    public bool AllPassed => Gates.All(gate => gate.Status != QualityGateStatus.Failed);

    public IReadOnlyList<QualityGate> FailedGates
        => Gates.Where(gate => gate.Status == QualityGateStatus.Failed).ToList();
}
