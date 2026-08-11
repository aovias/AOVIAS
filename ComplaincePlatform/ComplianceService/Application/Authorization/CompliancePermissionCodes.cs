namespace ComplianceService.Application.Authorization;

/// <summary>
/// Permission code constants for ComplianceService. Codes only — not an authorization engine.
/// </summary>
public static class CompliancePermissionCodes
{
    public const string EvidenceVerify = "Compliance.Evidence.Verify";
    public const string StandardView = "Compliance.Standard.View";
    public const string EvidenceUpload = "Evidence.Document.Upload";
}
