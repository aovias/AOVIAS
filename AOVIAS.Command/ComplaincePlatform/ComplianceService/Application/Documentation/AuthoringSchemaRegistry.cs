using ComplianceService.Domain.Enums;

namespace ComplianceService.Application.Documentation;

public sealed record AuthoringSectionDefinition(
    string Key,
    string Title,
    bool IsRequired,
    int SortOrder);

public static class AuthoringSchemaRegistry
{
    public static string ResolveSchemaKey(ControlledDocumentType type) => type switch
    {
        ControlledDocumentType.Policy => "policy",
        ControlledDocumentType.Sop or ControlledDocumentType.Procedure or ControlledDocumentType.Protocol => "sop",
        ControlledDocumentType.Form or ControlledDocumentType.Checklist => "form",
        ControlledDocumentType.CommitteeDocument => "committee-minutes",
        ControlledDocumentType.Manual => "manual",
        _ => "sop",
    };

    public static IReadOnlyList<AuthoringSectionDefinition> GetSections(string schemaKey) =>
        schemaKey switch
        {
            "policy" => PolicySections,
            "form" => FormSections,
            "committee-minutes" => CommitteeSections,
            "manual" => ManualSections,
            _ => SopSections,
        };

    public static readonly IReadOnlyList<AuthoringSectionDefinition> SopSections =
    [
        new("general-info", "General Information", true, 1),
        new("purpose", "Purpose", true, 2),
        new("scope", "Scope", true, 3),
        new("definitions", "Definitions", false, 4),
        new("responsibilities", "Responsibilities", true, 5),
        new("procedure", "Procedure", true, 6),
        new("references", "References", false, 7),
        new("records", "Records", false, 8),
        new("attachments", "Attachments", false, 9),
        new("revision-history", "Revision History", true, 10),
        new("approvals", "Approvals", true, 11),
    ];

    public static readonly IReadOnlyList<AuthoringSectionDefinition> PolicySections =
    [
        new("general-info", "General Information", true, 1),
        new("policy-statement", "Policy Statement", true, 2),
        new("applicability", "Applicability", true, 3),
        new("responsibilities", "Responsibilities", true, 4),
        new("compliance-requirements", "Compliance Requirements", true, 5),
        new("references", "References", false, 6),
        new("revision-history", "Revision History", true, 7),
        new("approvals", "Approvals", true, 8),
    ];

    public static readonly IReadOnlyList<AuthoringSectionDefinition> FormSections =
    [
        new("header", "Header", true, 1),
        new("fields", "Fields", true, 2),
        new("instructions", "Instructions", true, 3),
        new("sign-off", "Sign-off", false, 4),
    ];

    public static readonly IReadOnlyList<AuthoringSectionDefinition> CommitteeSections =
    [
        new("meeting-details", "Meeting Details", true, 1),
        new("agenda", "Agenda", true, 2),
        new("discussion", "Discussion", true, 3),
        new("decisions", "Decisions", true, 4),
        new("action-items", "Action Items", true, 5),
    ];

    public static readonly IReadOnlyList<AuthoringSectionDefinition> ManualSections =
    [
        new("overview", "Overview", true, 1),
        new("organization", "Organization", true, 2),
        new("procedures", "Procedures", true, 3),
        new("references", "References", false, 4),
        new("revision-history", "Revision History", true, 5),
    ];
}
