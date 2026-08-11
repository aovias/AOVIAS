using KnowledgeBase.Application.DTOs;

namespace KnowledgeBase.Application.Questionnaire;

public interface IQuestionnaireGenerator
{
    FrameworkQuestionnaireDto Generate(QuestionnaireInput input);
}
