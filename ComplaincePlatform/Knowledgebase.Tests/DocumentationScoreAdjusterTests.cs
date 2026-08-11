using KnowledgeBase.Domain.Services;
using Xunit;

namespace Knowledgebase.Tests;

public sealed class DocumentationScoreAdjusterTests
{
    [Fact]
    public void No_downgrade_when_documentation_adequate()
    {
        Assert.Equal(5, DocumentationScoreAdjuster.Apply(5, true, true));
    }

    [Fact]
    public void Downgrade_by_one_when_requires_doc_and_inadequate()
    {
        Assert.Equal(4, DocumentationScoreAdjuster.Apply(5, true, false));
        Assert.Equal(1, DocumentationScoreAdjuster.Apply(2, true, false));
    }

    [Fact]
    public void No_downgrade_when_not_required()
    {
        Assert.Equal(3, DocumentationScoreAdjuster.Apply(3, false, false));
    }
}
