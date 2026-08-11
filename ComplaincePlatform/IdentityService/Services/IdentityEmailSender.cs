using Microsoft.AspNetCore.Identity.UI.Services;
using Shared.Infrastructure.Email;

namespace IdentityService.Services
{
    public class IdentityEmailSender : IEmailSender
    {
        private readonly IEmailService _email;

        public IdentityEmailSender(IEmailService email)
        {
            _email = email;
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
            => _email.SendAsync(email, subject, htmlMessage);
    }
}
