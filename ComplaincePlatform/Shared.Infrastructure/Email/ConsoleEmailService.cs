using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Infrastructure.Email
{
    public class ConsoleEmailService : IEmailService
    {
        public Task SendAsync(string to, string subject, string html)
        {
            Console.WriteLine($"To: {to}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine(html);
            return Task.CompletedTask;
        }
    }
}
