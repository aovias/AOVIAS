namespace ComplianceService.Domain.Enums;

public enum DocumentAuthoringCommentType
{
    Inline = 0,
    Suggestion = 1,
    Approval = 2,
    Discussion = 3,
}

public enum DocumentCrossReferenceType
{
    Policy = 0,
    Sop = 1,
    Evidence = 2,
    ObjectiveElement = 3,
    Finding = 4,
    Capa = 5,
    Document = 6,
}

public enum DocumentExportFormat
{
    Pdf = 0,
    Docx = 1,
    Html = 2,
    Markdown = 3,
}
