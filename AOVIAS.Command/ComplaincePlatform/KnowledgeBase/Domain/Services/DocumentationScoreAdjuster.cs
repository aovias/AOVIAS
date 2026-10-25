namespace KnowledgeBase.Domain.Services;

/// <summary>
/// Applies NABH-style documentation downgrade: inadequate documentation can reduce score by one point.
/// </summary>
public static class DocumentationScoreAdjuster
{
    public static int Apply(int rawScoreOneToFive, bool requiresDocumentation, bool documentationAdequate)
    {
        if (rawScoreOneToFive is < 1 or > 5)
            throw new ArgumentOutOfRangeException(nameof(rawScoreOneToFive));

        if (!requiresDocumentation || documentationAdequate)
            return rawScoreOneToFive;

        return Math.Max(1, rawScoreOneToFive - 1);
    }
}
