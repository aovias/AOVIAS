namespace ComplianceService.Application.EvidenceRegistration;

/// <summary>
/// Canonical evidence classification values for registration validation.
/// Aliases (e.g. document → Document) are normalized via <see cref="Normalize"/>.
/// </summary>
public static class EvidenceTypeCatalog
{
    public const string Policy = "Policy";
    public const string Sop = "SOP";
    public const string Register = "Register";
    public const string TrainingRecord = "Training Record";
    public const string AuditReport = "Audit Report";
    public const string Minutes = "Minutes";
    public const string Certificate = "Certificate";
    public const string ClinicalRecord = "Clinical Record";
    public const string Image = "Image";
    public const string Video = "Video";
    public const string Document = "Document";
    public const string Other = "Other";

    private static readonly HashSet<string> Canonical = new(StringComparer.OrdinalIgnoreCase)
    {
        Policy,
        Sop,
        Register,
        TrainingRecord,
        AuditReport,
        Minutes,
        Certificate,
        ClinicalRecord,
        Image,
        Video,
        Document,
        Other
    };

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["document"] = Document,
        ["sop"] = Sop,
        ["policy"] = Policy,
        ["register"] = Register,
        ["training"] = TrainingRecord,
        ["trainingrecord"] = TrainingRecord,
        ["training record"] = TrainingRecord,
        ["audit"] = AuditReport,
        ["auditreport"] = AuditReport,
        ["audit report"] = AuditReport,
        ["minutes"] = Minutes,
        ["certificate"] = Certificate,
        ["clinical"] = ClinicalRecord,
        ["clinicalrecord"] = ClinicalRecord,
        ["clinical record"] = ClinicalRecord,
        ["image"] = Image,
        ["photo"] = Image,
        ["video"] = Video,
        ["other"] = Other,
    };

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new ArgumentException("Evidence type is required.", nameof(raw));
        }

        var trimmed = raw.Trim();
        if (Canonical.Contains(trimmed))
        {
            return Canonical.First(c => c.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
        }

        if (Aliases.TryGetValue(trimmed, out var mapped))
        {
            return mapped;
        }

        throw new ArgumentException(
            $"Evidence type '{trimmed}' is not recognized. Use one of: {string.Join(", ", Canonical.OrderBy(x => x))}.",
            nameof(raw));
    }

    public static bool TryNormalize(string? raw, out string normalized)
    {
        normalized = string.Empty;
        try
        {
            normalized = Normalize(raw);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
