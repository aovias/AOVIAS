using Shared.Infrastructure.Email;

namespace IdentityService.Configuration;

public sealed class IdentityEmailVerificationOptions
{
    public const string SectionName = "Identity";

    /// <summary>Primary mode. When unset, legacy RequireEmailVerification is mapped.</summary>
    public string? EmailVerificationMode { get; set; }

    /// <summary>Legacy bool — true maps to RequiredBeforeLogin, false to Async.</summary>
    public bool? RequireEmailVerification { get; set; }

    public string PublicBaseUrl { get; set; } = "http://localhost:4200";

    public EmailVerificationTokenOptions EmailVerification { get; set; } = new();
}

public sealed class EmailVerificationTokenOptions
{
    public int TokenLifetimeHours { get; set; } = 48;
    public int ReminderAfterDays { get; set; } = 3;
    public int ResendCooldownoldownSeconds { get; set; } = 60;
}

public static class EmailVerificationModeResolver
{
    public static EmailVerificationMode Resolve(IConfiguration configuration)
    {
        var modeText = configuration["Identity:EmailVerificationMode"];
        if (!string.IsNullOrWhiteSpace(modeText)
            && Enum.TryParse<EmailVerificationMode>(modeText, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        // Legacy mapping
        if (configuration.GetValue<bool?>("Identity:RequireEmailVerification") == true)
            return EmailVerificationMode.RequiredBeforeLogin;

        // Phase 0 default commercial: Async
        return EmailVerificationMode.Async;
    }

    public static bool ShouldSendVerificationEmail(EmailVerificationMode mode)
        => mode is not EmailVerificationMode.None;

    public static bool ShouldConfirmEmailOnCreate(EmailVerificationMode mode)
        => mode is EmailVerificationMode.None;

    /// <summary>
    /// Async/None: account is Active (may be unconfirmed). Required*: PendingVerification until confirmed.
    /// </summary>
    public static bool IsActiveOnCreate(EmailVerificationMode mode)
        => mode is EmailVerificationMode.None or EmailVerificationMode.Async;

    public static bool BlocksPasswordLoginWhenUnconfirmed(EmailVerificationMode mode)
        => mode is EmailVerificationMode.RequiredBeforeLogin
            or EmailVerificationMode.RequiredBeforeOrganizationProvisioning;

    public static bool BlocksAnonymousOrgSignup(EmailVerificationMode mode)
        => mode is EmailVerificationMode.RequiredBeforeOrganizationProvisioning;
}
