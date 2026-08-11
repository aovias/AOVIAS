using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Shared.Infrastructure.Email;

/// <summary>Production SMTP delivery with TLS and exponential backoff retries.</summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly SmtpOptions _options;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(IOptions<SmtpOptions> options, ILogger<SmtpEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string html)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
            throw new InvalidOperationException("Smtp:Host is required when Email:Provider=Smtp.");
        if (string.IsNullOrWhiteSpace(_options.FromAddress))
            throw new InvalidOperationException("Smtp:FromAddress is required when Email:Provider=Smtp.");

        var attempts = Math.Max(1, _options.MaxRetryAttempts);
        Exception? last = null;

        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using var message = new MailMessage
                {
                    From = new MailAddress(
                        _options.FromAddress,
                        string.IsNullOrWhiteSpace(_options.FromDisplayName) ? null : _options.FromDisplayName),
                    Subject = subject,
                    Body = html,
                    IsBodyHtml = true
                };
                message.To.Add(to);
                using var client = new SmtpClient(_options.Host, _options.Port)
                {
                    EnableSsl = _options.EnableSsl,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Timeout = Math.Max(5, _options.TimeoutSeconds) * 1000
                };

                if (!string.IsNullOrWhiteSpace(_options.UserName))
                {
                    client.Credentials = new NetworkCredential(_options.UserName, _options.Password ?? string.Empty);
                }

                await client.SendMailAsync(message).ConfigureAwait(false);
                _logger.LogInformation("SMTP email sent to {To} subject {Subject} attempt {Attempt}", to, subject, attempt);
                return;
            }
            catch (Exception ex)
            {
                last = ex;
                _logger.LogWarning(ex, "SMTP send failed to {To} attempt {Attempt}/{Max}", to, attempt, attempts);
                if (attempt < attempts)
                {
                    var delay = _options.RetryBaseDelayMs * (int)Math.Pow(2, attempt - 1);
                    await Task.Delay(delay).ConfigureAwait(false);
                }
            }
        }

        throw new InvalidOperationException($"Failed to send email to {to} after {attempts} attempts.", last);
    }
}
