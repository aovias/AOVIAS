using KnowledgeBase.Domain.Entities;

namespace KnowledgeBase.Domain.Services;

public sealed class ComplianceNodeSequenceComparer : IComparer<ComplianceNode>
{
    public int Compare(ComplianceNode? x, ComplianceNode? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;
        return x.Sequence.CompareTo(y.Sequence);
    }
}
