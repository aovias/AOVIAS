using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Infrastructure.Email
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string html);
    }
}
