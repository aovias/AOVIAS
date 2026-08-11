namespace Shared.Infrastructure.Email;

/// <summary>HTML + plain-text identity email bodies for CRP Phase 1.</summary>
public static class IdentityEmailTemplates
{
    public static (string Subject, string Html) Verification(string verifyUrl, string? displayName = null)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? "there" : displayName;
        var subject = "Verify your AOVIAS account";
        var html = $"""
            <html><body style="font-family:Segoe UI,Arial,sans-serif;color:#1a1a1a">
            <h2>Verify your email</h2>
            <p>Hi {System.Net.WebUtility.HtmlEncode(name)},</p>
            <p>Please confirm your email address to finish setting up your AOVIAS account.</p>
            <p><a href="{System.Net.WebUtility.HtmlEncode(verifyUrl)}" style="display:inline-block;padding:10px 16px;background:#0b5fff;color:#fff;text-decoration:none;border-radius:4px">Verify email</a></p>
            <p>Or open this link:<br/><span style="word-break:break-all">{System.Net.WebUtility.HtmlEncode(verifyUrl)}</span></p>
            <p>If you did not create an account, you can ignore this message.</p>
            </body></html>
            """;
        return (subject, html);
    }

    public static (string Subject, string Html) Welcome(string? displayName = null)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? "there" : displayName;
        var subject = "Welcome to AOVIAS Command";
        var html = $"""
            <html><body style="font-family:Segoe UI,Arial,sans-serif;color:#1a1a1a">
            <h2>Welcome</h2>
            <p>Hi {System.Net.WebUtility.HtmlEncode(name)},</p>
            <p>Your AOVIAS Command account is ready. Sign in to continue onboarding and compliance setup.</p>
            </body></html>
            """;
        return (subject, html);
    }

    /// <summary>Welcome after SaaS signup with trial commercial details (CRP Phase 2/3).</summary>
    public static (string Subject, string Html) WelcomeWithTrial(
        string? displayName,
        string organizationName,
        int trialDays,
        int maxSeats,
        DateTime? expiresAtUtc,
        string? loginUrl = null,
        string? docsUrl = null,
        string? supportEmail = null)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? "there" : displayName;
        var org = string.IsNullOrWhiteSpace(organizationName) ? "your organization" : organizationName;
        var expiry = expiresAtUtc.HasValue
            ? expiresAtUtc.Value.ToString("yyyy-MM-dd") + " UTC"
            : $"{trialDays} days from signup";
        var login = string.IsNullOrWhiteSpace(loginUrl) ? "#" : loginUrl;
        var docs = string.IsNullOrWhiteSpace(docsUrl) ? null : docsUrl;
        var support = string.IsNullOrWhiteSpace(supportEmail) ? null : supportEmail;
        var subject = "Welcome to AOVIAS Command — your trial is ready";
        var html = $"""
            <html><body style="font-family:Segoe UI,Arial,sans-serif;color:#1a1a1a">
            <h2>Welcome to AOVIAS</h2>
            <p>Hi {System.Net.WebUtility.HtmlEncode(name)},</p>
            <p><strong>{System.Net.WebUtility.HtmlEncode(org)}</strong> is provisioned with a <strong>{trialDays}-day trial</strong>
            ({maxSeats} seats). Trial expires on <strong>{System.Net.WebUtility.HtmlEncode(expiry)}</strong>.</p>
            <p><strong>Next steps</strong></p>
            <ol>
              <li>Sign in and open the onboarding wizard</li>
              <li>Complete hospital profile and framework selection</li>
              <li>Run the compliance assessment and invite your team</li>
            </ol>
            <p><a href="{System.Net.WebUtility.HtmlEncode(login)}" style="display:inline-block;padding:10px 16px;background:#0b5fff;color:#fff;text-decoration:none;border-radius:4px">Sign in</a></p>
            {(docs == null ? "" : $"<p>Documentation: <a href=\"{System.Net.WebUtility.HtmlEncode(docs)}\">{System.Net.WebUtility.HtmlEncode(docs)}</a></p>")}
            {(support == null ? "" : $"<p>Support: <a href=\"mailto:{System.Net.WebUtility.HtmlEncode(support)}\">{System.Net.WebUtility.HtmlEncode(support)}</a></p>")}
            </body></html>
            """;
        return (subject, html);
    }

    /// <summary>Getting-started follow-up after signup (CRP Phase 3).</summary>
    public static (string Subject, string Html) GettingStarted(
        string? displayName,
        string organizationName,
        string? loginUrl = null,
        string? docsUrl = null,
        string? supportEmail = null)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? "there" : displayName;
        var org = string.IsNullOrWhiteSpace(organizationName) ? "your organization" : organizationName;
        var login = string.IsNullOrWhiteSpace(loginUrl) ? "#" : loginUrl;
        var subject = "Getting started with AOVIAS Command";
        var html = $"""
            <html><body style="font-family:Segoe UI,Arial,sans-serif;color:#1a1a1a">
            <h2>Getting started</h2>
            <p>Hi {System.Net.WebUtility.HtmlEncode(name)},</p>
            <p>Use this checklist to bring <strong>{System.Net.WebUtility.HtmlEncode(org)}</strong> to operational readiness:</p>
            <ul>
              <li>Finish guided onboarding (profile → frameworks → assessment)</li>
              <li>Invite quality and department owners</li>
              <li>Review your trial status and seat usage</li>
            </ul>
            <p><a href="{System.Net.WebUtility.HtmlEncode(login)}" style="display:inline-block;padding:10px 16px;background:#0b5fff;color:#fff;text-decoration:none;border-radius:4px">Continue setup</a></p>
            {(string.IsNullOrWhiteSpace(docsUrl) ? "" : $"<p>Knowledge base: <a href=\"{System.Net.WebUtility.HtmlEncode(docsUrl)}\">{System.Net.WebUtility.HtmlEncode(docsUrl)}</a></p>")}
            {(string.IsNullOrWhiteSpace(supportEmail) ? "" : $"<p>Need help? <a href=\"mailto:{System.Net.WebUtility.HtmlEncode(supportEmail)}\">{System.Net.WebUtility.HtmlEncode(supportEmail)}</a></p>")}
            </body></html>
            """;
        return (subject, html);
    }

    public static (string Subject, string Html) PasswordReset(string resetUrl)
    {
        var subject = "Reset your AOVIAS password";
        var html = $"""
            <html><body style="font-family:Segoe UI,Arial,sans-serif;color:#1a1a1a">
            <h2>Password reset</h2>
            <p>We received a request to reset your password.</p>
            <p><a href="{System.Net.WebUtility.HtmlEncode(resetUrl)}" style="display:inline-block;padding:10px 16px;background:#0b5fff;color:#fff;text-decoration:none;border-radius:4px">Reset password</a></p>
            <p>Or open this link:<br/><span style="word-break:break-all">{System.Net.WebUtility.HtmlEncode(resetUrl)}</span></p>
            <p>If you did not request this, you can ignore this message.</p>
            </body></html>
            """;
        return (subject, html);
    }

    public static (string Subject, string Html) SecurityNotification(string title, string detail)
    {
        var subject = $"AOVIAS security notice: {title}";
        var html = $"""
            <html><body style="font-family:Segoe UI,Arial,sans-serif;color:#1a1a1a">
            <h2>{System.Net.WebUtility.HtmlEncode(title)}</h2>
            <p>{System.Net.WebUtility.HtmlEncode(detail)}</p>
            <p>If this was not you, reset your password and contact your administrator.</p>
            </body></html>
            """;
        return (subject, html);
    }

    public static (string Subject, string Html) Invitation(string inviteUrl, string organizationName)
    {
        var subject = $"Invitation to join {organizationName} on AOVIAS";
        var html = $"""
            <html><body style="font-family:Segoe UI,Arial,sans-serif;color:#1a1a1a">
            <h2>You're invited</h2>
            <p>You have been invited to join <strong>{System.Net.WebUtility.HtmlEncode(organizationName)}</strong>.</p>
            <p><a href="{System.Net.WebUtility.HtmlEncode(inviteUrl)}" style="display:inline-block;padding:10px 16px;background:#0b5fff;color:#fff;text-decoration:none;border-radius:4px">Accept invitation</a></p>
            </body></html>
            """;
        return (subject, html);
    }

    /// <summary>CRP Phase 5 — trial days-remaining reminder.</summary>
    public static (string Subject, string Html) TrialReminder(
        string? displayName,
        string organizationName,
        int daysRemaining,
        string licenseStatus,
        string? upgradeUrl = null,
        string? supportEmail = null,
        string? docsUrl = null)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? "there" : displayName;
        var org = string.IsNullOrWhiteSpace(organizationName) ? "your organization" : organizationName;
        var subject = daysRemaining <= 1
            ? "Your AOVIAS trial ends tomorrow"
            : $"Your AOVIAS trial ends in {daysRemaining} days";
        var html = $"""
            <html><body style="font-family:Segoe UI,Arial,sans-serif;color:#1a1a1a">
            <h2>Trial reminder</h2>
            <p>Hi {System.Net.WebUtility.HtmlEncode(name)},</p>
            <p><strong>{System.Net.WebUtility.HtmlEncode(org)}</strong> has <strong>{daysRemaining}</strong> day(s) remaining on trial
            (status: {System.Net.WebUtility.HtmlEncode(licenseStatus)}).</p>
            <p>Upgrade to keep full access, seats, and framework activation.</p>
            {(string.IsNullOrWhiteSpace(upgradeUrl) ? "" : $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(upgradeUrl)}\" style=\"display:inline-block;padding:10px 16px;background:#0b5fff;color:#fff;text-decoration:none;border-radius:4px\">Upgrade options</a></p>")}
            {(string.IsNullOrWhiteSpace(docsUrl) ? "" : $"<p>Docs: <a href=\"{System.Net.WebUtility.HtmlEncode(docsUrl)}\">{System.Net.WebUtility.HtmlEncode(docsUrl)}</a></p>")}
            {(string.IsNullOrWhiteSpace(supportEmail) ? "" : $"<p>Support: <a href=\"mailto:{System.Net.WebUtility.HtmlEncode(supportEmail)}\">{System.Net.WebUtility.HtmlEncode(supportEmail)}</a></p>")}
            </body></html>
            """;
        return (subject, html);
    }

    public static (string Subject, string Html) TrialGraceStarted(
        string? displayName,
        string organizationName,
        string licenseStatus,
        string? upgradeUrl = null,
        string? supportEmail = null,
        string? docsUrl = null)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? "there" : displayName;
        var org = string.IsNullOrWhiteSpace(organizationName) ? "your organization" : organizationName;
        var subject = "Your AOVIAS trial has entered the grace period";
        var html = $"""
            <html><body style="font-family:Segoe UI,Arial,sans-serif;color:#1a1a1a">
            <h2>Grace period started</h2>
            <p>Hi {System.Net.WebUtility.HtmlEncode(name)},</p>
            <p>The trial for <strong>{System.Net.WebUtility.HtmlEncode(org)}</strong> is now in grace ({System.Net.WebUtility.HtmlEncode(licenseStatus)}).
            You can still work, but upgrade soon to avoid read-only mode.</p>
            {(string.IsNullOrWhiteSpace(upgradeUrl) ? "" : $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(upgradeUrl)}\" style=\"display:inline-block;padding:10px 16px;background:#0b5fff;color:#fff;text-decoration:none;border-radius:4px\">Upgrade now</a></p>")}
            {(string.IsNullOrWhiteSpace(supportEmail) ? "" : $"<p>Support: <a href=\"mailto:{System.Net.WebUtility.HtmlEncode(supportEmail)}\">{System.Net.WebUtility.HtmlEncode(supportEmail)}</a></p>")}
            </body></html>
            """;
        return (subject, html);
    }

    public static (string Subject, string Html) TrialGraceEnding(
        string? displayName,
        string organizationName,
        string? upgradeUrl = null,
        string? supportEmail = null)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? "there" : displayName;
        var org = string.IsNullOrWhiteSpace(organizationName) ? "your organization" : organizationName;
        var subject = "AOVIAS grace period ending soon";
        var html = $"""
            <html><body style="font-family:Segoe UI,Arial,sans-serif;color:#1a1a1a">
            <h2>Grace ending</h2>
            <p>Hi {System.Net.WebUtility.HtmlEncode(name)},</p>
            <p>Grace for <strong>{System.Net.WebUtility.HtmlEncode(org)}</strong> ends soon. After that the workspace becomes read-only.</p>
            {(string.IsNullOrWhiteSpace(upgradeUrl) ? "" : $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(upgradeUrl)}\">Upgrade options</a></p>")}
            {(string.IsNullOrWhiteSpace(supportEmail) ? "" : $"<p>Support: {System.Net.WebUtility.HtmlEncode(supportEmail)}</p>")}
            </body></html>
            """;
        return (subject, html);
    }

    public static (string Subject, string Html) TrialExpiredReadOnly(
        string? displayName,
        string organizationName,
        string? upgradeUrl = null,
        string? supportEmail = null,
        string? docsUrl = null)
    {
        var name = string.IsNullOrWhiteSpace(displayName) ? "there" : displayName;
        var org = string.IsNullOrWhiteSpace(organizationName) ? "your organization" : organizationName;
        var subject = "Your AOVIAS trial has expired — read-only mode";
        var html = $"""
            <html><body style="font-family:Segoe UI,Arial,sans-serif;color:#1a1a1a">
            <h2>Trial expired</h2>
            <p>Hi {System.Net.WebUtility.HtmlEncode(name)},</p>
            <p><strong>{System.Net.WebUtility.HtmlEncode(org)}</strong> is now in read-only mode. Existing data remains viewable;
            new records, users, seats, and framework activations are blocked until you upgrade or reactivate.</p>
            {(string.IsNullOrWhiteSpace(upgradeUrl) ? "" : $"<p><a href=\"{System.Net.WebUtility.HtmlEncode(upgradeUrl)}\" style=\"display:inline-block;padding:10px 16px;background:#0b5fff;color:#fff;text-decoration:none;border-radius:4px\">Upgrade / reactivate</a></p>")}
            {(string.IsNullOrWhiteSpace(docsUrl) ? "" : $"<p>Docs: <a href=\"{System.Net.WebUtility.HtmlEncode(docsUrl)}\">{System.Net.WebUtility.HtmlEncode(docsUrl)}</a></p>")}
            {(string.IsNullOrWhiteSpace(supportEmail) ? "" : $"<p>Support: <a href=\"mailto:{System.Net.WebUtility.HtmlEncode(supportEmail)}\">{System.Net.WebUtility.HtmlEncode(supportEmail)}</a></p>")}
            </body></html>
            """;
        return (subject, html);
    }

    public static (string Subject, string Html) TrialInternalAlert(
        string title,
        string organizationId,
        string licenseStatus,
        string supportWorkspaceUrl)
    {
        var subject = $"[AOVIAS Ops] {title}";
        var html = $"""
            <html><body style="font-family:Segoe UI,Arial,sans-serif;color:#1a1a1a">
            <h2>{System.Net.WebUtility.HtmlEncode(title)}</h2>
            <p>Organization: <code>{System.Net.WebUtility.HtmlEncode(organizationId)}</code></p>
            <p>License status: {System.Net.WebUtility.HtmlEncode(licenseStatus)}</p>
            <p><a href="{System.Net.WebUtility.HtmlEncode(supportWorkspaceUrl)}">Open support workspace</a></p>
            </body></html>
            """;
        return (subject, html);
    }
}
