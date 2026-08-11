namespace Shared.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string? FromDisplayName { get; set; }
    public bool EnableSsl { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryBaseDelayMs { get; set; } = 500;
}

public sealed class EmailProviderOptions
{
    public const string SectionName = "Email";

    /// <summary>Console | Smtp</summary>
    public string Provider { get; set; } = "Console";
}
