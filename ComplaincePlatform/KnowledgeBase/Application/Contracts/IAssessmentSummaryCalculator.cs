using KnowledgeBase.Application.AssessmentSummary;

namespace KnowledgeBase.Application.Contracts;

public interface IAssessmentSummaryCalculator
{
    AssessmentSummaryResult Calculate(AssessmentSummaryInput input);
}
